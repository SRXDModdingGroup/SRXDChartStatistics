using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ChartHelper;
using ChartMetrics;

namespace ChartAutoRating {
    internal class Program {
        public static readonly int METRIC_COUNT = ChartProcessor.Metrics.Count;

        private static readonly int CALCULATOR_COUNT = 32;
        private static readonly int GROUP_COUNT = 8;
        private static readonly int CROSSOVERS = 8;
        private static readonly string[] METRIC_NAMES = ChartProcessor.Metrics.Select(metric => metric.Name).ToArray();

        private class CalculatorInfo : IComparable<CalculatorInfo> {
            public Calculator Calculator { get; }
            
            public double Fitness { get; set; }
            
            public double CrossChance { get; set; }
            
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

        public static void Main(string[] args) {
            var random = new Random();
            var dataSet = new DataSet(GetDataSamples());
            double[] baseCoefficients = DataSet.Normalize(dataSet);
            var groups = GetInitialGroups(dataSet, random);
            var threadInfo = groups.Select(group => new ThreadInfo(group)).ToArray();

            Parallel.Invoke(() => ConsoleLoop(threadInfo), () => TrainGroups(threadInfo, dataSet, random));
            SaveGroups(groups);
            OutputDetailedInfo(groups);
        }

        private static void ConsoleLoop(ThreadInfo[] threadInfo) {
            var stopwatch = new Stopwatch();
            
            stopwatch.Start();

            while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter) {
                if (stopwatch.ElapsedMilliseconds < 1000)
                    continue;
                
                for (int i = 0; i < GROUP_COUNT; i++) {
                    var thread = threadInfo[i];
                    
                    lock (thread.Lock) {
                        var group = thread.Group;
                        var best = group[0];
                        var worst = group[0];
                        
                        foreach (var info in group) {
                            if (info.Fitness > best.Fitness)
                                best = info;

                            if (info.Fitness < worst.Fitness)
                                worst = info;
                        }
                        
                        Console.WriteLine($"Group {i}: Generation {threadInfo[i].Generation} {best.Fitness:0.00000000} - {worst.Fitness:0.00000000}");
                    }
                }
                
                Console.WriteLine();
                stopwatch.Restart();
            }

            foreach (var info in threadInfo)
                info.Active = false;
        }
        
        private static void TrainGroups(ThreadInfo[] threadInfo, DataSet dataSet, Random random) =>
            Parallel.ForEach(threadInfo, info => TrainGroup(info, dataSet, new Random(random.Next() % (2 << 16))));

        private static void TrainGroup(ThreadInfo threadInfo, DataSet dataSet, Random random) {
            var group = threadInfo.Group;

            while (threadInfo.Active) {
                lock (threadInfo.Lock) {
                    CrossCalculators(group, dataSet, random);
                    threadInfo.Generation++;
                }
            }
        }

        private static void CrossCalculators(CalculatorInfo[] group, DataSet dataSet, Random random) {
            CalculatorInfo remainingStart;
            double sumCross;
            double sumKill;

            InitLinkedList();

            for (int j = 0; j < CROSSOVERS; j++) {
                var firstInfo = PopRandomFit();
                var secondInfo = PopRandomFit();
                var childInfo = PopRandomUnfit();
                var childCalculator = childInfo.Calculator;

                Calculator.Cross(random, firstInfo.Calculator, secondInfo.Calculator, childCalculator);
                childInfo.Fitness = 0.5d * (childCalculator.CalculateCorrelation(dataSet) + 1d);
            }
            
            void InitLinkedList() {
                    double min = double.PositiveInfinity;
                    double max = 0d;

                    remainingStart = group[0];

                    for (int j = 0; j < CALCULATOR_COUNT; j++) {
                        var info = group[j];
                        double fitness = info.Fitness;

                        if (fitness < min)
                            min = fitness;

                        if (fitness > max)
                            max = fitness;

                        if (j < CALCULATOR_COUNT - 1)
                            info.Next = group[j + 1];
                        else
                            info.Next = null;
                    }

                    sumCross = 0d;
                    sumKill = 0d;

                    double scale = 1d / (max - min);

                    foreach (var info in group) {
                        double crossChance = scale * (info.Fitness - min);

                        info.CrossChance = crossChance;
                        sumCross += crossChance;
                    }

                    sumKill = CALCULATOR_COUNT - sumCross;
                }

                CalculatorInfo PopRandomFit() {
                    double position = sumCross * random.NextDouble();
                    var current = remainingStart;
                    CalculatorInfo previous = null;

                    while (current != null) {
                        double crossChance = current.CrossChance;

                        if (position <= crossChance) {
                            sumCross -= crossChance;
                            sumKill -= 1d - crossChance;

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

                    return previous;
                }

                CalculatorInfo PopRandomUnfit() {
                    double position = sumKill * random.NextDouble();
                    var current = remainingStart;
                    CalculatorInfo previous = null;

                    while (current != null) {
                        double crossChance = current.CrossChance;
                        double killChance = 1d - crossChance;

                        if (position <= killChance) {
                            sumCross -= crossChance;
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

                    return previous;
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