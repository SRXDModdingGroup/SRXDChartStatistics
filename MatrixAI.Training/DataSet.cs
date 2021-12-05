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

        public static DataSet Deserialize(BinaryReader reader) {
            int size = reader.ReadInt32();
            int sampleSize = reader.ReadInt32();
            int matrixDimensions = reader.ReadInt32();
            var dataSet = new DataSet(size, sampleSize, matrixDimensions);

            dataSet.sumExpected = reader.ReadDouble();
            dataSet.sumExpected2 = reader.ReadDouble();

            for (int i = 0; i < size; i++)
                dataSet.Data[i] = DataWrapper.Deserialize(reader);

            return dataSet;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(Size);
            writer.Write(sampleSize);
            writer.Write(matrixDimensions);
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

        public double GetResult(Matrix matrix, out Matrix vector, out double[] results, out double scale, out double bias) {
            Parallel.For(0, Size, i => this.results[i] = Data[i].GetResultAndVector(matrix, out vectors[i]));
            results = this.results;
            
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
            scale = (count * sumExpected2 - sumExpected * sumExpected) / (count * sumProduct - sumExpected * sumReturned);
            bias = (sumReturned * sumExpected2 - sumProduct * sumExpected) / (sumExpected * sumExpected - count * sumExpected2);
            
            double sumSqError = 0d;

            for (int i = 0; i < Size; i++) {
                double adjusted = scale * (results[i] + bias);
                
                results[i] = adjusted;

                double error = Data[i].ExpectedResult - adjusted;
                double sqError = error * error;
                
                MatrixExtensions.AddWeighted(vector, Math.Sign(error) * sqError, vectors[i]);
                sumSqError += sqError;
            }
            
            return 1d - Math.Sqrt(sumSqError / Size);
        }
    }
}