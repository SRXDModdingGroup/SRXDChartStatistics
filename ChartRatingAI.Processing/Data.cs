using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Util;

namespace ChartRatingAI.Processing {
    public class Data {
        private readonly struct TempDataSample {
            public double[] Values { get; }
        
            public double Time { get; }

            public TempDataSample(double[] values, double time) {
                Values = values;
                Time = time;
            }
        }
        
        public string Name { get; }
        
        public int Size { get; }

        public int SampleSize { get; }
        
        public ReadOnlyCollection<DataSample> Samples { get; }

        protected DataSample[] SamplesArray { get; }

        protected Data(string name, int size, int sampleSize) {
            Name = name;
            Size = size;
            SampleSize = sampleSize;
            SamplesArray = new DataSample[size];
            Samples = new ReadOnlyCollection<DataSample>(SamplesArray);
        }

        public static Data Create(string name, int sampleSize, Func<int, IEnumerable<(double, double)>> selector) =>
            Create(name, sampleSize, selector, (n, ss, s) => new Data(n, s, ss));

        public void Clamp(int valueIndex, double max) {
            foreach (var sample in SamplesArray) {
                double[] values = sample.Values;

                values[valueIndex] = Math.Min(values[valueIndex], max);
            }
        }

        public void Normalize(double[] scales, double[] powers) {
            foreach (var sample in SamplesArray) {
                for (int i = 0; i < SampleSize; i++)
                    sample.Values[i] = Math.Pow(scales[i] * sample.Values[i], powers[i]);
            }
        }

        public double GetQuantile(int valueIndex, double quantile) {
            var sorted = new DataSample[SamplesArray.Length];

            for (int i = 0; i < SamplesArray.Length; i++)
                sorted[i] = SamplesArray[i];
                
            Array.Sort(sorted, new DataSample.Comparer(valueIndex));

            double[] cumulativeWeights = new double[sorted.Length];
            double sum = 0d;

            for (int i = 0; i < sorted.Length; i++) {
                sum += sorted[i].Weight;
                cumulativeWeights[i] = sum;
            }

            double targetTotal = quantile * sum;
            var first = sorted[0];
                
            if (targetTotal < 0.5f * first.Weight)
                return first.Values[valueIndex];

            var last = sorted[sorted.Length - 1];

            if (targetTotal > sum - 0.5f * last.Weight)
                return last.Values[valueIndex];
                
            for (int i = 0; i < sorted.Length - 1; i++) {
                double end = cumulativeWeights[i] + 0.5f * sorted[i + 1].Weight;
                    
                if (end < targetTotal)
                    continue;

                double start = cumulativeWeights[i] - 0.5f * sorted[i].Weight;

                return MathU.Remap(targetTotal, start, end, sorted[i].Values[valueIndex], sorted[i + 1].Values[valueIndex]);
            }

            return sorted[sorted.Length - 1].Values[valueIndex];
        }

        protected static T Create<T>(string name, int sampleSize, Func<int, IEnumerable<(double, double)>> selector,
            Func<string, int, int, T> constructor) where T : Data {
            var enumerators = new IEnumerator<(double, double)>[sampleSize];
            bool[] remaining = new bool[sampleSize];

            for (int i = 0; i < sampleSize; i++) {
                var enumerator = selector(i).GetEnumerator();
                
                enumerators[i] = enumerator;
                remaining[i] = enumerator.MoveNext();
            }

            var tempSamples = new List<TempDataSample>();
            double[] currentValues = new double[sampleSize];
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
                        double[] newValues = new double[sampleSize];
                        
                        Array.Copy(currentValues, newValues, sampleSize);
                        tempSamples.Add(new TempDataSample(newValues, currentTime));
                    }

                    currentTime = soonestTime;
                    firstFound = true;
                }

                currentValues[soonestIndex] = soonest.Current.Item1;
                remaining[soonestIndex] = soonest.MoveNext();
            } while (anyRemaining);
            
            tempSamples.Add(new TempDataSample(currentValues, currentTime));

            var data = constructor(name, tempSamples.Count - 1, sampleSize);
            var samples = data.SamplesArray;
            double scale = 1d / (tempSamples[tempSamples.Count - 1].Time - tempSamples[0].Time);

            for (int i = 0; i < data.Size; i++) {
                var tempSample = tempSamples[i];

                samples[i] = new DataSample(tempSample.Values, scale * (tempSamples[i + 1].Time - tempSample.Time));
            }

            return data;
        }
    }
}