using System;
using System.Collections.Generic;
using MatrixAI.Processing;

namespace MatrixAI.Training {
    public class DataSet {
        public int Size { get; }
        
        public DataWrapper[] Data { get; }

        private double sumExpected;
        private double sumExpected2;
        private double[] results;
        private Matrix[] vectors;

        private DataSet(int size) {
            Size = size;
            Data = new DataWrapper[size];
            results = new double[size];
            vectors = new Matrix[size];
        }

        public static DataSet Create(int setSize, Func<int, (Data, double)> selector) {
            var dataSet = new DataSet(setSize);

            for (int i = 0; i < setSize; i++) {
                (var data, double expectedResult) = selector(i);

                dataSet.Data[i] = new DataWrapper(data, expectedResult);
                dataSet.sumExpected += expectedResult;
                dataSet.sumExpected2 += expectedResult * expectedResult;
            }

            return dataSet;
        }

        public double GetResult(Matrix matrix, Matrix vector, out double[] results) {
            double sumSqError = 0d;
            double sumReturned = 0d;
            double sumProduct = 0d;
            int count = 0;

            results = this.results;

            for (int i = 0; i < Size; i++) {
                var data = Data[i];
                double result = data.GetResultAndVector(matrix, out vectors[i]);

                results[i] = result;
                sumReturned += result;
                sumProduct += result * data.ExpectedResult;
                count += data.Samples.Count;
            }

            double scale = (count * sumExpected2 - sumExpected * sumExpected) / (count * sumProduct - sumExpected * sumReturned);
            double bias = (sumReturned * sumExpected2 - sumProduct * sumExpected) / (sumExpected * sumExpected - count * sumExpected2);

            for (int i = 0; i < Size; i++) {
                double adjusted = scale * (results[i] + bias);
                
                results[i] = adjusted;

                double error = Data[i].ExpectedResult - adjusted;
                double sqError = error * error;
                
                MatrixExtensions.AddWeighted(vector, Math.Sign(error) * sqError, vectors[i]);
                sumSqError += sqError;
            }

            return sumSqError;
        }
    }
}