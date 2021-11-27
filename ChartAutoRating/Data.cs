using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Util;

namespace ChartAutoRating {
    public class Data {
        private class TempDataSample {
            public double[] Values { get; }
        
            public double Time { get; }

            public TempDataSample(double[] values, double time) {
                Values = values;
                Time = time;
            }
        }
        
        internal DataSample[] DataSamples { get; }

        private int metricCount;

        private Data(int metricCount, int sampleCount) {
            this.metricCount = metricCount;
            DataSamples = new DataSample[sampleCount];
        }

        public static Data Create(int metricCount, Func<int, IEnumerable<(double, double)>> selector) {
            var samplesList = new List<TempDataSample>();
            
            for (int i = 0; i < metricCount; i++) {
                foreach ((double value, double time) in selector(i)) {
                    double[] newValues;
                    
                    if (samplesList.Count > 0) {
                        bool found = false;
                        
                        for (int j = 0; j < samplesList.Count; j++) {
                            var sample = samplesList[j];
                            double sampleTime = sample.Time;

                            if (MathU.AlmostEquals(time, sampleTime)) {
                                sample.Values[i] = value;
                                found = true;

                                break;
                            }

                            if (time < sampleTime) {
                                newValues = new double[metricCount];
                                newValues[i] = value;
                                samplesList.Insert(i, new TempDataSample(newValues, time));
                                found = true;
                                
                                break;
                            }
                        }
                        
                        if (found)
                            continue;
                    }
                    
                    newValues = new double[metricCount];
                    
                    if (i > 0) {
                        double[] previousValues = samplesList[i - 1].Values;

                        for (int j = 0; j < metricCount; j++)
                            newValues[j] = previousValues[j];
                    }
                    
                    newValues[i] = value;
                    samplesList.Add(new TempDataSample(newValues, time));
                }
            }
            
            var data = new Data(metricCount, samplesList.Count - 1);

            for (int i = 0; i < data.DataSamples.Length; i++)
                data.DataSamples[i] = new DataSample(samplesList[i].Values, samplesList[i + 1].Time - samplesList[i].Time);

            return data;
        }

        public static Data Deserialize(int metricCount, BinaryReader reader) {
            int sampleCount = reader.ReadInt32();
            var data = new Data(metricCount, sampleCount);

            for (int i = 0; i < sampleCount; i++) {
                double[] newValues = new double[metricCount];

                for (int j = 0; j < metricCount; j++)
                    newValues[j] = reader.ReadDouble();

                data.DataSamples[i] = new DataSample(newValues, reader.ReadDouble());
            }

            return data;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(DataSamples.Length);

            foreach (var sample in DataSamples) {
                double[] values = sample.Values;

                for (int j = 0; j < metricCount; j++)
                    writer.Write(values[j]);
                
                writer.Write(sample.Weight);
            }
        }

        public void Normalize(double[] baseCoefficients) {
            foreach (var sample in DataSamples) {
                double[] values = sample.Values;
                
                for (int i = 0; i < metricCount; i++)
                    values[i] = Math.Sqrt(Math.Min(baseCoefficients[i] * values[i], 1d));
            }
        }

        public void Clamp(int metricIndex, double min, double max) {
            foreach (var sample in DataSamples) {
                double[] values = sample.Values;

                values[metricIndex] = MathU.Clamp(values[metricIndex], min, max);
            }
        }

        public double GetQuantile(int metricIndex, double quantile) {
            var sorted = new DataSample[DataSamples.Length];

            for (int i = 0; i < DataSamples.Length; i++)
                sorted[i] = DataSamples[i];
                
            Array.Sort(sorted, new DataSample.Comparer(metricIndex));

            double[] cumulativeWeights = new double[sorted.Length];
            double sum = 0d;

            for (int i = 0; i < sorted.Length; i++) {
                sum += sorted[i].Weight;
                cumulativeWeights[i] = sum;
            }

            double targetTotal = quantile * sum;
            var first = sorted[0];
                
            if (targetTotal < 0.5f * first.Weight)
                return first.Values[metricIndex];

            var last = sorted[sorted.Length - 1];

            if (targetTotal > sum - 0.5f * last.Weight)
                return last.Values[metricIndex];
                
            for (int i = 0; i < sorted.Length - 1; i++) {
                double end = cumulativeWeights[i] + 0.5f * sorted[i + 1].Weight;
                    
                if (end < targetTotal)
                    continue;

                double start = cumulativeWeights[i] - 0.5f * sorted[i].Weight;

                return MathU.Remap(targetTotal, start, end, sorted[i].Values[metricIndex], sorted[i + 1].Values[metricIndex]);
            }

            return sorted[sorted.Length - 1].Values[metricIndex];
        }

        public double GetMaxValue(int metricIndex) {
            double max = 0d;
            
            foreach (var sample in DataSamples) {
                if (sample.Values[metricIndex] > max)
                    max = sample.Values[metricIndex];
            }

            return max;
        }
    }
}