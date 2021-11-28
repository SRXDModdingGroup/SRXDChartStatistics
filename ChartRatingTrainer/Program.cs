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

namespace ChartRatingTrainer {
    public class Program {
        public static readonly int POPULATION_SIZE = 32;
        public static readonly int CROSSOVERS = 8;
        
        private static readonly int KEEP_N = 8;
        private static readonly string[] METRIC_NAMES = ChartProcessor.Metrics.Select(metric => metric.Name).ToArray();

        public static void Main(string[] args) {
            var random = new Random();
            var dataSets = GetDataSets();

            foreach (var dataSet in dataSets)
                dataSet.Trim(0.95d);

            double[] baseCoefficients = DataSet.GetBaseCoefficients(dataSets);

            foreach (var dataSet in dataSets)
                dataSet.Normalize(baseCoefficients);

            var population = GetPopulation(dataSets, random);
            var form = new Form1();

            Array.Sort(population);
            Parallel.Invoke(() => MainThread(population, dataSets, random, form), () => FormThread(form));
            SavePopulation(population);
            SaveParameters(population[0].Calculator, baseCoefficients);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static void MainThread(Individual[] population, DataSet[] dataSets, Random random, Form1 form) {
            var lastBestTime = DateTime.Now;
            var drawInfo = new DrawInfoItem[POPULATION_SIZE];
            var crossGroups = new Individual[CROSSOVERS][];
            var randoms = new Random[CROSSOVERS];

            for (int i = 0; i < CROSSOVERS; i++) {
                crossGroups[i] = new Individual[3];
                randoms[i] = new Random(random.Next() % 2 << 16);
            }
            
            double lastBest = population[0].Fitness;
            int generation = 0;

            for (int i = 0; i < POPULATION_SIZE; i++)
                drawInfo[i] = new DrawInfoItem();

            var drawWatch = new Stopwatch();
            var checkWatch = new Stopwatch();
            var autoSaveWatch = new Stopwatch();

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

                        if (fitness > best)
                            best = fitness;

                        for (int k = 0; k < Calculator.METRIC_COUNT; k++) {
                            for (int l = k; l < Calculator.METRIC_COUNT; l++)
                                drawCurves[k, l] = valueCurves[k, l];

                            drawCurves[k, Calculator.METRIC_COUNT] = weightCurves[k];
                        }
                    }
                    
                    form.Draw(drawInfo, best, best - 0.02d);
                    drawWatch.Restart();
                }

                if (checkWatch.ElapsedMilliseconds > 60000) {
                    double currentBest = population[0].Fitness;

                    if (currentBest <= lastBest)
                        continue;

                    Console.WriteLine($"{DateTime.Now:hh\\:mm\\:ss} Generation {generation}: {currentBest:0.00000000} ({0.5d * (population[0].Calculator.CalculateCorrelation(dataSets, 0) + 1d):0.00000000}) (+{currentBest - lastBest:0.00000000} in {DateTime.Now - lastBestTime:hh\\:mm\\:ss})");
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
            double sumCross;
            double sumKill;
            var random = randoms[0];

            InitLinkedList();

            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i][0] = PopRandomFit();

            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i][1] = PopRandomFit();
            
            for (int i = 0; i < CROSSOVERS; i++)
                crossGroups[i][2] = PopRandomUnfit();

            Parallel.For(0, CROSSOVERS, i => Cross(crossGroups[i], dataSets, randoms[i], i));
            Array.Sort(population);

            void InitLinkedList() {
                crossStart = population[0];

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

                    if (i < KEEP_N) {
                        crossChance = 1d;
                        killChance = 0d;
                    }
                    else {
                        killChance = (double) (i - KEEP_N + 1) / (POPULATION_SIZE - KEEP_N + 1);
                        killChance = killChance * killChance * (3d - 2d * killChance);
                        crossChance = 1d - killChance;
                    }

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
                var current = crossStart;
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

                        return current;
                    }

                    position -= killChance;
                    previous = current;
                    current = next;
                }

                return null;
            }
        }

        private static void Cross(Individual[] crossGroup, DataSet[] dataSets, Random random, int threadIndex) {
            var childInfo = crossGroup[2];
            var childCalculator = childInfo.Calculator;

            Calculator.Cross(crossGroup[0].Calculator, crossGroup[1].Calculator, childCalculator, random);
            childInfo.Fitness = childCalculator.CalculateFitness(dataSets, threadIndex);
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
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "results.dat");

            if (File.Exists(path)) {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open))) {
                    for (int i = 0; i < POPULATION_SIZE; i++) {
                        var calculator = Calculator.Deserialize(i, reader);
                        var individual = new Individual(calculator);
                            
                        individual.Fitness = calculator.CalculateFitness(dataSets, 0);
                        population[i] = individual;
                    }
                }
            }
            else {
                for (int i = 0; i < POPULATION_SIZE; i++) {
                    var calculator = Calculator.Random(i, random);
                    var individual = new Individual(calculator);

                    individual.Fitness = calculator.CalculateFitness(dataSets, 0);
                    population[i] = individual;
                }
            }

            return population;
        }
    }
}