using System;
using System.Linq;

namespace ChartAutoRating {
    public class DataSet {
        public int Size { get; }
        
        public DataSample[] Samples { get; }

        public Table DifficultyComparisons { get; }
        
        public Table[] MetricComparisons { get; }

        public DataSet(DataSample[] samples) {
            Size = samples.Length;
            Samples = samples;
            DifficultyComparisons = new Table(Size);
            Table.GenerateComparisonTable(DifficultyComparisons, index => samples[index].DifficultyRating, Size);
            MetricComparisons = new Table[Program.METRIC_COUNT];

            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                var table = new Table(Size);
                int j = i;
                
                Table.GenerateComparisonTable(table, index => samples[index].Metrics[j], Size);
                MetricComparisons[i] = table;
            }
        }

        public static float[] Normalize(params DataSet[] dataSets) {
            float[] baseCoefficients = new float[Program.METRIC_COUNT];
            float[] values = new float[dataSets.Sum(dataSet => dataSet.Size)];

            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                int j = 0;

                foreach (var dataSet in dataSets) {
                    foreach (var sample in dataSet.Samples) {
                        values[j] = sample.Metrics[i];
                        j++;
                    }
                }
                
                Array.Sort(values);
                float baseCoefficient = 1f / values[values.Length * 9 / 10];
                
                baseCoefficients[i] = baseCoefficient;
                
                foreach (var dataSet in dataSets) {
                    foreach (var sample in dataSet.Samples)
                        sample.Metrics[i] = Math.Min(sample.Metrics[i] * baseCoefficient, 1f);
                }
            }

            return baseCoefficients;
        }
    }
}