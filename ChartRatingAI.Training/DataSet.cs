using System;
using System.Collections.Generic;
using System.IO;
using AI.Training;

namespace ChartRatingAI.Training {
    public class DataSet : DataSet<Data> {
        private int sampleSize;

        public DataSet(int size, int sampleSize, int batchCount, IList<(Data, double)> dataList)
            : this(size, sampleSize, batchCount) {
            for (int i = 0; i < size; i++) {
                (var data, double expectedResult) = dataList[i];

                Data[i] = new DataPair<Data, double>(data, expectedResult);
            }
        }
        
        private DataSet(int size, int sampleSize, int batchCount) : base(size, batchCount) => this.sampleSize = sampleSize;

        public static DataSet Deserialize(BinaryReader reader, int batchCount) {
            int size = reader.ReadInt32();
            int sampleSize = reader.ReadInt32();
            var dataSet = new DataSet(size, sampleSize, batchCount);

            for (int i = 0; i < size; i++) {
                var data = Training.Data.Deserialize(reader);
                double expectedResult = reader.ReadDouble();

                dataSet.Data[i] = new DataPair<Data, double>(data, expectedResult);
            }

            return dataSet;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(Size);
            writer.Write(sampleSize);

            foreach (var pair in Data) {
                pair.Data.Serialize(writer);
                writer.Write(pair.ExpectedResult);
            }
        }

        public void Trim(double localLimit, double globalLimit, out double[] limits) {
            int totalSize = 0;

            foreach (var pair in Data)
                totalSize += pair.Data.Size;

            double[] values = new double[totalSize];

            limits = new double[sampleSize];

            for (int i = 0; i < sampleSize; i++) {
                int counter = 0;
                
                foreach (var pair in Data) {
                    foreach (var sample in pair.Data.Samples) {
                        values[counter] = sample.Values[i];
                        counter++;
                    }
                }
                
                Array.Sort(values);

                double limit = values[(int) (globalLimit * (totalSize - 1))];

                limits[i] = limit;

                foreach (var pair in Data)
                    pair.Data.Clamp(i, Math.Min(pair.Data.GetQuantile(i, localLimit), limit));
            }
        }

        public void Normalize(out double[] scales, out double[] powers) {
            scales = new double[sampleSize];

            for (int i = 0; i < sampleSize; i++) {
                double max = 0d;

                foreach (var pair in Data) {
                    foreach (var sample in pair.Data.Samples) {
                        double newMax = sample.Values[i];
                        
                        if (newMax > max)
                            max = newMax;
                    }
                }
                
                scales[i] = 1d / max;
            }

            int count = 0;

            foreach (var pair in Data)
                count += pair.Data.Size;
            
            double[] values = new double[count];

            powers = new double[sampleSize];

            for (int i = 0; i < sampleSize; i++) {
                int counter = 0;

                foreach (var pair in Data) {
                    foreach (var sample in pair.Data.Samples) {
                        values[counter] = sample.Values[i];
                        counter++;
                    }
                }
                
                Array.Sort(values);

                double min = 0d;
                double max = 2d;
                double bestPow = 1d;
                double bestError = double.PositiveInfinity;
                double baseCoeff = scales[i];

                for (int j = 0; j < 16; j++) {
                    double sumError = 0d;
                    double pow = 0.5d * (min + max);

                    for (int k = 0; k < count; k++) {
                        double error = (double) k / (count - 1) - Math.Pow(baseCoeff * values[k], pow);
                        double sqError = error * error;

                        sumError += Math.Sign(error) * sqError;
                    }

                    if (sumError > 0d)
                        max = 0.5d * (max + pow);
                    else
                        min = 0.5d * (min + pow);

                    if (Math.Abs(sumError) > bestError)
                        continue;
                    
                    bestError = Math.Abs(sumError);
                    bestPow = pow;
                }

                powers[i] = bestPow;
            }

            foreach (var pair in Data)
                pair.Data.Normalize(scales, powers);
        }
    }
}