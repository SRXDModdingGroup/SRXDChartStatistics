using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChartAutoRating;
using ChartHelper;
using ChartMetrics;

namespace ChartRatingTrainer {
    public class Program {
        public static readonly int POPULATION_SIZE = 16;
        public static readonly int CROSSOVERS = 2;
        
        private static readonly int KEEP_N = 8;
        private static readonly string[] METRIC_NAMES = ChartProcessor.Metrics.Select(metric => metric.Name).ToArray();

        public static void Main(string[] args) {
            var random = new Random();
            var dataSets = GetDataSets();

            foreach (var dataSet in dataSets)
                dataSet.Trim(0.9d);

            double[] baseCoefficients = DataSet.GetBaseCoefficients(dataSets);

            foreach (var dataSet in dataSets)
                dataSet.Normalize(baseCoefficients);

            var population = GetPopulation(dataSets, random);
            var form = new Form1();

            Array.Sort(population);
            Parallel.Invoke(() => MainThread(population, dataSets, random, form), () => FormThread(form));
            SavePopulation(population);
            SaveParameters(population[0].Calculator, baseCoefficients);
            OutputDetailedInfo(population[0], dataSets);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static void MainThread(Individual[] population, DataSet[] dataSets, Random random, Form1 form) {
            var lastBestTime = DateTime.Now;
            var drawInfo = new DrawInfoItem[POPULATION_SIZE];
            var crossGroups = new Individual[CROSSOVERS][];
            var randoms = new Random[CROSSOVERS];
            double lastBest = population[0].Fitness;
            int generation = 0;
            var drawWatch = new Stopwatch();
            var checkWatch = new Stopwatch();
            var autoSaveWatch = new Stopwatch();
            
            for (int i = 0; i < CROSSOVERS; i++) {
                crossGroups[i] = new Individual[3];
                randoms[i] = new Random(random.Next() % 2 << 16);
            }
            
            for (int i = 0; i < POPULATION_SIZE; i++)
                drawInfo[i] = new DrawInfoItem();
            drawWatch.Start();
            checkWatch.Start();
            autoSaveWatch.Start();

            while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter) {
                Generate(population, dataSets, randoms, crossGroups);
                
                if (Form.ActiveForm == form && drawWatch.ElapsedMilliseconds > 166) {
                    double best = 0d;

                    for (int i = 0; i < POPULATION_SIZE; i++) {
                        var individual = population[i];
                        var valueCurves = individual.Calculator.ValueCurves;
                        var weightCurves = individual.Calculator.WeightCurves;
                        var item = drawInfo[i];
                        var drawCurves = item.Curves;
                        double fitness = individual.Fitness;

                        item.Fitness = fitness;
                        item.Color = individual.IdColor;

                        if (fitness > best)
                            best = fitness;

                        for (int k = 0; k < Calculator.METRIC_COUNT; k++) {
                            for (int l = k; l < Calculator.METRIC_COUNT; l++)
                                drawCurves[k, l] = valueCurves[k, l];

                            drawCurves[k, Calculator.METRIC_COUNT] = weightCurves[k];
                        }
                    }
                    
                    form.Draw(drawInfo, best, best - 0.001d);
                    drawWatch.Restart();
                }

                if (checkWatch.ElapsedMilliseconds > 5000) {
                    double currentBest = population[0].Fitness;

                    if (currentBest <= lastBest)
                        continue;

                    Console.WriteLine($"{DateTime.Now:hh\\:mm\\:ss} Generation {generation}: {currentBest:0.00000000} (+{(currentBest - lastBest) / (DateTime.Now - lastBestTime).TotalSeconds:0.00000000} / s)");
                    lastBest = currentBest;
                    lastBestTime = DateTime.Now;
                    checkWatch.Restart();
                }

                if (autoSaveWatch.ElapsedMilliseconds > 300000) {
                    SavePopulation(population);
                    autoSaveWatch.Restart();
                }

                generation++;
            }

            form.Invoke((MethodInvoker) form.Close);
        }

        [STAThread]
        private static void FormThread(Form1 form) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(form);
        }

        private static void Generate(Individual[] population, DataSet[] dataSets, Random[] randoms, Individual[][] crossGroups) {
            Individual crossStart;
            Individual killStart;
            var random = randoms[0];
            double sumCross;
            double sumKill;

            InitLinkedList();

            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i][0] = PopRandomFit();

            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i][1] = PopRandomFit();
            
            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i][2] = PopRandomUnfit();

            for (int i = 0; i < CROSSOVERS; i++)
                Cross(crossGroups[i], dataSets, randoms[i]);

            Array.Sort(population);

            void InitLinkedList() {
                crossStart = population[0];
                killStart = population[KEEP_N];

                for (int i = 0; i < POPULATION_SIZE; i++) {
                    var info = population[i];
                    
                    if (i < POPULATION_SIZE - 1)
                        info.Next = population[i + 1];
                    else
                        info.Next = null;
                }

                sumCross = 0d;
                sumKill = 0d;

                for (int i = 0; i < POPULATION_SIZE; i++) {
                    var info = population[i];
                    double crossChance;
                    double killChance;
                    
                    if (i < KEEP_N)
                        killChance = 0d;
                    else
                        killChance = 1d;

                    crossChance = 1d - (double) i / POPULATION_SIZE;
                    info.CrossChance = crossChance;
                    info.KillChance = killChance;
                    sumCross += crossChance;
                    sumKill += killChance;
                }
            }

            Individual PopRandomFit() {
                double position = sumCross * random.NextDouble();
                var current = crossStart;
                Individual previous = null;

                while (current != null) {
                    double crossChance = current.CrossChance;
                    var next = current.Next;

                    if (position < crossChance || next == null) {
                        sumCross -= crossChance;
                        sumKill -= current.KillChance;

                        if (previous == null)
                            crossStart = current.Next;
                        else
                            previous.Next = current.Next;

                        if (current == killStart)
                            killStart = current.Next;

                        return current;
                    }

                    position -= crossChance;
                    previous = current;
                    current = next;
                }

                return null;
            }

            Individual PopRandomUnfit() {
                double position = sumKill * random.NextDouble();
                var current = killStart;
                Individual previous = null;

                while (current != null) {
                    double killChance = current.KillChance;
                    var next = current.Next;

                    if (position < killChance || next == null) {
                        sumCross -= current.CrossChance;
                        sumKill -= killChance;

                        if (previous == null)
                            crossStart = current.Next;
                        else
                            previous.Next = current.Next;
                        
                        if (current == killStart)
                            killStart = current.Next;

                        return current;
                    }

                    position -= killChance;
                    previous = current;
                    current = next;
                }

                return null;
            }
        }

        private static void Cross(Individual[] crossGroup, DataSet[] dataSets, Random random) {
            var child = crossGroup[2];
            var childCalculator = child.Calculator;

            Calculator.Cross(crossGroup[0].Calculator, crossGroup[1].Calculator, childCalculator, random);
            child.Fitness = childCalculator.CalculateFitness(dataSets);
        }

        private static void OutputDetailedInfo(Individual best, DataSet[] dataSets) {
            Console.WriteLine();
            Console.WriteLine($"Fitness: {best.Fitness:0.00000000}");
            Console.WriteLine("Value magnitudes:");

            var valueCurves = best.Calculator.ValueCurves;
            var weightCurves = best.Calculator.WeightCurves;

            for (int i = 0; i < Calculator.METRIC_COUNT; i++) {
                for (int j = 0; j < Calculator.METRIC_COUNT; j++) {
                    if (j >= i)
                        Console.Write($"{valueCurves[i, j].Magnitude:0.0000} ");
                    else
                        Console.Write("       ");
                }
                
                Console.WriteLine($"   {weightCurves[i].Magnitude:0.0000}");
            }
            
            Console.WriteLine();

            var calculator = best.Calculator;

            foreach (var dataSet in dataSets) {
                var resultsByValue = new Result[dataSet.Size];
                int longestName = 0;

                calculator.CacheResults(dataSet);

                for (int i = 0; i < dataSet.Size; i++) {
                    double correlation = dataSet.ResultsArray2[i] - dataSet.PositionValues[i];

                    correlation = 1d / (correlation * correlation + 1d);
                    resultsByValue[i] = new Result(
                        dataSet.RelevantChartInfo[i].Title,
                        dataSet.RelevantChartInfo[i].DifficultyRating,
                        dataSet.ResultsArray2[i],
                        correlation);

                    int nameLength = dataSet.RelevantChartInfo[i].Title.Length;

                    if (nameLength > longestName)
                        longestName = nameLength;
                }

                var resultsByRating = new Result[dataSet.Size];
                var resultsByCorrelation = new Result[dataSet.Size];
                
                Array.Copy(resultsByValue, resultsByRating, dataSet.Size);
                Array.Copy(resultsByValue, resultsByCorrelation, dataSet.Size);
                Array.Sort(resultsByValue, (a, b) => a.Value.CompareTo(b.Value));
                Array.Sort(resultsByRating, (a, b) => a.DifficultyRating.CompareTo(b.DifficultyRating));
                Array.Sort(resultsByCorrelation, (a, b) => a.Correlation.CompareTo(b.Correlation));
                Console.WriteLine("Ordered rankings:");

                for (int i = 0; i < dataSet.Size; i++) {
                    var result = resultsByValue[i];
                    
                    Console.Write($"{i.ToString().PadLeft(3)} <- {Array.IndexOf(resultsByRating, result).ToString().PadLeft(3)} ({result.Correlation:0.0000}) - {result.Title.PadRight(longestName)}\t");

                    result = resultsByRating[i];
                    
                    Console.Write($"{Array.IndexOf(resultsByValue, result).ToString().PadLeft(3)} <- {i.ToString().PadLeft(3)} ({result.Correlation:0.0000}) - {result.Title.PadRight(longestName)}\t");
                    
                    result = resultsByCorrelation[i];
                    
                    Console.WriteLine($"{Array.IndexOf(resultsByValue, result).ToString().PadLeft(3)} <- {Array.IndexOf(resultsByRating, result).ToString().PadLeft(3)} ({result.Correlation:0.0000}) - {result.Title}");
                }
                
                Console.WriteLine();
            }
        }

        private readonly struct Result {
            public string Title { get; }
            
            public int DifficultyRating { get; }

            public double Value { get; }
            
            public double Correlation { get; }

            public Result(string title, int difficultyRating, double value, double correlation) {
                Title = title;
                DifficultyRating = difficultyRating;
                Value = value;
                Correlation = correlation;
            }
        }

        private static void SavePopulation(Individual[] population) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "results.dat"), FileMode.Create))) {
                foreach (var individual in population)
                    individual.Calculator.Serialize(writer);
            }
            
            Console.WriteLine();
            Console.WriteLine("Saved file successfully");
            Console.WriteLine();
        }

        private static void SaveParameters(Calculator calculator, double[] baseCoefficients) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat"), FileMode.Create))) {
                for (int i = 0; i < Calculator.METRIC_COUNT; i++)
                    writer.Write(baseCoefficients[i]);
                
                calculator.SerializeNetwork(writer);
            }
            
            Console.WriteLine("Saved parameters successfully");
            Console.WriteLine();
        }

        private static DataSet[] GetDataSets() {
            var paths = new List<string>();
            string pathsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Paths.txt");

            if (!Directory.Exists(pathsPath))
                return new[] { new DataSet(FileHelper.CustomPath) };
            
            using (var reader = new StreamReader(pathsPath)) {
                while (!reader.EndOfStream)
                    paths.Add(reader.ReadLine());
            }

            var dataSets = new DataSet[paths.Count];

            for (int i = 0; i < paths.Count; i++)
                dataSets[i] = new DataSet(paths[i]);

            return dataSets;
        }

        private static Individual[] GetPopulation(DataSet[] dataSets, Random random) {
            var population = new Individual[POPULATION_SIZE];
            var calculators = new Calculator[POPULATION_SIZE];
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "results.dat");

            if (File.Exists(path)) {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open))) {
                    for (int i = 0; i < POPULATION_SIZE; i++)
                        calculators[i] = Calculator.Deserialize(reader);
                }
            }
            else {
                for (int i = 0; i < POPULATION_SIZE; i++)
                    calculators[i] = Calculator.Random(random);
            }

            for (int i = 0; i < POPULATION_SIZE; i++) {
                var calculator = calculators[i];
                var individual = new Individual(calculator, Color.FromArgb(
                    (int) (255d * random.NextDouble()),
                    (int) (255d * random.NextDouble()),
                    (int) (255d * random.NextDouble())));
                
                individual.Fitness = calculator.CalculateFitness(dataSets);
                population[i] = individual;
            }

            return population;
        }
    }
}