using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChartHelper;
using ChartHelper.Parsing;
using ChartHelper.Types;
using ChartMetrics;
using MatrixAI.Processing;
using MatrixAI.Training;

namespace ChartRatingTrainer {
    public static class Program {
        public static readonly int POPULATION_SIZE = 1;
        
        private static readonly string ASSEMBLY_DIRECTORY = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly int METRIC_COUNT = ChartProcessor.DifficultyMetrics.Count;
        private static readonly int MATRIX_DIMENSIONS = 1;
        private static readonly double APPROACH_FACTOR = 0.005d;

        public static void Main(string[] args) {
            var random = new Random();
            var dataSet = GetDataSet();

            dataSet.Trim(0.975d);

            double[] baseCoefficients = dataSet.GetBaseCoefficients();
            
            dataSet.Normalize(baseCoefficients);

            var population = GetPopulation();
            var form = new Form1();

            Array.Sort(population);
            //MainThread(population, dataSet, form);
            Parallel.Invoke(() => MainThread(population, dataSet, form), () => FormThread(form));
            Array.Sort(population);

            var best = population[0];

            double[] results = dataSet.GetResults(best.Matrix, out double scale, out double bias);
            SavePopulation(population);
            OutputDetailedInfo(best, dataSet, results);
            SaveParameters(best.Matrix, bias, scale);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static void MainThread(Individual[] population, DataSet dataSet, Form1 form) {
            var lastBestTime = DateTime.Now;
            var drawExpectedReturned = new PointF[dataSet.Size];
            int generation = 0;
            var drawWatch = new Stopwatch();
            var checkWatch = new Stopwatch();
            var autoSaveWatch = new Stopwatch();
            var individual = population[0];
            var matrix = individual.Matrix;

            individual.Fitness = dataSet.GetFitnessAndVector(matrix, out _);
            Console.WriteLine($"Initial fitness: {individual.Fitness:0.00000000}");
            
            double lastBest = population[0].Fitness;
            
            drawWatch.Start();
            checkWatch.Start();
            autoSaveWatch.Start();

            while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter) {
                individual.Fitness = dataSet.GetFitnessAndVector(matrix, out var vector);
                MatrixExtensions.AddWeighted(matrix, APPROACH_FACTOR, vector);
                MatrixExtensions.Normalize(matrix);

                if (Form.ActiveForm == form && drawWatch.ElapsedMilliseconds > 166) {
                    double[] results = _ = dataSet.GetResults(matrix, out _, out _);

                    for (int i = 0; i < dataSet.Data.Length; i++) {
                        drawExpectedReturned[i] = new PointF(
                            (float) dataSet.Data[i].ExpectedResult,
                            (float) results[i]);
                    }

                    form.Draw(drawExpectedReturned);
                    drawWatch.Restart();
                }

                if (checkWatch.ElapsedMilliseconds > 10000) {
                    double currentBest = individual.Fitness;

                    Console.WriteLine($"{DateTime.Now:hh\\:mm\\:ss} Generation {generation}: {currentBest:0.00000000} (+{(currentBest - lastBest) / (DateTime.Now - lastBestTime).TotalMinutes:0.00000000} / m)");
                    
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

        private static void SavePopulation(Individual[] population) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "results.dat"), FileMode.Create))) {
                foreach (var individual in population)
                    individual.Matrix.Serialize(writer);
            }
            
            Console.WriteLine();
            Console.WriteLine("Saved file successfully");
            Console.WriteLine();
        }

        private static void SaveParameters(Matrix matrix, double bias, double scale) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat"), FileMode.Create))) {
                matrix.Serialize(writer);
                writer.Write(bias);
                writer.Write(scale);
            }
            
            Console.WriteLine("Saved parameters successfully");
            Console.WriteLine();
        }

        private static void OutputDetailedInfo(Individual best, DataSet dataSet, double[] results) {
            var matrix = best.Matrix;
            
            Console.WriteLine();
            Console.WriteLine($"Fitness: {best.Fitness:0.00000000}");
            Console.WriteLine("Value magnitudes:");

            var valueCoefficients = matrix.ValueCoefficients;
            var weightCoefficients = matrix.WeightCoefficients;
            
            for (int i = 0; i < matrix.TotalSize; i++)
                Console.WriteLine($"{valueCoefficients[i].Magnitude:0.0000}");
            
            Console.WriteLine();

            for (int i = 0; i < matrix.SampleSize; i++)
                Console.WriteLine($"{weightCoefficients[i].Magnitude:0.0000}");

            Console.WriteLine();

            var datas = dataSet.Data;
            var expectedReturnedPairs = datas.Select((data, i) => new ExpectedReturned(i, data.ExpectedResult, results[i])).ToArray();
            int longestName = 0;

            Array.Sort(expectedReturnedPairs, (a, b) => a.Index.CompareTo(b.Index));

            for (int i = 0; i < dataSet.Size; i++) {
                int nameLength = dataSet.Data[i].Name.Length;

                if (nameLength > longestName)
                    longestName = nameLength;
            }

            Console.WriteLine("Ordered rankings:");
            Array.Sort(expectedReturnedPairs);

            for (int j = 0; j < dataSet.Size; j++) {
                var pair = expectedReturnedPairs[j];
                    
                Console.WriteLine($"{pair.Returned:0.0000} <- {pair.Expected:0.0000} ({1d - Math.Abs(pair.Returned - pair.Expected):0.0000}) - {datas[pair.Index].Name}");
            }
                
            Console.WriteLine();
            Console.WriteLine("Correlation:");
            Array.Sort(expectedReturnedPairs, (a, b) => (1d - Math.Abs(a.Returned - a.Expected)).CompareTo(1d - Math.Abs(b.Returned - b.Expected)));

            for (int j = 0; j < dataSet.Size; j++) {
                var pair = expectedReturnedPairs[j];
                    
                Console.WriteLine($"{pair.Returned:0.0000} <- {pair.Expected:0.0000} ({1d - Math.Abs(pair.Returned - pair.Expected):0.0000}) - {datas[pair.Index].Name}");
            }
        }

        private static DataSet GetDataSet() {
            string cachePath = Path.Combine(ASSEMBLY_DIRECTORY, "Cache.dat");
            DataSet dataSet;

            if (File.Exists(cachePath)) {
                using (var reader = new BinaryReader(File.OpenRead(cachePath)))
                    dataSet = DataSet.Deserialize(reader, MATRIX_DIMENSIONS);

                return dataSet;
            }
            
            string directoriesPath = Path.Combine(ASSEMBLY_DIRECTORY, "Paths.txt");
            var directories = new List<string>();

            if (File.Exists(directoriesPath)) {
                using (var reader = new StreamReader(directoriesPath)) {
                    while (!reader.EndOfStream) {
                        string directory = reader.ReadLine();
                        
                        if (Directory.Exists(directory))
                            directories.Add(directory);
                    }
                }
            }
            else
                directories.Add(FileHelper.CustomPath);

            var processor = new ChartProcessor();
            var ratings = new Dictionary<string, int>();
            var dataList = new List<(Data, double)>();

            foreach (string directory in directories) {
                var regex = new Regex(@"(\d+)\t(.*)");

                using (var reader = new StreamReader(Path.Combine(directory, "Ratings.txt"))) {
                    while (!reader.EndOfStream) {
                        string line = reader.ReadLine();
                        var match = regex.Match(line);

                        if (match.Success)
                            ratings.Add(match.Groups[2].ToString().Trim(), int.Parse(match.Groups[1].ToString()));
                    }
                }

                foreach (string path in FileHelper.GetAllSrtbs(directory)) {
                    if (!ChartData.TryCreateFromFile(path, out var chartData, Difficulty.XD))
                        continue;

                    string trim = chartData.Title.Trim();
                    
                    if (ratings.TryGetValue(trim, out int rating))
                        ratings.Remove(trim);
                    else if (ratings.TryGetValue($"{trim} ({chartData.Charter})", out rating))
                        ratings.Remove($"{trim} ({chartData.Charter})");
                    else
                        continue;

                    processor.SetData(string.Empty, chartData.TrackData[Difficulty.XD].Notes);
                    dataList.Add((processor.CreateRatingData(), 0.01d * rating));
                }

                ratings.Clear();
            }
            
            dataSet = DataSet.Create(dataList.Count, METRIC_COUNT, MATRIX_DIMENSIONS, dataList);

            using (var writer = new BinaryWriter(File.OpenWrite(cachePath)))
                dataSet.Serialize(writer);

            return dataSet;
        }

        private static Individual[] GetPopulation() {
            var population = new Individual[POPULATION_SIZE];
            string path = Path.Combine(ASSEMBLY_DIRECTORY, "results.dat");
            int count = 0;

            if (File.Exists(path)) {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open))) {
                    for (int i = 0; i < POPULATION_SIZE && reader.BaseStream.Position < reader.BaseStream.Length; i++) {
                        population[i] = new Individual(Matrix.Deserialize(reader));
                        count++;
                    }
                }
            }

            for (int i = count; i < POPULATION_SIZE; i++)
                population[i] = new Individual(MatrixExtensions.Identity(METRIC_COUNT, MATRIX_DIMENSIONS));

            return population;
        }

        private static Color GetRandomColor(Random random) => Color.FromArgb(
            (int) (255d * random.NextDouble()),
            (int) (255d * random.NextDouble()),
            (int) (255d * random.NextDouble()));
    }
}