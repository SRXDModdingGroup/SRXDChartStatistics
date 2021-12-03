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
        public static readonly int POPULATION_SIZE = 16;
        
        private static readonly int CROSSOVERS = 2;

        public static void Main(string[] args) {
            var random = new Random();
            var dataSets = GetDataSets();

            foreach (var dataSet in dataSets)
                dataSet.Trim(0.975d);

            double[] baseCoefficients = DataSet.GetBaseCoefficients(dataSets);

            foreach (var dataSet in dataSets)
                dataSet.Normalize(baseCoefficients);

            var population = GetPopulation(dataSets, random);
            var form = new Form1();

            Array.Sort(population);
            Parallel.Invoke(() => MainThread(population, dataSets, random, form), () => FormThread(form));
            SavePopulation(population);
            OutputDetailedInfo(population[0], dataSets);
            SaveParameters(population[0].Calculator, baseCoefficients, dataSets[0].Bias, dataSets[0].Scale);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static void MainThread(Individual[] population, DataSet[] dataSets, Random random, Form1 form) {
            var lastBestTime = DateTime.Now;
            var drawInfo = new DrawInfoItem[POPULATION_SIZE];
            var drawExpectedReturned = new PointF[dataSets.Sum(dataSet => dataSet.Size)];
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
                    double best = population[0].Fitness;

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

                        for (int j = 0; j < Calculator.METRIC_COUNT; j++) {
                            for (int k = j; k < Calculator.METRIC_COUNT; k++)
                                drawValueCurves[j, k] = valueCurves[j, k];

                            drawWeightCurves[j] = weightCurves[j];
                        }
                    }

                    var calculator = population[0].Calculator;
                    int l = 0;

                    foreach (var dataSet in dataSets) {
                        calculator.CacheResults(dataSet);
                        
                        foreach (var expectedReturned in dataSet.ExpectedReturned) {
                            drawExpectedReturned[l] = new PointF(
                                (float) expectedReturned.Expected,
                                (float) expectedReturned.Returned);
                            l++;
                        }
                    }

                    form.Draw(drawInfo, drawExpectedReturned, best, best - 0.01d);
                    drawWatch.Restart();
                }

                if (checkWatch.ElapsedMilliseconds > 60000) {
                    double currentBest = population[0].Fitness;

                    Console.WriteLine($"{DateTime.Now:hh\\:mm\\:ss} Generation {generation}: {currentBest:0.00000000} (+{(currentBest - lastBest) / (DateTime.Now - lastBestTime).TotalMinutes:0.00000000} / m)");
                    
                    if (currentBest < lastBest)
                        Console.WriteLine();
                    
                    if (currentBest > lastBest) {
                        lastBest = currentBest;
                        lastBestTime = DateTime.Now;
                    }
                    
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
            var bestStart = population[0];
            var crossStart = population[CROSSOVERS];
            var killStart = population[POPULATION_SIZE - CROSSOVERS];
            double crossSum = 0d;

            for (int i = 0; i < POPULATION_SIZE - 1; i++) {
                population[i].Next = population[i + 1];

                if (i <= CROSSOVERS || i >= POPULATION_SIZE - CROSSOVERS)
                    continue;
                
                double crossChance = 1d - (double) (i - CROSSOVERS) / (POPULATION_SIZE - 2 * CROSSOVERS);

                population[i].CrossChance = crossChance;
                crossSum += crossChance;
            }

            for (int i = 0; i < CROSSOVERS; i++) {
                var parent1 = PopBest();
                var parent2 = PopRandomFit();
                var child = PopWorst();
                
                Calculator.Cross(parent1.Calculator, parent2.Calculator, child.Calculator, random);
                parent2.Fitness = child.Calculator.CalculateFitness(dataSets);
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
                double position = crossSum * random.NextDouble();
                var current = crossStart;
                Individual previous = null;

                while (current != null) {
                    var next = current.Next;

                    if (position < current.CrossChance || next == null) {
                        if (previous == null)
                            crossStart = current.Next;
                        else
                            previous.Next = current.Next;

                        crossSum -= current.CrossChance;

                        return current;
                    }

                    position -= current.CrossChance;
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

        private static void SaveParameters(Calculator calculator, double[] baseCoefficients, double bias, double scale) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat"), FileMode.Create))) {
                for (int i = 0; i < Calculator.METRIC_COUNT; i++)
                    writer.Write(baseCoefficients[i]);
                
                calculator.Serialize(writer);
                
                writer.Write(bias);
                writer.Write(scale);
            }
            
            Console.WriteLine("Saved parameters successfully");
            Console.WriteLine();
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
            var dataSet = dataSets[0];
            var expectedReturnedPairs = dataSet.ExpectedReturned;
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
                    
                Console.WriteLine($"{pair.Returned:0.0000} <- {pair.Expected:0.0000} ({1d - Math.Abs(pair.Returned - pair.Expected):0.0000}) - {chartInfo[pair.Index].Title}");
            }
                
            Console.WriteLine();
            Console.WriteLine("Correlation:");
            Array.Sort(expectedReturnedPairs, (a, b) => (1d - Math.Abs(a.Returned - a.Expected)).CompareTo(1d - Math.Abs(b.Returned - b.Expected)));

            for (int j = 0; j < dataSet.Size; j++) {
                var pair = expectedReturnedPairs[j];
                    
                Console.WriteLine($"{pair.Returned:0.0000} <- {pair.Expected:0.0000} ({1d - Math.Abs(pair.Returned - pair.Expected):0.0000}) - {chartInfo[pair.Index].Title}");
            }
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
            int count = 0;

            if (File.Exists(path)) {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open))) {
                    for (int i = 0; i < POPULATION_SIZE && reader.BaseStream.Position < reader.BaseStream.Length; i++) {
                        calculators[i] = Calculator.Deserialize(reader);
                        count++;
                    }
                }
            }
            
            for (int i = count; i < POPULATION_SIZE; i++)
                calculators[i] = Calculator.Random(random);

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