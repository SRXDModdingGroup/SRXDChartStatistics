using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MatrixAI.Processing;

namespace MatrixAI.Training {
    public class DataSet {
        public int Size { get; }
        
        public DataWrapper[] Data { get; }

        private int sampleSize;
        private int matrixDimensions;
        private double sumExpected;
        private double sumExpected2;
        private double[] results;
        private Matrix[] vectors;
        private Matrix overallVector;

        private DataSet(int size, int sampleSize, int matrixDimensions) {
            Size = size;
            Data = new DataWrapper[size];
            this.sampleSize = sampleSize;
            this.matrixDimensions = matrixDimensions;
            results = new double[size];
            vectors = new Matrix[size];
            overallVector = new Matrix(sampleSize, matrixDimensions);
        }

        public static DataSet Create(int size, int sampleSize, int matrixDimensions, IList<(Data, double)> dataList) {
            var dataSet = new DataSet(size, sampleSize, matrixDimensions);

            for (int i = 0; i < size; i++) {
                (var data, double expectedResult) = dataList[i];

                dataSet.Data[i] = DataWrapper.Create(data, expectedResult, matrixDimensions);
                dataSet.sumExpected += expectedResult;
                dataSet.sumExpected2 += expectedResult * expectedResult;
            }

            return dataSet;
        }

        public static DataSet Deserialize(BinaryReader reader, int matrixDimensions) {
            int size = reader.ReadInt32();
            int sampleSize = reader.ReadInt32();
            var dataSet = new DataSet(size, sampleSize, matrixDimensions);

            dataSet.sumExpected = reader.ReadDouble();
            dataSet.sumExpected2 = reader.ReadDouble();

            for (int i = 0; i < size; i++)
                dataSet.Data[i] = DataWrapper.Deserialize(reader, matrixDimensions);

            return dataSet;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(Size);
            writer.Write(sampleSize);
            writer.Write(sumExpected);
            writer.Write(sumExpected2);

            foreach (var data in Data)
                data.Serialize(writer);
        }
        
        public void Trim(double upperQuantile) {
            foreach (var data in Data) {
                for (int i = 0; i < sampleSize; i++)
                    data.Clamp(i, data.GetQuantile(i, upperQuantile));
            }
        }
        
        public void Normalize(double[] baseCoefficients) {
            foreach (var data in Data)
                data.Normalize(baseCoefficients);
        }

        public double[] GetBaseCoefficients() {
            double[] baseCoefficients = new double[sampleSize];

            for (int i = 0; i < sampleSize; i++) {
                double max = 0d;

                foreach (var data in Data) {
                    double newMax = data.GetMaxValue(i);

                    if (newMax > max)
                        max = newMax;
                }
                
                baseCoefficients[i] = 1d / max;
            }

            return baseCoefficients;
        }
        
        public double[] GetResults(Matrix matrix, out double scale, out double bias) {
            for (int i = 0; i < Size; i++)
                results[i] = Data[i].GetResultAndVector(matrix, out vectors[i]);

            //Parallel.For(0, Size, i => results[i] = Data[i].GetResultAndVector(matrix, out vectors[i]));
            
            double sumReturned = 0d;
            double sumProduct = 0d;
            int count = 0;
            
            for (int i = 0; i < Size; i++) {
                var data = Data[i];
                double result = results[i];
                
                sumReturned += result;
                sumProduct += result * data.ExpectedResult;
                count += data.Samples.Count;
            }
            
            scale = (count * sumExpected2 - sumExpected * sumExpected) / (count * sumProduct - sumExpected * sumReturned);
            bias = (sumReturned * sumExpected2 - sumProduct * sumExpected) / (sumExpected * sumExpected - count * sumExpected2);
            
            for (int i = 0; i < Size; i++) {
                double adjusted = scale * (results[i] + bias);
                
                results[i] = adjusted;
            }
            
            return results;
        }

        public double GetFitnessAndVector(Matrix matrix, out Matrix vector) {
            Parallel.For(0, Size, i => results[i] = Data[i].GetResultAndVector(matrix, out vectors[i]));
            
            double sumReturned = 0d;
            double sumProduct = 0d;
            int count = 0;
            
            for (int i = 0; i < Size; i++) {
                var data = Data[i];
                double result = results[i];
                
                sumReturned += result;
                sumProduct += result * data.ExpectedResult;
                count += data.Samples.Count;
            }

            vector = overallVector;
            MatrixExtensions.Zero(vector);
            
            double scale = (count * sumExpected2 - sumExpected * sumExpected) / (count * sumProduct - sumExpected * sumReturned);
            double bias = (sumReturned * sumExpected2 - sumProduct * sumExpected) / (sumExpected * sumExpected - count * sumExpected2);
            double sumSqError = 0d;

            for (int i = 0; i < Size; i++) {
                double error = Data[i].ExpectedResult - scale * (results[i] + bias);
                double sqError = error * error;
                
                MatrixExtensions.AddWeighted(vector, Math.Sign(error) * sqError, vectors[i]);
                sumSqError += sqError;
            }
            
            return 1d - Math.Sqrt(sumSqError / Size);
        }
    }
}