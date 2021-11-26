using System;
using ChartMetrics;

namespace ChartAutoRating {
    public class Data {
        public DataSample[][] DataSamples { get; }
        
        public Data(ChartProcessor processor) {
            int metricCount = ChartProcessor.Metrics.Count;
            
            DataSamples = new DataSample[metricCount][];

            for (int i = 0; i < metricCount; i++) {
                if (!processor.TryGetMetric(ChartProcessor.Metrics[i].Name, out var result)) {
                    DataSamples[i] = Array.Empty<DataSample>();
                    
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

                DataSamples[i] = metricDataSamples;
            }
        }

        public Data(DataSample[][] dataSamples) {
            DataSamples = dataSamples;
        }
    }
}