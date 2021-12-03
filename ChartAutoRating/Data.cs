﻿using System;
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

        private int sampleSize;
        private DataSample[] samples;
        private Matrix cachedVector;

        private Data(int sampleSize, int sampleCount) {
            this.sampleSize = sampleSize;
            samples = new DataSample[sampleCount];
            cachedVector = new Matrix(sampleSize);
        }

        public static Data Create(int sampleSize, Func<int, IEnumerable<(double, double)>> selector) {
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

            var data = new Data(sampleSize, tempSamples.Count - 1);
            var samples = data.samples;
            double scale = 1d / (tempSamples[tempSamples.Count - 1].Time - tempSamples[0].Time);

            for (int i = 0; i < samples.Length; i++) {
                var tempSample = tempSamples[i];

                samples[i] = new DataSample(tempSample.Values, scale * (tempSamples[i + 1].Time - tempSample.Time));
            }

            return data;
        }

        public static Data Deserialize(BinaryReader reader) {
            int sampleSize = reader.ReadInt32();
            int sampleCount = reader.ReadInt32();
            var data = new Data(sampleSize, sampleCount);

            for (int i = 0; i < sampleCount; i++) {
                double[] newValues = new double[sampleSize];

                for (int j = 0; j < sampleSize; j++)
                    newValues[j] = reader.ReadDouble();

                double weight = reader.ReadDouble();
                
                data.samples[i] = new DataSample(newValues, weight);
            }

            return data;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(samples.Length);

            foreach (var sample in samples) {
                double[] values = sample.Values;

                for (int j = 0; j < sampleSize; j++)
                    writer.Write(values[j]);
                
                writer.Write(sample.Weight);
            }
        }

        public void Normalize(double[] baseCoefficients) {
            foreach (var sample in samples) {
                double[] values = sample.Values;
                
                for (int i = 0; i < sampleSize; i++)
                    values[i] = Math.Sqrt(Math.Min(baseCoefficients[i] * values[i], 1d));
            }
        }

        public void Clamp(int metricIndex, double max) {
            foreach (var sample in samples) {
                double[] values = sample.Values;

                values[metricIndex] = Math.Min(values[metricIndex], max);
            }
        }

        public double GetResult(Matrix matrix) {
            double sumValue = 0d;
            double sumWeight = 0d;

            foreach (var sample in samples) {
                double value = 0d;
                double weight = 0d;
                
                for (int i = 0; i < sampleSize; i++) {
                    double a = sample.Values[i];

                    weight += Coefficients.Compute(a * a, matrix.WeightCoefficients[i]);
                }

                weight *= sample.Weight;
                
                for (int i = 0; i < sampleSize; i++) {
                    double a = sample.Values[i];

                    for (int j = i; j < sampleSize; j++)
                        value += Coefficients.Compute(a * sample.Values[j], matrix.ValueCoefficients[i, j]);
                }

                sumValue += weight * value;
                sumWeight += weight;
            }

            return sumValue / sumWeight;
        }

        public double GetResultAndVector(Matrix matrix, out Matrix vector) {
            double sumValue = 0d;
            double sumWeight = 0d;
            
            vector = cachedVector;

            for (int i = 0; i < sampleSize; i++) {
                for (int j = i; j < sampleSize; j++)
                    vector.ValueCoefficients[i, j] = new Coefficients(0d, 0d, 0d, 0d, 0d);

                vector.WeightCoefficients[i] = new Coefficients(0d, 0d, 0d, 0d, 0d);
            }

            foreach (var sample in samples) {
                double value = 0d;
                double weight = 0d;
                var sampleVector = sample.Vector;
                
                for (int i = 0; i < sampleSize; i++) {
                    double a = sample.Values[i];

                    for (int j = i; j < sampleSize; j++)
                        value += Coefficients.Compute(a * sample.Values[j], matrix.ValueCoefficients[i, j]);
                }
                
                for (int i = 0; i < sampleSize; i++) {
                    double a = sample.Values[i];

                    weight += Coefficients.Compute(a * a, matrix.WeightCoefficients[i]);
                }

                weight *= sample.Weight;
                
                for (int i = 0; i < sampleSize; i++) {
                    for (int j = i; j < sampleSize; j++)
                        vector.ValueCoefficients[i, j] += weight * sampleVector.ValueCoefficients[i, j];

                    vector.WeightCoefficients[i] += weight * value * sampleVector.WeightCoefficients[i];
                }

                sumValue += weight * value;
                sumWeight += weight;
            }

            double scale = 1d / sumWeight;

            for (int i = 0; i < sampleSize; i++) {
                for (int j = i; j < sampleSize; j++)
                    vector.ValueCoefficients[i, j] *= scale;

                vector.WeightCoefficients[i] *= scale;
            }

            return scale * sumValue;
        }

        public double GetQuantile(int metricIndex, double quantile) {
            var sorted = new DataSample[samples.Length];

            for (int i = 0; i < samples.Length; i++)
                sorted[i] = samples[i];
                
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
            
            foreach (var sample in samples) {
                if (sample.Values[metricIndex] > max)
                    max = sample.Values[metricIndex];
            }

            return max;
        }
    }
}