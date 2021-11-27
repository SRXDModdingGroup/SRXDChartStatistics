using System;
using System.IO;
using ChartMetrics;
using Util;

namespace ChartAutoRating {
    public class Data {
        internal DataSample[][] DataSamples { get; private set; }

        private int metricCount;

        public static Data Create(ChartProcessor processor) {
            int metricCount = ChartProcessor.Metrics.Count;
            var data = new Data();
            
            data.DataSamples = new DataSample[metricCount][];
            data.metricCount = metricCount;

            for (int i = 0; i < metricCount; i++) {
                if (!processor.TryGetMetric(ChartProcessor.Metrics[i].Name, out var result)) {
                    data.DataSamples[i] = Array.Empty<DataSample>();
                    
                    continue;
                }

                var samples = result.Samples;
                var metricDataSamples = new DataSample[samples.Count];
                double totalWeight = 0d;

                foreach (var sample in samples)
                    totalWeight += sample.Length;

                double weightCoefficient = 1d / totalWeight;

                for (int j = 0; j < samples.Count; j++) {
                    var sample = samples[j];

                    metricDataSamples[j] = new DataSample(sample.Value, weightCoefficient * sample.Length);
                }

                data.DataSamples[i] = metricDataSamples;
            }

            return data;
        }

        public static Data Deserialize(BinaryReader reader) {
            int metricCount = ChartProcessor.Metrics.Count;
            var data = new Data();
            
            data.DataSamples = new DataSample[metricCount][];
            data.metricCount = metricCount;

            for (int i = 0; i < metricCount; i++) {
                int count = reader.ReadInt32();
                var metricDataSamples = new DataSample[count];

                for (int j = 0; j < count; j++) {
                    double value = reader.ReadDouble();
                    double weight = reader.ReadDouble();

                    metricDataSamples[j] = new DataSample(value, weight);
                }

                data.DataSamples[i] = metricDataSamples;
            }

            return data;
        }

        public void Serialize(BinaryWriter writer) {
            for (int j = 0; j < metricCount; j++) {
                var metricDataSamples = DataSamples[j];
                        
                writer.Write(metricDataSamples.Length);

                foreach (var sample in metricDataSamples) {
                    writer.Write(sample.Value);
                    writer.Write(sample.Weight);
                }
            }
        }

        public void Normalize(double[] baseCoefficients) {
            for (int i = 0; i < metricCount; i++) {
                var samples = DataSamples[i];
                double coeff = baseCoefficients[i];
                
                for (int j = 0; j < samples.Length; j++)
                    samples[j] = new DataSample(Math.Min(coeff * samples[j].Value, 1d), samples[j].Weight);
            }
        }

        public void Clamp(int metricIndex, double min, double max) {
            var samples = DataSamples[metricIndex];
                
            for (int i = 0; i < samples.Length; i++)
                samples[i] = new DataSample(MathU.Clamp(samples[i].Value, min, max), samples[i].Weight);
        }

        public double GetQuantile(int metricIndex, double quantile) {
            var samples = DataSamples[metricIndex];
            var sorted = new DataSample[samples.Length];

            for (int i = 0; i < samples.Length; i++)
                sorted[i] = samples[i];
                
            Array.Sort(sorted);

            double[] cumulativeWeights = new double[sorted.Length];
            double sum = 0d;

            for (int i = 0; i < sorted.Length; i++) {
                sum += sorted[i].Weight;
                cumulativeWeights[i] = sum;
            }

            double targetTotal = quantile * sum;
            var first = sorted[0];
                
            if (targetTotal < 0.5f * first.Weight)
                return first.Value;

            var last = sorted[sorted.Length - 1];

            if (targetTotal > sum - 0.5f * last.Weight)
                return last.Value;
                
            for (int i = 0; i < sorted.Length - 1; i++) {
                double end = cumulativeWeights[i] + 0.5f * sorted[i + 1].Weight;
                    
                if (end < targetTotal)
                    continue;

                double start = cumulativeWeights[i] - 0.5f * sorted[i].Weight;

                return MathU.Remap(targetTotal, start, end, sorted[i].Value, sorted[i + 1].Value);
            }

            return sorted[sorted.Length - 1].Value;
        }

        public double GetMaxValue(int metricIndex) {
            double max = 0d;
            
            foreach (var sample in DataSamples[metricIndex]) {
                if (sample.Value > max)
                    max = sample.Value;
            }

            return max;
        }
    }
}