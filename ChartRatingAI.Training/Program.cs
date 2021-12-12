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
using ChartHelper.Types;
using ChartMetrics;
using AI.Training;
using ChartHelper.Parsing;
using ArrayModel = AI.Training.ArrayModel;

namespace ChartRatingAI.Training {
    public static class Program {
        private static readonly string ASSEMBLY_DIRECTORY = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly int METRIC_COUNT = ChartProcessor.DifficultyMetrics.Count;
        private static readonly int COMPILER_DIMENSIONS = 4;
        private static readonly int BATCH_COUNT = 2;
        private static readonly double LEARNING_RATE = 0.00001d;
        private static readonly double MOMENTUM = 0.9d;

        public static void Main(string[] args) {
            var random = new Random();
            var dataSet = GetDataSet();

            dataSet.Trim(0.9d, 0.95d, out double[] limits);
            dataSet.Normalize(out double[] scales, out double[] powers);

            var algorithm = new Algorithm(METRIC_COUNT, COMPILER_DIMENSIONS);
            var model = GetModel(random);
            var form = new Form1();

            //MainThread(population, dataSet, form);
            Parallel.Invoke(() => MainThread(algorithm, model, dataSet, form, random), () => FormThread(form));

            double fitness = dataSet.GetResults(algorithm, model, out double[] results);
            
            SaveModel(model);
            OutputDetailedInfo(fitness, model, dataSet, results);
            SaveParameters(model, scales, powers, limits);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static void MainThread(Algorithm algorithm, Model model, DataSet dataSet, Form1 form, Random random) {
            int generation = 0;
            int lastCheckedGeneration = 0;
            var drawExpectedReturned = new PointF[dataSet.Size];
            var lastCheckTime = DateTime.Now;
            var drawWatch = new Stopwatch();
            var checkWatch = new Stopwatch();
            var autoSaveWatch = new Stopwatch();
            var vector = new Model(
                new ArrayModel(new double[model.ValueCompilerModel.Array.Length]),
                new ArrayModel(new double[model.WeightCompilerModel.Array.Length]));
            
            drawWatch.Start();
            checkWatch.Start();
            autoSaveWatch.Start();
            
            double fitness = dataSet.GetResults(algorithm, model, out _);
            double currentBest = fitness;
            double lastCheckedBest = fitness;
            
            Console.WriteLine($"Initial fitness: {fitness:0.00000000}");

            while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter) {
                dataSet.Shuffle(random);
                fitness = dataSet.Backpropagate(algorithm, model, vector, LEARNING_RATE, MOMENTUM, out double[] results);
                
                if (fitness > currentBest)
                    currentBest = fitness;

                if (Form.ActiveForm == form && drawWatch.ElapsedMilliseconds > 166) {
                    for (int i = 0; i < dataSet.Data.Length; i++) {
                        drawExpectedReturned[i] = new PointF(
                            (float) dataSet.Data[i].ExpectedResult,
                            (float) results[i]);
                    }

                    form.Draw(drawExpectedReturned, 1d, 0d);
                    drawWatch.Restart();
                }

                if (currentBest > lastCheckedBest) {
                    Console.WriteLine($"{DateTime.Now:hh\\:mm\\:ss} Generation {generation} ({(generation - lastCheckedGeneration) / (DateTime.Now - lastCheckTime).TotalSeconds:0.00} / s):" +
                                      $"{currentBest:0.00000000} (+{(currentBest - lastCheckedBest) / (DateTime.Now - lastCheckTime).TotalMinutes:0.00000000} / m)");
                    lastCheckedGeneration = generation;
                    lastCheckedBest = currentBest;
                    lastCheckTime = DateTime.Now;
                    checkWatch.Restart();
                }

                // if (autoSaveWatch.ElapsedMilliseconds > 300000) {
                //     SaveModel(model, learningRate);
                //     autoSaveWatch.Restart();
                // }

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

        private static void SaveModel(Model model) {
            using (var writer = new BinaryWriter(File.OpenWrite(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Results.dat"))))
                model.Serialize(writer);

            Console.WriteLine();
            Console.WriteLine("Saved file successfully");
            Console.WriteLine();
        }

        private static void SaveParameters(Model model, double[] baseScales, double[] basePowers, double[] limits) {
            using (var writer = new BinaryWriter(File.OpenWrite(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat")))) {
                model.Serialize(writer);

                foreach (double baseScale in baseScales)
                    writer.Write(baseScale);
                
                foreach (double basePower in basePowers)
                    writer.Write(basePower);
                
                foreach (double limit in limits)
                    writer.Write(limit);
            }
            
            Console.WriteLine("Saved parameters successfully");
            Console.WriteLine();
        }

        private static void OutputDetailedInfo(double fitness, Model model, DataSet dataSet, double[] results) {
            Console.WriteLine();
            Console.WriteLine($"Fitness: {fitness:0.00000000}");
            Console.WriteLine("Values:");

            var valueList = ArrayExtensions.EnumerateValues(model.ValueCompilerModel.Array, METRIC_COUNT, COMPILER_DIMENSIONS);
            
            valueList.Sort((a, b) => -a.Item2.CompareTo(b.Item2));

            foreach ((string index, double value) in valueList)
                Console.WriteLine($"{index} {value:0.0000}");

            Console.WriteLine();
            
            var weightList = ArrayExtensions.EnumerateValues(model.WeightCompilerModel.Array, METRIC_COUNT, COMPILER_DIMENSIONS);
            
            weightList.Sort((a, b) => -a.Item2.CompareTo(b.Item2));

            foreach ((string index, double value) in weightList)
                Console.WriteLine($"{index} {value:0.0000}");
            
            Console.WriteLine();

            var dataPairs = dataSet.Data;
            var expectedReturnedPairs = dataPairs.Select((pair, i) => new ExpectedReturned(pair.Data.Name, pair.ExpectedResult, results[i])).ToArray();
            int longestName = 0;

            for (int i = 0; i < dataSet.Size; i++) {
                int nameLength = dataSet.Data[i].Data.Name.Length;

                if (nameLength > longestName)
                    longestName = nameLength;
            }

            Console.WriteLine("Ordered rankings:");
            Array.Sort(expectedReturnedPairs);

            for (int j = 0; j < dataSet.Size; j++) {
                var pair = expectedReturnedPairs[j];
                    
                Console.WriteLine($"{pair.Returned:0.0000} <- {pair.Expected:0.0000} ({1d - Math.Abs(pair.Returned - pair.Expected):0.0000}) - {pair.Name}");
            }
                
            Console.WriteLine();
            Console.WriteLine("Correlation:");
            Array.Sort(expectedReturnedPairs, (a, b) => (1d - Math.Abs(a.Returned - a.Expected)).CompareTo(1d - Math.Abs(b.Returned - b.Expected)));

            for (int j = 0; j < dataSet.Size; j++) {
                var pair = expectedReturnedPairs[j];
                    
                Console.WriteLine($"{pair.Returned:0.0000} <- {pair.Expected:0.0000} ({1d - Math.Abs(pair.Returned - pair.Expected):0.0000}) - {pair.Name}");
            }
        }

        private static DataSet GetDataSet() {
            string cachePath = Path.Combine(ASSEMBLY_DIRECTORY, "Cache.dat");
            DataSet dataSet;

            if (File.Exists(cachePath)) {
                using (var reader = new BinaryReader(File.OpenRead(cachePath)))
                    dataSet = DataSet.Deserialize(reader, BATCH_COUNT);

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

                    processor.SetData(chartData.Title, chartData.TrackData[Difficulty.XD].Notes);
                    dataList.Add((new Data(processor.Title, METRIC_COUNT, processor.CreateRatingData()), 0.01d * rating));
                }

                ratings.Clear();
            }
            
            dataSet = new DataSet(dataList.Count, METRIC_COUNT, BATCH_COUNT, dataList);

            using (var writer = new BinaryWriter(File.OpenWrite(cachePath)))
                dataSet.Serialize(writer);

            return dataSet;
        }

        private static Model GetModel(Random random) {
            string path = Path.Combine(ASSEMBLY_DIRECTORY, "Results.dat");

            if (File.Exists(path)) {
                using (var reader = new BinaryReader(File.OpenRead(path)))
                    return Model.Deserialize(reader);
            }

            int num = 1;

            for (int i = METRIC_COUNT + 1; i < METRIC_COUNT + COMPILER_DIMENSIONS + 1; i++)
                num *= i;

            int den = 1;

            for (int i = 2; i <= COMPILER_DIMENSIONS; i++)
                den *= i;
            
            int size = num / den;

            return new Model(new ArrayModel(ArrayExtensions.Random(size, 1d, random)), new ArrayModel(ArrayExtensions.Random(size, 1d, random)));
        }
    }
}