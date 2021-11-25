using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChartHelper;
using ChartMetrics;

namespace ChartAutoRating {
    public class Program {
        public static readonly int METRIC_COUNT = ChartProcessor.Metrics.Count;
        public static readonly int CALCULATOR_COUNT = 32;
        public static readonly int GROUP_COUNT = 8;
        
        private static readonly int CROSSOVERS = 4;
        private static readonly double MIN_CROSS_CHANCE = 0.25f;
        private static readonly double MIN_KILL_CHANCE = 0.125f;
        private static readonly string[] METRIC_NAMES = ChartProcessor.Metrics.Select(metric => metric.Name).ToArray();

        public class CalculatorInfo : IComparable<CalculatorInfo> {
            public Calculator Calculator { get; }
            
            public double Fitness { get; set; }
            
            public double CrossChance { get; set; }
            
            public double KillChance { get; set; }
            
            public CalculatorInfo Next { get; set; }

            public CalculatorInfo(Calculator calculator) {
                Calculator = calculator;
            }

            public int CompareTo(CalculatorInfo other) => -Fitness.CompareTo(other.Fitness);
        }

        private class ThreadInfo {
            public CalculatorInfo[] Group { get; }
            
            public long Generation { get; set; }
            
            public bool Active { get; set; }
            
            public object Lock { get; }

            public ThreadInfo(CalculatorInfo[] group) {
                Group = group;
                Generation = 0;
                Active = true;
                Lock = new object();
            }
        }

        public class DrawInfoItem {
            public double Fitness { get; set; }
                
            public CurveWeights[] CurveWeights { get; }

            public DrawInfoItem() => CurveWeights = new CurveWeights[METRIC_COUNT];
        }

        public static void Main(string[] args) {
            var random = new Random();
            var dataSet = new DataSet(GetDataSamples());
            double[] baseCoefficients = DataSet.Normalize(dataSet);
            var groups = GetInitialGroups(dataSet, random);
            var threadInfo = groups.Select(group => new ThreadInfo(group)).ToArray();
            var form = new Form1();

            Parallel.Invoke(() => MainThread(threadInfo, form), () => FormThread(form), () => TrainGroups(threadInfo, dataSet, random));
            SaveGroups(groups);
            OutputDetailedInfo(groups);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static void MainThread(ThreadInfo[] threadInfo, Form1 form) {
            double[] bestHistory = new double[GROUP_COUNT];
            int[] generationHistory = new int[GROUP_COUNT];
            var drawWatch = new Stopwatch();
            var checkWatch = new Stopwatch();
            var drawInfo = new DrawInfoItem[GROUP_COUNT][];

            for (int i = 0; i < GROUP_COUNT; i++) {
                var group = new DrawInfoItem[CALCULATOR_COUNT];
                
                for (int j = 0; j < CALCULATOR_COUNT; j++)
                    group[j] = new DrawInfoItem();

                drawInfo[i] = group;
            }

            drawWatch.Start();
            checkWatch.Start();

            while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter) {
                if (drawWatch.ElapsedMilliseconds > 66) {
                    double best = 0d;
                    double worst = 1d;
                    
                    for (int i = 0; i < GROUP_COUNT; i++) {
                        var thread = threadInfo[i];
                        var group = thread.Group;
                        var drawGroup = drawInfo[i];

                        lock (thread.Lock) {
                            for (int j = 0; j < CALCULATOR_COUNT; j++) {
                                var info = group[j];
                                var item = drawGroup[j];
                                var weights = info.Calculator.MetricCurveWeights;
                                double fitness = info.Fitness;

                                item.Fitness = fitness;

                                if (fitness > best)
                                    best = fitness;

                                if (fitness < worst)
                                    worst = fitness;

                                for (int k = 0; k < METRIC_COUNT; k++)
                                    item.CurveWeights[k] = weights[k];
                            }
                        }
                    }
                    
                    form.Draw(drawInfo, best, worst);
                    drawWatch.Restart();
                }

                if (checkWatch.ElapsedMilliseconds > 10000) {
                    bool anyNew = false;
                    
                    for (int i = 0; i < GROUP_COUNT; i++) {
                        var thread = threadInfo[i];
                        double best;
                        double worst;

                        lock (thread.Lock) {
                            var group = thread.Group;
                            
                            best = group[0].Fitness;
                            worst = group[CALCULATOR_COUNT - 1].Fitness;
                        }

                        if (best <= bestHistory[i])
                            continue;

                        if (!anyNew) {
                            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss"));
                            anyNew = true;
                        }

                        Console.WriteLine($"Group {i}: Generation {threadInfo[i].Generation}: {best:0.00000000} - {worst:0.00000000}");
                        bestHistory[i] = best;
                    }
                    
                    if (anyNew)
                        Console.WriteLine();
                    
                    checkWatch.Restart();
                }
            }

            foreach (var info in threadInfo)
                info.Active = false;

            form.Invoke((MethodInvoker) form.Close);
        }

        [STAThread]
        private static void FormThread(Form1 form) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(form);
        }
        
        private static void TrainGroups(ThreadInfo[] threadInfo, DataSet dataSet, Random random) =>
            Parallel.ForEach(threadInfo, info => TrainGroup(info, dataSet, new Random(random.Next() % (2 << 16))));

        private static void TrainGroup(ThreadInfo threadInfo, DataSet dataSet, Random random) {
            var group = threadInfo.Group;
            var crossGroups = new CalculatorInfo[CROSSOVERS, 3];
            
            Array.Sort(group);

            while (threadInfo.Active) {
                lock (threadInfo.Lock) {
                    CrossCalculators(group, dataSet, random, crossGroups);
                    threadInfo.Generation++;
                }
            }
        }

        private static void CrossCalculators(CalculatorInfo[] group, DataSet dataSet, Random random, CalculatorInfo[,] crossGroups) {
            int remainingCount = CALCULATOR_COUNT;
            CalculatorInfo remainingStart;
            double sumCross;
            double sumKill;

            InitLinkedList();

            for (int i = 0; i < CROSSOVERS; i++) {
                if (i == 0)
                    crossGroups[i, 0] = PopBest();
                else
                    crossGroups[i, 0] = PopRandomFit();
            }
            
            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i, 1] = PopRandomFit();
            
            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i, 2] = PopRandomUnfit();

            for (int i = 0; i < CROSSOVERS; i++) {
                var childInfo = crossGroups[i, 2];
                var childCalculator = childInfo.Calculator;

                Calculator.Cross(random, crossGroups[i, 0].Calculator, crossGroups[i, 1].Calculator, childCalculator);
                childInfo.Fitness = 0.5d * (childCalculator.CalculateCorrelation(dataSet) + 1d);
            }
            
            Array.Sort(group);

            void InitLinkedList() {
                remainingStart = group[0];

                for (int i = 0; i < CALCULATOR_COUNT; i++) {
                    var info = group[i];
                    
                    if (i < CALCULATOR_COUNT - 1)
                        info.Next = group[i + 1];
                    else
                        info.Next = null;
                }

                sumCross = 0d;
                sumKill = 0d;

                for (int i = 0; i < CALCULATOR_COUNT; i++) {
                    var info = group[i];
                    
                    if (i == 0) {
                        info.CrossChance = 1d;
                        info.KillChance = 0d;
                        sumCross++;
                        
                        continue;
                    }

                    double interp = (double) (CALCULATOR_COUNT - 1 - i) / (CALCULATOR_COUNT - 1);
                    double crossChance = (1d - interp) * MIN_CROSS_CHANCE + interp;
                    double killChance = 1d - interp + interp * MIN_KILL_CHANCE;

                    info.CrossChance = crossChance;
                    info.KillChance = killChance;
                    sumCross += crossChance;
                    sumKill += killChance;
                }
            }

            CalculatorInfo PopBest() {
                var current = remainingStart;
                var best = current;
                CalculatorInfo previous = null;
                CalculatorInfo bestPrevious = null;

                while (current != null) {
                    if (current.Fitness > best.Fitness) {
                        best = current;
                        bestPrevious = previous;
                    }

                    previous = current;
                    current = current.Next;
                }
                
                sumCross -= best.CrossChance;
                sumKill -= best.KillChance;
                remainingCount--;

                if (bestPrevious == null)
                    remainingStart = best.Next;
                else
                    bestPrevious.Next = best.Next;

                return best;
            }

            CalculatorInfo PopRandomFit() {
                double position = sumCross * random.NextDouble();
                var current = remainingStart;
                CalculatorInfo previous = null;

                while (current != null) {
                    double crossChance = current.CrossChance;

                    if (position <= crossChance) {
                        sumCross -= crossChance;
                        sumKill -= current.KillChance;
                        remainingCount--;

                        if (previous == null)
                            remainingStart = current.Next;
                        else
                            previous.Next = current.Next;

                        return current;
                    }

                    position -= crossChance;
                    previous = current;
                    current = current.Next;
                }

                return null;
            }

            CalculatorInfo PopRandomUnfit() {
                double position = sumKill * random.NextDouble();
                var current = remainingStart;
                CalculatorInfo previous = null;

                while (current != null) {
                    double killChance = current.KillChance;

                    if (position <= killChance) {
                        sumCross -= current.CrossChance;
                        sumKill -= killChance;
                        remainingCount--;

                        if (previous == null)
                            remainingStart = current.Next;
                        else
                            previous.Next = current.Next;

                        return current;
                    }

                    position -= killChance;
                    previous = current;
                    current = current.Next;
                }

                return null;
            }

            CalculatorInfo PopRandom() {
                int position = random.Next(0, remainingCount);
                var current = remainingStart;
                CalculatorInfo previous = null;

                for (int i = 0; i < position; i++) {
                    previous = current;
                    current = current.Next;
                }
                
                sumCross -= current.CrossChance;
                sumKill -= current.KillChance;
                remainingCount--;

                if (previous == null)
                    remainingStart = current.Next;
                else
                    previous.Next = current.Next;

                return current;
            }
        }

        private static void SaveGroups(CalculatorInfo[][] groups) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "results.dat"), FileMode.Create))) {
                foreach (var calculatorInfo in groups) {
                    foreach (var info in calculatorInfo) {
                        writer.Write(info.Fitness);

                        foreach (var curveWeights in info.Calculator.MetricCurveWeights) {
                            writer.Write(curveWeights.W0);
                            writer.Write(curveWeights.W1);
                            writer.Write(curveWeights.W2);
                        }
                    }
                }
            }
        }

        private static void OutputDetailedInfo(CalculatorInfo[][] groups) {
            Console.WriteLine("Best calculators");
            Console.WriteLine();
            
            foreach (var calculatorInfo in groups) {
                var best = calculatorInfo[0];

                foreach (var info in calculatorInfo) {
                    if (info.Fitness > best.Fitness)
                        best = info;
                }

                Console.WriteLine($"Fitness: {best.Fitness}");

                var metricCurveWeights = best.Calculator.MetricCurveWeights;

                for (int i = 0; i < METRIC_COUNT; i++) {
                    var weights = metricCurveWeights[i];

                    Console.WriteLine($"{METRIC_NAMES[i]}: ({weights.W0:0.00000}, {weights.W1:0.00000}, {weights.W2:0.00000}) {weights.Magnitude:0.00000}");
                }

                Console.WriteLine();
            }
        }

        private static DataSample[] GetDataSamples() {
            var cache = new Dictionary<string, DataSample>();
            string cachePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cache.txt");

            if (File.Exists(cachePath)) {
                using (var reader = new StreamReader(cachePath)) {
                    while (!reader.EndOfStream) {
                        string id = reader.ReadLine();
                        int difficultyRating = int.Parse(reader.ReadLine());
                        double[] metrics = new double[METRIC_COUNT];
                        int i = 0;

                        while (!reader.EndOfStream) {
                            string line = reader.ReadLine();

                            if (string.IsNullOrWhiteSpace(line))
                                break;

                            metrics[i] = double.Parse(line);
                            i++;
                        }

                        if (i == METRIC_COUNT)
                            cache.Add(id, new DataSample(difficultyRating, metrics));
                    }
                }
            }

            var dataSamples = new List<DataSample>();
            var ids = new List<string>();

            foreach (string path in FileHelper.GetAllSrtbs()) {
                string id = $"{path}_{File.GetLastWriteTime(path)}";

                if (cache.TryGetValue(id, out var data)) {
                    dataSamples.Add(data);
                    ids.Add(id);

                    continue;
                }

                if (!ChartData.TryCreateFromFile(path, out var chartData, Difficulty.XD) || !chartData.TrackData.ContainsKey(Difficulty.XD))
                    continue;

                var processor = new ChartProcessor(chartData.TrackData[Difficulty.XD].Notes);
                double[] metricResults = new double[METRIC_COUNT];

                for (int i = 0; i < METRIC_COUNT; i++) {
                    if (!processor.TryGetMetric(METRIC_NAMES[i], out var result))
                        continue;

                    metricResults[i] = result.GetClippedMean(result.GetQuantile(0.1f), result.GetQuantile(0.85f));
                }

                int difficultyRating = chartData.TrackData[Difficulty.XD].DifficultyRating;

                dataSamples.Add(new DataSample(difficultyRating, metricResults));
                ids.Add(id);
            }

            using (var writer = new StreamWriter(cachePath)) {
                for (int i = 0; i < dataSamples.Count; i++) {
                    var sample = dataSamples[i];

                    writer.WriteLine(ids[i]);
                    writer.WriteLine(sample.DifficultyRating);

                    foreach (double value in sample.Metrics)
                        writer.WriteLine(value);

                    writer.WriteLine();
                }
            }

            return dataSamples.ToArray();
        }

        private static CalculatorInfo[][] GetInitialGroups(DataSet dataSet, Random random) {
            var groups = new CalculatorInfo[GROUP_COUNT][];
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "results.dat");

            if (File.Exists(path)) {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open))) {
                    for (int i = 0; i < GROUP_COUNT; i++) {
                        var calculatorInfo = new CalculatorInfo[CALCULATOR_COUNT];
                        
                        for (int j = 0; j < CALCULATOR_COUNT; j++) {
                            var calculator = new Calculator(dataSet.Size, i);
                            var info = new CalculatorInfo(calculator);
                            var weights = new CurveWeights[METRIC_COUNT];

                            info.Fitness = reader.ReadDouble();

                            for (int k = 0; k < METRIC_COUNT; k++) {
                                double w0 = reader.ReadDouble();
                                double w1 = reader.ReadDouble();
                                double w2 = reader.ReadDouble();
                                
                                weights[k] = new CurveWeights(w0, w1, w2);
                            }

                            calculator.SetWeights(weights);
                            calculatorInfo[j] = info;
                        }

                        groups[i] = calculatorInfo;
                    }
                }
            }
            else {
                for (int i = 0; i < GROUP_COUNT; i++) {
                    var calculatorInfo = new CalculatorInfo[CALCULATOR_COUNT];
                    
                    for (int j = 0; j < CALCULATOR_COUNT; j++) {
                        var calculator = new Calculator(dataSet.Size, i);
                        var info = new CalculatorInfo(calculator);

                        calculator.Randomize(random, 1d);
                        info.Fitness = 0.5d * (calculator.CalculateCorrelation(dataSet) + 1d);
                        calculatorInfo[j] = info;
                    }

                    groups[i] = calculatorInfo;
                }
            }

            return groups;
        }
    }
}