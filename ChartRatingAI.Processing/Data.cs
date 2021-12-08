using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Util;

namespace ChartRatingAI.Processing {
    public class Data {
        public string Name { get; }
        
        public int Size { get; }

        public int SampleSize { get; }

        public DataSample[] Samples { get; }

        public Data(string name, int sampleSize, DataSample[] samples) {
            Name = name;
            Size = samples.Length;
            SampleSize = sampleSize;
            Samples = samples;
        }

        public static Data Deserialize(BinaryReader reader) =>
            Deserialize(reader, (name, size, sampleSize) => new Data(name, size, sampleSize));

        protected Data(string name, int size, int sampleSize) {
            Name = name;
            Size = size;
            SampleSize = sampleSize;
            Samples = new DataSample[size];
        }

        public void Clamp(int valueIndex, double max) {
            foreach (var sample in Samples) {
                double[] values = sample.Values;

                values[valueIndex] = Math.Min(values[valueIndex], max);
            }
        }

        public void Trim(double localLimit, double[] globalLimits) {
            for (int i = 0; i < SampleSize; i++)
                Clamp(i, Math.Min(GetQuantile(i, localLimit), globalLimits[i]));
        }

        public void Normalize(double[] scales, double[] powers) {
            foreach (var sample in Samples) {
                for (int i = 0; i < SampleSize; i++)
                    sample.Values[i] = Math.Pow(scales[i] * sample.Values[i], powers[i]);
            }
        }

        public double GetQuantile(int valueIndex, double quantile) {
            var sorted = new DataSample[Samples.Length];

            for (int i = 0; i < Samples.Length; i++)
                sorted[i] = Samples[i];
                
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

        protected static T Deserialize<T>(BinaryReader reader, Func<string, int, int, T> constructor) where T : Data {
            string name = reader.ReadString();
            int size = reader.ReadInt32();
            int sampleSize = reader.ReadInt32();
            var data = constructor(name, size, sampleSize);
            
            for (int i = 0; i < size; i++) {
                double[] newValues = new double[sampleSize];

                for (int j = 0; j < sampleSize; j++)
                    newValues[j] = reader.ReadDouble();

                double weight = reader.ReadDouble();
                
                data.Samples[i] = new DataSample(newValues, weight);
            }

            return data;
        }
    }
}