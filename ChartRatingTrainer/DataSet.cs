using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ChartAutoRating;
using ChartHelper;
using ChartMetrics;

namespace ChartRatingTrainer {
    public class DataSet {
        public int Size { get; }
        
        public RelevantChartInfo[] RelevantChartInfo { get; }

        public Data[] Datas { get; }

        public Table DifficultyComparisons { get; }
        
        public double[][] ResultsArrays { get; }
        
        public Table[] ResultsTables { get; }

        public DataSet(string path) {
            var ratings = new Dictionary<string, int>();
            var regex = new Regex(@"(\d+)\t(.*)");

            using (var reader = new StreamReader(Path.Combine(path, "Ratings.txt"))) {
                while (!reader.EndOfStream) {
                    string line = reader.ReadLine();
                    var match = regex.Match(line);

                    if (match.Success)
                        ratings.Add(match.Groups[2].ToString(), int.Parse(match.Groups[1].ToString()));
                }
            }
            
            int metricCount = ChartProcessor.Metrics.Count;
            var cache = new Dictionary<string, CacheInfo>();
            string cachePath = Path.Combine(path, "cache.dat");

            if (File.Exists(cachePath)) {
                using (var reader = new BinaryReader(File.Open(cachePath, FileMode.Open))) {
                    var stream = reader.BaseStream;
                    
                    while (stream.Position < stream.Length) {
                        string id = reader.ReadString();
                        string title = reader.ReadString();
                        int difficultyRating = reader.ReadInt32();
                        var dataSamples = new DataSample[metricCount][];

                        for (int i = 0; i < metricCount; i++) {
                            int count = reader.ReadInt32();
                            var metricDataSamples = new DataSample[count];

                            for (int j = 0; j < count; j++) {
                                double value = reader.ReadDouble();
                                double weight = reader.ReadDouble();

                                metricDataSamples[j] = new DataSample(value, weight);
                            }

                            dataSamples[i] = metricDataSamples;
                        }

                        cache.Add(id, new CacheInfo(new RelevantChartInfo(title, difficultyRating), new Data(dataSamples)));
                    }
                }
            }

            var chartInfo = new List<RelevantChartInfo>();
            var dataList = new List<Data>();
            var ids = new List<string>();

            foreach (string chartPath in FileHelper.GetAllSrtbs(path)) {
                string id = $"{chartPath}_{File.GetLastWriteTime(chartPath)}";
                RelevantChartInfo newChartInfo;
                Data data;

                if (cache.TryGetValue(id, out var cacheInfo)) {
                    newChartInfo = cacheInfo.ChartInfo;
                    data = cacheInfo.Data;
                }
                else if (!ChartData.TryCreateFromFile(chartPath, out var chartData, Difficulty.XD) || !chartData.TrackData.TryGetValue(Difficulty.XD, out var trackData))
                    continue;
                else {
                    var processor = new ChartProcessor(chartData.Title, trackData.Notes);

                    data = new Data(processor);

                    if (!ratings.TryGetValue(chartData.Title, out int rating))
                        rating = trackData.DifficultyRating;

                    newChartInfo = new RelevantChartInfo(chartData.Title, rating);
                }

                chartInfo.Add(newChartInfo);
                dataList.Add(data);
                ids.Add(id);
            }

            using (var writer = new BinaryWriter(File.Open(cachePath, FileMode.Create))) {
                for (int i = 0; i < chartInfo.Count; i++) {
                    var info = chartInfo[i];
                    var data = dataList[i];

                    writer.Write(ids[i]);
                    writer.Write(info.Title);
                    writer.Write(info.DifficultyRating);

                    for (int j = 0; j < metricCount; j++) {
                        var metricDataSamples = data.DataSamples[j];
                        
                        writer.Write(metricDataSamples.Length);

                        foreach (var sample in metricDataSamples) {
                            writer.Write(sample.Value);
                            writer.Write(sample.Weight);
                        }
                    }
                }
            }
            
            Size = chartInfo.Count;
            RelevantChartInfo = chartInfo.ToArray();
            Datas = dataList.ToArray();
            DifficultyComparisons = new Table(Size);
            Table.GenerateComparisonTable(DifficultyComparisons, RelevantChartInfo.Select(sample => (double) sample.DifficultyRating).ToArray(), Size);
            ResultsArrays = new double[Program.GROUP_COUNT][];
            ResultsTables = new Table[Program.GROUP_COUNT];

            for (int i = 0; i < Program.GROUP_COUNT; i++) {
                ResultsArrays[i] = new double[Size];
                ResultsTables[i] = new Table(Size);
            }
        }

        public static double[] Normalize(params DataSet[] dataSets) {
            double[] baseCoefficients = new double[Program.METRIC_COUNT];

            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                var values = new List<double>();

                foreach (var dataSet in dataSets) {
                    foreach (var data in dataSet.Datas) {
                        foreach (var sample in data.DataSamples[i])
                            values.Add(sample.Value);
                    }
                }
                
                values.Sort();

                double baseCoefficient = 1d / values[values.Count - 16];
                
                baseCoefficients[i] = baseCoefficient;
                
                foreach (var dataSet in dataSets) {
                    foreach (var data in dataSet.Datas) {
                        var samples = data.DataSamples[i];

                        for (int j = 0; j < samples.Length; j++)
                            samples[j] = new DataSample(Math.Min(baseCoefficient * samples[j].Value, 1d), samples[j].Weight);
                    }
                }
            }

            return baseCoefficients;
        }
    }
}