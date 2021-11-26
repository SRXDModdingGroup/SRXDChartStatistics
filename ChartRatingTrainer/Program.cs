using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChartHelper;
using ChartMetrics;

namespace ChartRatingTrainer {
    public class Program {
        public static readonly int METRIC_COUNT = ChartProcessor.Metrics.Count;
        public static readonly int CALCULATOR_COUNT = 32;
        public static readonly int GROUP_COUNT = 8;
        
        private static readonly int CROSSOVERS = 8;
        private static readonly int KEEP_N = 4;
        private static readonly double MIN_CROSS_CHANCE = 0.125f;
        private static readonly double MIN_KILL_CHANCE = 0.0625f;
        private static readonly string[] METRIC_NAMES = ChartProcessor.Metrics.Select(metric => metric.Name).ToArray();

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

        private readonly struct CacheInfo {
            public RelevantChartInfo ChartInfo { get; }
            
            public double[] Metrics { get; }

            public CacheInfo(RelevantChartInfo chartInfo, double[] metrics) {
                ChartInfo = chartInfo;
                Metrics = metrics;
            }
        }

        public static void Main(string[] args) {
            var random = new Random();
            var dataSet = GetDataSet();
            double[] baseCoefficients = DataSet.Normalize(dataSet);
            var groups = GetInitialGroups(dataSet, random);
            var threadInfo = groups.Select(group => new ThreadInfo(group)).ToArray();
            var form = new Form1();

            Parallel.Invoke(() => MainThread(threadInfo, form), () => FormThread(form), () => TrainGroups(threadInfo, dataSet, random));
            SaveGroups(threadInfo);
            OutputDetailedInfo(groups);
            SaveParameters(groups, dataSet, baseCoefficients);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static void MainThread(ThreadInfo[] threadInfo, Form1 form) {
            double[] bestHistory = new double[GROUP_COUNT];
            var lastBestTime = new DateTime[GROUP_COUNT];

            for (int i = 0; i < GROUP_COUNT; i++) {
                bestHistory[i] = threadInfo[i].Group[0].Fitness;
                lastBestTime[i] = DateTime.Now;
            }

            var drawInfo = new DrawInfoItem[GROUP_COUNT][];

            for (int i = 0; i < GROUP_COUNT; i++) {
                var group = new DrawInfoItem[CALCULATOR_COUNT];
                
                for (int j = 0; j < CALCULATOR_COUNT; j++)
                    group[j] = new DrawInfoItem();

                drawInfo[i] = group;
            }

            var drawWatch = new Stopwatch();
            var checkWatch = new Stopwatch();
            var autoSaveWatch = new Stopwatch();

            drawWatch.Start();
            checkWatch.Start();
            autoSaveWatch.Start();

            while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter) {
                if (Form.ActiveForm == form && drawWatch.ElapsedMilliseconds > 66) {
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
                        double best = threadInfo[i].Group[0].Fitness;

                        if (best <= bestHistory[i])
                            continue;

                        if (!anyNew) {
                            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss"));
                            anyNew = true;
                        }

                        Console.WriteLine($"Group {i}: Generation {threadInfo[i].Generation}: {best:0.00000000} (+{best - bestHistory[i]:0.00000000} in {(DateTime.Now - lastBestTime[i]):hh\\:mm\\:ss})");
                        bestHistory[i] = best;
                        lastBestTime[i] = DateTime.Now;
                    }
                    
                    if (anyNew)
                        Console.WriteLine();
                    
                    checkWatch.Restart();
                }

                if (autoSaveWatch.ElapsedMilliseconds > 300000) {
                    SaveGroups(threadInfo);
                    autoSaveWatch.Restart();
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
            CalculatorInfo remainingStart;
            double sumCross;
            double sumKill;

            InitLinkedList();

            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i, 0] = PopRandomFit();

            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i, 1] = PopRandomFit();
            
            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i, 2] = PopRandomUnfit();

            for (int i = 0; i < CROSSOVERS; i++) {
                var childInfo = crossGroups[i, 2];
                var childCalculator = childInfo.Calculator;

                Calculator.Cross(random, crossGroups[i, 0].Calculator, crossGroups[i, 1].Calculator, childCalculator);
                childInfo.Fitness = childCalculator.CalculateFitness(dataSet);
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

                    double interp = (double) i / (CALCULATOR_COUNT - 1);
                    double crossChance = 1d - interp + interp * MIN_CROSS_CHANCE;
                    double killChance;

                    if (i < KEEP_N) {
                        killChance = 0d;
                        info.Keep = true;
                    }
                    else {
                        interp = (double) (i - KEEP_N) / (CALCULATOR_COUNT - 1 - KEEP_N);
                        killChance = (1d - interp) * MIN_KILL_CHANCE + interp;
                        info.Keep = false;
                    }

                    info.CrossChance = crossChance;
                    info.KillChance = killChance;
                    sumCross += crossChance;
                    sumKill += killChance;
                }
            }

            CalculatorInfo PopRandomFit() {
                double position = sumCross * random.NextDouble();
                var current = remainingStart;
                CalculatorInfo previous = null;

                while (current != null) {
                    double crossChance = current.CrossChance;

                    if (position < crossChance) {
                        sumCross -= crossChance;
                        sumKill -= current.KillChance;

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

                    if (!current.Keep && position < killChance) {
                        sumCross -= current.CrossChance;
                        sumKill -= killChance;

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
        }

        private static void SaveGroups(ThreadInfo[] threadInfo) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "results.dat"), FileMode.Create))) {
                foreach (var thread in threadInfo) {
                    lock (thread.Lock) {
                        foreach (var info in thread.Group) {
                            foreach (var curveWeights in info.Calculator.MetricCurveWeights) {
                                writer.Write(curveWeights.W0);
                                writer.Write(curveWeights.W1);
                                writer.Write(curveWeights.W2);
                            }
                        }
                    }
                }
            }
            
            Console.WriteLine("Saved file successfully");
            Console.WriteLine();
        }

        private static void SaveParameters(CalculatorInfo[][] groups, DataSet dataSet, double[] baseCoefficients) {
            var best = groups[0][0];

            for (int i = 1; i < groups.Length; i++) {
                var info = groups[i][0];

                if (info.Fitness > best.Fitness)
                    best = info;
            }

            var calculator = best.Calculator;
            
            Console.WriteLine("Anchors:");
            Console.WriteLine();

            foreach (var group in calculator.CalculateAnchors(dataSet).GroupBy(a => a.To / 5).OrderBy(g => g.Key)) {
                foreach (var anchor in group)
                    Console.WriteLine($"{anchor.From} -> {anchor.To} ({anchor.Correlation:0.0000})");
                
                Console.WriteLine();
            }

            var metricCurveWeights = calculator.MetricCurveWeights;
            
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat"), FileMode.Create))) {
                for (int i = 0; i < METRIC_COUNT; i++) {
                    var coefficients = new Coefficients(metricCurveWeights[i]);

                    writer.Write(baseCoefficients[i]);
                    writer.Write(coefficients.X1);
                    writer.Write(coefficients.X2);
                    writer.Write(coefficients.X3);
                    
                    Console.WriteLine($"{METRIC_NAMES[i]}: Base {baseCoefficients[i]:0.000000} ({coefficients.X1:0.000000}, {coefficients.X2:0.000000}, {coefficients.X3:0.000000}) {coefficients.Magnitude:0.000000}");
                }
                
                Console.WriteLine();
            }

            Console.WriteLine("Saved parameters successfully");
            Console.WriteLine();
        }

        private static void OutputDetailedInfo(CalculatorInfo[][] groups) {
            Console.WriteLine("Best calculators");
            Console.WriteLine();
            
            foreach (var group in groups.OrderBy(group => group[0].Fitness)) {
                var best = group[0];

                Console.WriteLine($"Fitness: {best.Fitness}");

                var metricCurveWeights = best.Calculator.MetricCurveWeights;

                for (int i = 0; i < METRIC_COUNT; i++) {
                    var weights = metricCurveWeights[i];

                    Console.WriteLine($"{METRIC_NAMES[i]}: ({weights.W0:0.00000}, {weights.W1:0.00000}, {weights.W2:0.00000}) {weights.Magnitude:0.00000}");
                }

                Console.WriteLine();
            }
        }

        private static DataSet GetDataSet() {
            var cache = new Dictionary<string, CacheInfo>();
            string cachePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cache.txt");

            if (File.Exists(cachePath)) {
                using (var reader = new StreamReader(cachePath)) {
                    while (!reader.EndOfStream) {
                        string id = reader.ReadLine();
                        string title = reader.ReadLine();
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
                            cache.Add(id, new CacheInfo(new RelevantChartInfo(title, difficultyRating), metrics));
                    }
                }
            }

            var ratings = new Dictionary<string, int>();
            var regex = new Regex(@"(\d+)\t(.*)");

            using (var reader = new StreamReader(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Ratings.txt"))) {
                while (!reader.EndOfStream) {
                    string line = reader.ReadLine();
                    var match = regex.Match(line);

                    if (match.Success)
                        ratings.Add(match.Groups[2].ToString(), int.Parse(match.Groups[1].ToString()));
                }
            }

            var chartInfo = new List<RelevantChartInfo>();
            var metricList = new List<double[]>();
            var ids = new List<string>();

            foreach (string path in FileHelper.GetAllSrtbs()) {
                string id = $"{path}_{File.GetLastWriteTime(path)}";
                RelevantChartInfo newChartInfo;
                double[] metrics;

                if (cache.TryGetValue(id, out var data)) {
                    newChartInfo = data.ChartInfo;
                    metrics = data.Metrics;
                }
                else if (!ChartData.TryCreateFromFile(path, out var chartData, Difficulty.XD) && chartData.TrackData.ContainsKey(Difficulty.XD))
                    continue;
                else {
                    var processor = new ChartProcessor(chartData.Title, chartData.TrackData[Difficulty.XD].Notes);

                    metrics = new double[METRIC_COUNT];

                    for (int i = 0; i < METRIC_COUNT; i++) {
                        if (!processor.TryGetMetric(METRIC_NAMES[i], out var result))
                            continue;

                        metrics[i] = result.GetClippedMean(result.GetQuantile(0.1f), result.GetQuantile(0.85f));
                    }

                    newChartInfo = new RelevantChartInfo(chartData.Title, ratings[chartData.Title]);
                }

                chartInfo.Add(newChartInfo);
                metricList.Add(metrics);
                ids.Add(id);
            }

            using (var writer = new StreamWriter(cachePath)) {
                for (int i = 0; i < chartInfo.Count; i++) {
                    var info = chartInfo[i];

                    writer.WriteLine(ids[i]);
                    writer.WriteLine(info.Title);
                    writer.WriteLine(info.DifficultyRating);

                    foreach (double value in metricList[i])
                        writer.WriteLine(value);

                    writer.WriteLine();
                }
            }

            return new DataSet(chartInfo.ToArray(), metricList.ToArray());
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

                            for (int k = 0; k < METRIC_COUNT; k++) {
                                double w0 = reader.ReadDouble();
                                double w1 = reader.ReadDouble();
                                double w2 = reader.ReadDouble();
                                
                                weights[k] = new CurveWeights(w0, w1, w2);
                            }

                            calculator.SetWeights(weights);
                            info.Fitness = calculator.CalculateFitness(dataSet);
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
                        info.Fitness = calculator.CalculateFitness(dataSet);
                        calculatorInfo[j] = info;
                    }

                    groups[i] = calculatorInfo;
                }
            }

            return groups;
        }
    }
}