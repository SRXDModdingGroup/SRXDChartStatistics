using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ChartHelper;
using ChartMetrics;

namespace ChartAutoRating {
    internal class Program {
        public static readonly int METRIC_COUNT = ChartProcessor.Metrics.Count;

        private static readonly int CALCULATOR_COUNT = 16;
        private static readonly int MAX_GENERATIONS = 1;
        private static readonly string[] METRIC_NAMES = ChartProcessor.Metrics.Select(metric => metric.Name).ToArray();

        public static void Main(string[] args) {
            var random = new Random();
            var dataSet = new DataSet(GetDataSamples());
            float[] baseCoefficients = DataSet.Normalize(dataSet);
            var calculators = new Calculator[CALCULATOR_COUNT];
            var curveWeights = new CurveWeights[CALCULATOR_COUNT][];

            for (int i = 0; i < CALCULATOR_COUNT; i++) {
                var calculator = new Calculator(dataSet.Size);
                var metricCurveWeights = new CurveWeights[METRIC_COUNT];
                
                calculators[i] = calculator;

                for (int j = 0; j < METRIC_COUNT; j++) {
                    var randomCurveWeights = CurveWeights.Normalize(new CurveWeights(
                        (float) random.NextDouble(),
                        (float) random.NextDouble(),
                        (float) random.NextDouble(),
                        (float) random.NextDouble()
                    ));

                    metricCurveWeights[j] = randomCurveWeights;
                }
                
                curveWeights[i] = metricCurveWeights;
            }

            for (int i = 0; i < CALCULATOR_COUNT; i++) {
                calculators[i].ApplyWeights(curveWeights[i]);
            }

            for (int i = 0; i < MAX_GENERATIONS; i++) {
                Console.WriteLine($"Generation {i}:");
                
                for (int j = 0; j < CALCULATOR_COUNT; j++) {
                    var calculator = calculators[j];
                    calculator.CalculateResults(dataSet, null, out float correlation);
                    
                    Console.WriteLine($"\t Calculator {j}: {correlation}");
                }
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
                        float[] metrics = new float[METRIC_COUNT];
                        int i = 0;

                        while (!reader.EndOfStream) {
                            string line = reader.ReadLine();

                            if (string.IsNullOrWhiteSpace(line))
                                break;

                            metrics[i] = float.Parse(line);
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
                float[] metricResults = new float[METRIC_COUNT];

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

                    foreach (float value in sample.Metrics)
                        writer.WriteLine(value);

                    writer.WriteLine();
                }
            }

            return dataSamples.ToArray();
        }
    }
}