using System;
using System.Collections.Generic;
using System.IO;
using Util;

namespace ChartAutoRating {
    public class Data {
        private readonly struct TempDataSample {
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
            var enumerators = new IEnumerator<(double, double)>[metricCount];
            bool[] remaining = new bool[metricCount];

            for (int i = 0; i < metricCount; i++) {
                var enumerator = selector(i).GetEnumerator();
                
                enumerators[i] = enumerator;
                remaining[i] = enumerator.MoveNext();
            }

            var tempSamples = new List<TempDataSample>();
            double[] currentValues = new double[metricCount];
            double currentTime = -1d;
            bool firstFound = false;
            bool anyRemaining;

            do {
                int soonestIndex = -1;
                double soonestTime = double.MaxValue;

                anyRemaining = false;

                for (int i = 0; i < enumerators.Length; i++) {
                    if (!remaining[i])
                        continue;

                    var enumerator = enumerators[i];
                    double time = enumerator.Current.Item2;

                    if (time >= soonestTime)
                        continue;

                    soonestIndex = i;
                    soonestTime = time;
                    anyRemaining = true;
                }
                
                if (!anyRemaining)
                    continue;

                var soonest = enumerators[soonestIndex];

                if (!firstFound || !MathU.AlmostEquals(soonestTime, currentTime)) {
                    if (firstFound) {
                        double[] newValues = new double[metricCount];
                        
                        Array.Copy(currentValues, newValues, metricCount);
                        tempSamples.Add(new TempDataSample(newValues, currentTime));
                    }

                    currentTime = soonestTime;
                    firstFound = true;
                }

                currentValues[soonestIndex] = soonest.Current.Item1;
                remaining[soonestIndex] = soonest.MoveNext();
            } while (anyRemaining);
            
            tempSamples.Add(new TempDataSample(currentValues, currentTime));

            var data = new Data(metricCount, tempSamples.Count - 1);
            var samples = data.DataSamples;
            double scale = 1d / (tempSamples[tempSamples.Count - 1].Time - tempSamples[0].Time);

            for (int i = 0; i < samples.Length; i++) {
                var tempSample = tempSamples[i];

                samples[i] = new DataSample(tempSample.Values, tempSample.Time, scale * (tempSamples[i + 1].Time - tempSample.Time));
            }

            return data;
        }

        public static Data Deserialize(int metricCount, BinaryReader reader) {
            int sampleCount = reader.ReadInt32();
            var data = new Data(metricCount, sampleCount);

            for (int i = 0; i < sampleCount; i++) {
                double[] newValues = new double[metricCount];

                for (int j = 0; j < metricCount; j++)
                    newValues[j] = reader.ReadDouble();

                double time = reader.ReadDouble();
                
                data.DataSamples[i] = new DataSample(newValues, time, reader.ReadDouble());
            }

            return data;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(DataSamples.Length);

            foreach (var sample in DataSamples) {
                double[] values = sample.Values;

                for (int j = 0; j < metricCount; j++)
                    writer.Write(values[j]);
                
                writer.Write(sample.Time);
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

        public void Clamp(int metricIndex, double max) {
            foreach (var sample in DataSamples) {
                double[] values = sample.Values;

                values[metricIndex] = Math.Min(values[metricIndex], max);
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