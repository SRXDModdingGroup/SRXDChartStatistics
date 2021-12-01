using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChartHelper;

namespace ChartRatingTrainer {
    public class Program {
        public static readonly int POPULATION_SIZE = 8;
        
        private static readonly int CROSSOVERS = 2;

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
            
            var anchors = OutputDetailedInfo(population[0], dataSets);
            
            SaveParameters(population[0].Calculator, baseCoefficients, anchors);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static void MainThread(Individual[] population, DataSet[] dataSets, Random random, Form1 form) {
            var lastBestTime = DateTime.Now;
            var drawInfo = new DrawInfoItem[POPULATION_SIZE];
            double lastBest = population[0].Fitness;
            int generation = 0;
            var drawWatch = new Stopwatch();
            var checkWatch = new Stopwatch();
            var autoSaveWatch = new Stopwatch();
            
            for (int i = 0; i < POPULATION_SIZE; i++)
                drawInfo[i] = new DrawInfoItem();
            
            drawWatch.Start();
            checkWatch.Start();
            autoSaveWatch.Start();

            while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter) {
                Generate(population, dataSets, random);
                
                if (Form.ActiveForm == form && drawWatch.ElapsedMilliseconds > 166) {
                    double best = 0d;

                    for (int i = 0; i < POPULATION_SIZE; i++) {
                        var individual = population[i];
                        var valueCurves = individual.Calculator.ValueCurves;
                        var weightCurves = individual.Calculator.WeightCurves;
                        var item = drawInfo[i];
                        var drawValueCurves = item.ValueCurves;
                        var drawWeightCurves = item.WeightCurves;
                        double fitness = individual.Fitness;

                        item.Fitness = fitness;
                        item.Color = individual.IdColor;

                        if (fitness > best)
                            best = fitness;

                        for (int j = 0; j < Calculator.METRIC_COUNT; j++) {
                            for (int k = j; k < Calculator.METRIC_COUNT; k++)
                                drawValueCurves[j, k] = valueCurves[j, k];

                            drawWeightCurves[j] = weightCurves[j];
                        }
                    }
                    
                    form.Draw(drawInfo, best, best - 0.001d);
                    drawWatch.Restart();
                }

                if (checkWatch.ElapsedMilliseconds > 60000) {
                    double currentBest = population[0].Fitness;

                    if (currentBest <= lastBest)
                        continue;

                    Console.WriteLine($"{DateTime.Now:hh\\:mm\\:ss} Generation {generation}: {currentBest:0.00000000} (+{(currentBest - lastBest) / (DateTime.Now - lastBestTime).TotalMinutes:0.00000000} / m)");
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

        private static void Generate(Individual[] population, DataSet[] dataSets, Random random) {
            int crossCount = POPULATION_SIZE - 2 * CROSSOVERS;
            var bestStart = population[0];
            var crossStart = population[CROSSOVERS];
            var killStart = population[POPULATION_SIZE - CROSSOVERS];

            for (int i = 0; i < POPULATION_SIZE - 1; i++)
                population[i].Next = population[i + 1];

            for (int i = 0; i < CROSSOVERS; i++) {
                var parent1 = PopBest();
                var parent2 = PopRandomFit();
                var child = PopWorst();
                
                Calculator.Cross(parent1.Calculator, parent2.Calculator, child.Calculator, random);
                parent2.Fitness = child.Calculator.CalculateFitness(dataSets);
                parent2.IdColor = Color.FromArgb(
                    (int) (255d * random.NextDouble()),
                    (int) (255d * random.NextDouble()),
                    (int) (255d * random.NextDouble()));
                child.Fitness = child.Calculator.CalculateFitness(dataSets);
                child.IdColor = Color.FromArgb(
                    (int) (255d * random.NextDouble()),
                    (int) (255d * random.NextDouble()),
                    (int) (255d * random.NextDouble()));
            }

            Array.Sort(population);

            Individual PopBest() {
                var best = bestStart;

                bestStart = best.Next;

                return best;
            }

            Individual PopRandomFit() {
                int position = random.Next(0, crossCount);
                var current = crossStart;
                Individual previous = null;

                while (current != null) {
                    var next = current.Next;

                    if (position == 0 || next == null) {
                        if (previous == null)
                            crossStart = current.Next;
                        else
                            previous.Next = current.Next;

                        crossCount--;

                        return current;
                    }

                    position--;
                    previous = current;
                    current = next;
                }

                return null;
            }

            Individual PopWorst() {
                var worst = killStart;

                killStart = worst.Next;

                return worst;
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

        private static void SaveParameters(Calculator calculator, double[] baseCoefficients, List<(double, int)> anchors) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat"), FileMode.Create))) {
                for (int i = 0; i < Calculator.METRIC_COUNT; i++)
                    writer.Write(baseCoefficients[i]);
                
                calculator.SerializeNetwork(writer);
                writer.Write(anchors.Count);

                foreach ((double value, int diff) in anchors) {
                    writer.Write(value);
                    writer.Write(diff);
                }
            }
            
            Console.WriteLine("Saved parameters successfully");
            Console.WriteLine();
        }

        private static List<(double, int)> OutputDetailedInfo(Individual best, DataSet[] dataSets) {
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
            DataSet dataSet;
            ExpectedReturned[] expectedReturnedPairs;

            for (int i = 0; i < dataSets.Length; i++) {
                dataSet = dataSets[i];
                expectedReturnedPairs = dataSet.ExpectedReturnedPairs;
                
                int longestName = 0;

                calculator.CacheResults(dataSet);
                Array.Sort(expectedReturnedPairs, (a, b) => a.Index.CompareTo(b.Index));

                for (int j = 0; j < dataSet.Size; j++) {
                    int nameLength = dataSet.ChartInfo[j].Title.Length;

                    if (nameLength > longestName)
                        longestName = nameLength;
                }

                Console.WriteLine("Ordered rankings:");
                Array.Sort(expectedReturnedPairs);

                var chartInfo = dataSet.ChartInfo;

                for (int j = 0; j < dataSet.Size; j++) {
                    var pair = expectedReturnedPairs[j];
                    
                    Console.WriteLine($"{pair.ReturnedPosition:0.0000} <- {pair.Expected:0.0000} ({1d - Math.Abs(pair.ReturnedPosition - pair.Expected):0.0000}) - {chartInfo[pair.Index].Title}");
                }
                
                Console.WriteLine();
            }

            dataSet = dataSets[0];
            expectedReturnedPairs = dataSet.ExpectedReturnedPairs;
            Array.Sort(expectedReturnedPairs, (a, b) => Math.Abs(a.ReturnedPosition - a.Expected).CompareTo(Math.Abs(b.ReturnedPosition - b.Expected)));

            var anchors = new List<(double, int)> {
                (expectedReturnedPairs.Min(pair => pair.ReturnedValue), 30),
                (expectedReturnedPairs.Max(pair => pair.ReturnedValue), 75)
            };

            foreach (var pair in expectedReturnedPairs) {
                double value = pair.ReturnedValue;
                int diff = (int) Math.Round(45d * pair.Expected + 30d);

                for (int i = 0; i < anchors.Count; i++) {
                    (double otherValue, int otherDiff) = anchors[i];

                    if (value > otherValue && diff > otherDiff)
                        continue;
                    
                    if (value < otherValue && diff < otherDiff)
                        anchors.Insert(i, (value, diff));

                    break;
                }
            }

            Console.WriteLine("Anchors:");
            
            foreach ((double value, int diff) in anchors)
                Console.WriteLine($"{value:0.0000} -> {diff}");
            
            Console.WriteLine();

            return anchors;
        }

        private static DataSet[] GetDataSets() {
            var paths = new List<string>();
            string pathsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Paths.txt");

            if (!File.Exists(pathsPath))
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