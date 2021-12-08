﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChartHelper;
using ChartHelper.Types;
using ChartMetrics;
using AI.Training;

namespace ChartRatingAI.Training {
    public static class Program {
        private static readonly string ASSEMBLY_DIRECTORY = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly int METRIC_COUNT = ChartProcessor.DifficultyMetrics.Count;
        private static readonly int MATRIX_DIMENSIONS = 4;
        private static readonly double MIN_APPROACH_FACTOR = 0.00025d;
        private static readonly double MAX_APPROACH_FACTOR = 0.01d;
        private static readonly double VECTOR_MAGNITUDE = 0.05d;
        private static readonly double DAMPENING = 2d;

        public static void Main(string[] args) {
            var random = new Random();
            var dataSet = GetDataSet();

            dataSet.Trim(0.9d, 0.95d);
            dataSet.GetBaseParameters(out double[] scales, out double[] powers);
            dataSet.Normalize(scales, powers);
            GetMatrices(random, out var valueMatrix, out var weightMatrix);
            
            var form = new Form1();

            //MainThread(population, dataSet, form);
            Parallel.Invoke(() => MainThread(valueMatrix, weightMatrix, dataSet, form, random), () => FormThread(form));

            double fitness = dataSet.Adjust(valueMatrix, weightMatrix, 0d, 0d, random);
            double[] results = dataSet.GetResults(valueMatrix, weightMatrix, out double scale, out double bias);
            
            SaveMatrices(valueMatrix, weightMatrix);
            OutputDetailedInfo(fitness, valueMatrix, weightMatrix, dataSet, results);
            SaveParameters(valueMatrix, weightMatrix, bias, scale, scales, powers);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        private static void MainThread(Compiler valueCompiler, Compiler weightCompiler, DataSet<,> dataSet, Form1 form, Random random) {
            int generation = 0;
            int lastCheckedGeneration = 0;
            double approachFactor = MAX_APPROACH_FACTOR;
            double fitness = dataSet.Adjust(valueCompiler, weightCompiler, 0d, 0d, random);
            double currentBest = fitness;
            double lastCheckedBest = fitness;
            var lastCheckTime = DateTime.Now;
            var drawExpectedReturned = new PointF[dataSet.Size];
            var drawWatch = new Stopwatch();
            var checkWatch = new Stopwatch();
            var autoSaveWatch = new Stopwatch();
            
            Console.WriteLine($"Initial fitness: {fitness:0.00000000}");

            drawWatch.Start();
            checkWatch.Start();
            autoSaveWatch.Start();

            while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter) {
                double newFitness = dataSet.Adjust(valueCompiler, weightCompiler, approachFactor, VECTOR_MAGNITUDE, random);

                if (newFitness > fitness)
                    approachFactor = Math.Min(DAMPENING * approachFactor, MAX_APPROACH_FACTOR);
                else
                    approachFactor = Math.Max(MIN_APPROACH_FACTOR, approachFactor / DAMPENING);

                fitness = newFitness;

                if (fitness > currentBest)
                    currentBest = fitness;

                if (Form.ActiveForm == form && drawWatch.ElapsedMilliseconds > 166) {
                    double[] results = dataSet.GetResults(valueCompiler, weightCompiler, out _, out _);

                    for (int i = 0; i < dataSet.Data.Length; i++) {
                        drawExpectedReturned[i] = new PointF(
                            (float) dataSet.Data[i].ExpectedResult,
                            (float) results[i]);
                    }

                    form.Draw(drawExpectedReturned, 1d, 0d);
                    drawWatch.Restart();
                }

                if (checkWatch.ElapsedMilliseconds > 10000 && currentBest > lastCheckedBest) {
                    Console.WriteLine($"{DateTime.Now:hh\\:mm\\:ss} Generation {generation} ({(generation - lastCheckedGeneration) / (DateTime.Now - lastCheckTime).TotalSeconds:0.00} / s): {currentBest:0.00000000} (+{(currentBest - lastCheckedBest) / (DateTime.Now - lastCheckTime).TotalMinutes:0.00000000} / m)");
                    lastCheckedGeneration = generation;
                    lastCheckedBest = currentBest;
                    lastCheckTime = DateTime.Now;
                    checkWatch.Restart();
                }

                if (autoSaveWatch.ElapsedMilliseconds > 300000) {
                    SaveMatrices(valueCompiler, weightCompiler);
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

        private static void SaveMatrices(Compiler valueCompiler, Compiler weightCompiler) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Results.dat"), FileMode.Create))) {
                valueCompiler.SerializeModel(writer);
                weightCompiler.SerializeModel(writer);
            }
            
            Console.WriteLine();
            Console.WriteLine("Saved file successfully");
            Console.WriteLine();
        }

        private static void SaveParameters(Compiler valueCompiler, Compiler weightCompiler, double bias, double scale, double[] baseScales, double[] basePowers) {
            using (var writer = new BinaryWriter(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat"), FileMode.Create))) {
                valueCompiler.SerializeModel(writer);
                weightCompiler.SerializeModel(writer);
                writer.Write(bias);
                writer.Write(scale);

                foreach (double baseScale in baseScales)
                    writer.Write(baseScale);
                
                foreach (double basePower in basePowers)
                    writer.Write(basePower);
            }
            
            Console.WriteLine("Saved parameters successfully");
            Console.WriteLine();
        }

        private static void OutputDetailedInfo(double fitness, Compiler valueCompiler, Compiler weightCompiler, DataSet<,> dataSet, double[] results) {
            Console.WriteLine();
            Console.WriteLine($"Fitness: {fitness:0.00000000}");
            Console.WriteLine("Values:");

            var valueList = ArrayExtensions.EnumerateValues(valueCompiler);
            
            valueList.Sort((a, b) => -a.Item2.CompareTo(b.Item2));

            foreach ((string index, double value) in valueList)
                Console.WriteLine($"{index} {value:0.0000}");

            Console.WriteLine();
            
            var weightList = ArrayExtensions.EnumerateValues(weightCompiler);
            
            weightList.Sort((a, b) => -a.Item2.CompareTo(b.Item2));

            foreach ((string index, double value) in weightList)
                Console.WriteLine($"{index} {value:0.0000}");
            
            Console.WriteLine();

            var datas = dataSet.Data;
            var expectedReturnedPairs = datas.Select((data, i) => new ExpectedReturned(data.Name, data.ExpectedResult, results[i])).ToArray();
            int longestName = 0;

            for (int i = 0; i < dataSet.Size; i++) {
                int nameLength = dataSet.Data[i].Name.Length;

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

        private static DataSet<,> GetDataSet() {
            string cachePath = Path.Combine(ASSEMBLY_DIRECTORY, "Cache.dat");
            DataSet<,> dataSet;

            if (File.Exists(cachePath)) {
                using (var reader = new BinaryReader(File.OpenRead(cachePath)))
                    dataSet = DataSet<,>.Deserialize(reader, MATRIX_DIMENSIONS);

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
            var dataList = new List<(AI.Processing.Data, double)>();

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
                    if (!Data.TryCreateFromFile(path, out var chartData, Difficulty.XD))
                        continue;

                    string trim = chartData.Title.Trim();
                    
                    if (ratings.TryGetValue(trim, out int rating))
                        ratings.Remove(trim);
                    else if (ratings.TryGetValue($"{trim} ({chartData.Charter})", out rating))
                        ratings.Remove($"{trim} ({chartData.Charter})");
                    else
                        continue;

                    processor.SetData(chartData.Title, chartData.TrackData[Difficulty.XD].Notes);
                    dataList.Add((processor.CreateRatingData(), 0.01d * rating));
                }

                ratings.Clear();
            }
            
            dataSet = DataSet<,>.Create(dataList.Count, METRIC_COUNT, MATRIX_DIMENSIONS, dataList);

            using (var writer = new BinaryWriter(File.OpenWrite(cachePath)))
                dataSet.Serialize(writer);

            return dataSet;
        }

        private static void GetMatrices(Random random, out Compiler valueCompiler, out Compiler weightCompiler) {
            string path = Path.Combine(ASSEMBLY_DIRECTORY, "Results.dat");

            if (File.Exists(path)) {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open))) {
                    valueCompiler = Compiler.DeserializeModel(reader);
                    weightCompiler = Compiler.DeserializeModel(reader);
                }
            }
            else {
                valueCompiler = ArrayExtensions.Random(METRIC_COUNT, MATRIX_DIMENSIONS, random);
                weightCompiler = ArrayExtensions.Random(METRIC_COUNT, MATRIX_DIMENSIONS, random);
                // valueCompiler = ArrayExtensions.Identity(METRIC_COUNT, MATRIX_DIMENSIONS);
                // weightCompiler = ArrayExtensions.Identity(METRIC_COUNT, MATRIX_DIMENSIONS);
            }
        }
    }
}