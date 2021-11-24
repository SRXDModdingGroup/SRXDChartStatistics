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
            Table.GenerateComparisonTable(DifficultyComparisons, samples.Select(sample => (double) sample.DifficultyRating).ToArray(), Size);
            MetricComparisons = new Table[Program.METRIC_COUNT];

            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                var table = new Table(Size);
                int j = i;
                
                Table.GenerateComparisonTable(table, samples.Select(sample => sample.Metrics[j]).ToArray(), Size);
                MetricComparisons[i] = table;
            }
        }

        public static double[] Normalize(params DataSet[] dataSets) {
            double[] baseCoefficients = new double[Program.METRIC_COUNT];
            double[] values = new double[dataSets.Sum(dataSet => dataSet.Size)];

            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                int j = 0;

                foreach (var dataSet in dataSets) {
                    foreach (var sample in dataSet.Samples) {
                        values[j] = sample.Metrics[i];
                        j++;
                    }
                }
                
                Array.Sort(values);
                double baseCoefficient = 1d / values[values.Length * 9 / 10];
                
                baseCoefficients[i] = baseCoefficient;
                
                foreach (var dataSet in dataSets) {
                    foreach (var sample in dataSet.Samples)
                        sample.Metrics[i] = Math.Min(sample.Metrics[i] * baseCoefficient, 1d);
                }
            }

            return baseCoefficients;
        }
    }
}