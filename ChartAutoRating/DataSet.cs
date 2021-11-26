using System;
using System.Linq;

namespace ChartAutoRating {
    public class DataSet {
        public int Size { get; }
        
        public RelevantChartInfo[] RelevantChartInfo { get; }

        public double[][] Metrics { get; }

        public Table DifficultyComparisons { get; }
        
        public Table[] MetricComparisons { get; }

        public DataSet(RelevantChartInfo[] relevantChartInfo, double[][] metrics) {
            Size = relevantChartInfo.Length;
            RelevantChartInfo = relevantChartInfo;
            Metrics = metrics;
            DifficultyComparisons = new Table(Size);
            Table.GenerateComparisonTable(DifficultyComparisons, relevantChartInfo.Select(sample => (double) sample.DifficultyRating).ToArray(), Size);
            MetricComparisons = new Table[Program.METRIC_COUNT];

            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                var table = new Table(Size);
                
                Table.GenerateComparisonTable(table, metrics.Select(sample => sample[i]).ToArray(), Size);
                MetricComparisons[i] = table;
            }
        }

        public static double[] Normalize(params DataSet[] dataSets) {
            double[] baseCoefficients = new double[Program.METRIC_COUNT];

            for (int i = 0; i < Program.METRIC_COUNT; i++) {
                double max = 0d;

                foreach (var dataSet in dataSets) {
                    for (int k = 0; k < dataSet.Size; k++) {
                        double value = dataSet.Metrics[k][i];

                        if (value > max)
                            max = value;
                    }
                }
                
                double baseCoefficient = 1d / max;
                
                baseCoefficients[i] = baseCoefficient;
                
                foreach (var dataSet in dataSets) {
                    for (int k = 0; k < dataSet.Size; k++)
                        dataSet.Metrics[k][i] *= baseCoefficient;
                }
            }

            return baseCoefficients;
        }
    }
}