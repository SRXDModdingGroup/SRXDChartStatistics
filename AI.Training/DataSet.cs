using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AI.Processing;

namespace AI.Training {
    public class DataSet {
        private static readonly int BATCH_COUNT = 4;
        
        public int Size { get; }
        
        public DataWrapper[] Data { get; }

        private int sampleSize;
        private int batchSize;
        private double[] results;
        private double[] weightScales;
        private Matrix[] valueVectors;
        private Matrix[] weightVectors;
        private Matrix overallValueVector;
        private Matrix overallWeightVector;

        private DataSet(int size, int sampleSize, int matrixDimensions) {
            Size = size;
            Data = new DataWrapper[size];
            this.sampleSize = sampleSize;
            batchSize = Size / BATCH_COUNT + 1;
            results = new double[size];
            weightScales = new double[size];
            valueVectors = new Matrix[batchSize];
            weightVectors = new Matrix[batchSize];
            overallValueVector = new Matrix(sampleSize, matrixDimensions);
            overallWeightVector = new Matrix(sampleSize, matrixDimensions);
        }

        public static DataSet Create(int size, int sampleSize, int matrixDimensions, IList<(Data, double)> dataList) {
            var dataSet = new DataSet(size, sampleSize, matrixDimensions);

            for (int i = 0; i < size; i++) {
                (var data, double expectedResult) = dataList[i];

                dataSet.Data[i] = new DataWrapper(data, expectedResult, matrixDimensions);
            }

            return dataSet;
        }

        public static DataSet Deserialize(BinaryReader reader, int matrixDimensions) {
            int size = reader.ReadInt32();
            int sampleSize = reader.ReadInt32();
            var dataSet = new DataSet(size, sampleSize, matrixDimensions);

            for (int i = 0; i < size; i++)
                dataSet.Data[i] = DataWrapper.Deserialize(reader, matrixDimensions);

            return dataSet;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(Size);
            writer.Write(sampleSize);

            foreach (var data in Data)
                data.Serialize(writer);
        }

        public void Trim(double localLimit, double globalLimit) {
            int totalSize = 0;

            foreach (var data in Data)
                totalSize += data.Size;

            double[] values = new double[totalSize];

            for (int i = 0; i < sampleSize; i++) {
                int counter = 0;
                
                foreach (var data in Data) {
                    foreach (var sample in data.Samples) {
                        values[counter] = sample.Values[i];
                        counter++;
                    }
                }
                
                Array.Sort(values);

                double limit = values[(int) (globalLimit * (totalSize - 1))];

                foreach (var data in Data)
                    data.Clamp(i, Math.Min(data.GetQuantile(i, localLimit), limit));
            }
        }

        public void Normalize(double[] scales, double[] powers) {
            foreach (var data in Data)
                data.Normalize(scales, powers);
        }

        public double Adjust(Matrix valueMatrix, Matrix weightMatrix, double approachFactor, double vectorMagnitude, Random random) {
            Shuffle(random);
            Parallel.For(0, Size, i => results[i] = Data[i].GetResult(valueMatrix, weightMatrix, out weightScales[i]));
            GetRegression(out double scale, out double bias);
            
            double totalError = 0d;

            for (int i = 0; i < BATCH_COUNT; i++) {
                int batchStart = batchSize * i;
                int batchEnd;

                if (i == BATCH_COUNT - 1)
                    batchEnd = Size;
                else
                    batchEnd = batchStart + batchSize;

                Parallel.For(batchStart, batchEnd, j => Data[j].GetVectors(valueMatrix, weightMatrix, weightScales[j], out valueVectors[j - batchStart], out weightVectors[j - batchStart]));
                MatrixExtensions.Zero(overallValueVector);
                MatrixExtensions.Zero(overallWeightVector);

                for (int j = batchStart; j < batchEnd; j++) {
                    double error = Data[j].ExpectedResult - scale * (results[j] + bias);
                    double sqError = error * error;
                    double vectorWeight = Math.Sign(error) * sqError;
                    
                    MatrixExtensions.AddWeighted(overallValueVector, vectorWeight, valueVectors[j - batchStart]);
                    MatrixExtensions.AddWeighted(overallWeightVector, vectorWeight, weightVectors[j - batchStart]);
                    
                    totalError += sqError;
                }
            }

            MatrixExtensions.AddWeighted(valueMatrix, approachFactor * vectorMagnitude / overallValueVector.Magnitude(), overallValueVector);
            MatrixExtensions.AddWeighted(weightMatrix, approachFactor * vectorMagnitude / overallWeightVector.Magnitude(), overallWeightVector);
            valueMatrix.Coefficients[valueMatrix.TotalSize - 1] = 0d;
            MatrixExtensions.Normalize(valueMatrix);
            MatrixExtensions.Normalize(weightMatrix);

            return 1d - Math.Sqrt(totalError / Size);
        }

        public void GetBaseCoefficients(out double[] baseCoefficients, out double[] powers) {
            baseCoefficients = new double[sampleSize];

            for (int i = 0; i < sampleSize; i++) {
                double max = 0d;

                foreach (var data in Data) {
                    double newMax = data.GetMaxValue(i);

                    if (newMax > max)
                        max = newMax;
                }
                
                baseCoefficients[i] = 1d / max;
            }

            int count = 0;

            foreach (var data in Data)
                count += data.Size;
            
            double[] values = new double[count];

            powers = new double[sampleSize];

            for (int i = 0; i < sampleSize; i++) {
                int counter = 0;

                foreach (var data in Data) {
                    foreach (var sample in data.Samples) {
                        values[counter] = sample.Values[i];
                        counter++;
                    }
                }
                
                Array.Sort(values);

                double min = 0d;
                double max = 2d;
                double bestPow = 1d;
                double bestError = double.PositiveInfinity;
                double baseCoeff = baseCoefficients[i];

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
        }

        public double[] GetResults(Matrix valueMatrix, Matrix weightMatrix, out double scale, out double bias) {
            Parallel.For(0, Size, i => results[i] = Data[i].GetResult(valueMatrix, weightMatrix, out _));
            GetRegression(out scale, out bias);

            for (int i = 0; i < Size; i++)
                results[i] = scale * (results[i] + bias);

            return results;
        }

        private void Shuffle(Random random) {
            for (int i = Size - 1; i > 2; i--) {
                int index = random.Next(i - 1);

                (Data[i], Data[index]) = (Data[index], Data[i]);
            }
        }

        private void GetRegression(out double scale, out double bias) {
            double sx = 0d;
            double sy = 0d;
            double sxx = 0d;
            double sxy = 0d;

            for (int i = 0; i < Size; i++) {
                double expected = Data[i].ExpectedResult;
                double returned = results[i];

                sx += expected;
                sy += returned;
                sxx += expected * expected;
                sxy += expected * returned;
            }

            scale = (Size * sxx - sx * sx) / (Size * sxy - sx * sy);
            bias = (sy * sxx - sxy * sx) / (sx * sx - Size * sxx);
        }
    }
}