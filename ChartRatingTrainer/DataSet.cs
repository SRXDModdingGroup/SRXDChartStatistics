using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            
            var cache = new Dictionary<string, CacheInfo>();
            string cachePath = Path.Combine(path, "cache.dat");

            if (File.Exists(cachePath)) {
                using (var reader = new BinaryReader(File.Open(cachePath, FileMode.Open))) {
                    var stream = reader.BaseStream;
                    
                    while (stream.Position < stream.Length) {
                        string id = reader.ReadString();
                        string title = reader.ReadString();
                        int difficultyRating = reader.ReadInt32();
                        
                        cache.Add(id, new CacheInfo(new RelevantChartInfo(title, difficultyRating), Data.Deserialize(Calculator.METRIC_COUNT, reader)));
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
                    data = new ChartProcessor(chartData.Title, trackData.Notes).CreateData();

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

                    writer.Write(ids[i]);
                    writer.Write(info.Title);
                    writer.Write(info.DifficultyRating);
                    dataList[i].Serialize(writer);
                }
            }
            
            Size = chartInfo.Count;
            RelevantChartInfo = chartInfo.ToArray();
            Datas = dataList.ToArray();
            DifficultyComparisons = new Table(Size);
            Table.GenerateComparisonTable(DifficultyComparisons, RelevantChartInfo.Select(sample => (double) sample.DifficultyRating).ToArray(), Size);
            ResultsArrays = new double[Program.CROSSOVERS][];
            ResultsTables = new Table[Program.CROSSOVERS];

            for (int i = 0; i < Program.CROSSOVERS; i++) {
                ResultsArrays[i] = new double[Size];
                ResultsTables[i] = new Table(Size);
            }
        }

        public void Trim(double upperQuantile) {
            foreach (var data in Datas) {
                for (int i = 0; i < Calculator.METRIC_COUNT; i++)
                    data.Clamp(i, data.GetQuantile(i, upperQuantile));
            }
        }

        public void Normalize(double[] baseCoefficients) {
            foreach (var data in Datas)
                data.Normalize(baseCoefficients);
        }

        public static double[] GetBaseCoefficients(params DataSet[] dataSets) {
            double[] baseCoefficients = new double[Calculator.METRIC_COUNT];

            for (int i = 0; i < Calculator.METRIC_COUNT; i++) {
                double max = 0d;

                foreach (var dataSet in dataSets) {
                    foreach (var data in dataSet.Datas) {
                        double newMax = data.GetMaxValue(i);

                        if (newMax > max)
                            max = newMax;
                    }
                }
                
                baseCoefficients[i] = 1d / max;
            }

            return baseCoefficients;
        }
    }
}