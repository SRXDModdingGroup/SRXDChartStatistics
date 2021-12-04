using System;
using MatrixAI.Processing;

namespace MatrixAI.Training {
    public class Superset {
        public int TotalSize { get; }
        
        public DataSet[] DataSets { get; }
        
        public double[] ExpectedResults { get; }
        
        private double[] results;
        private Matrix overallVector;

        public Superset(DataSet[] dataSets, int sampleSize) {
            DataSets = dataSets;
            TotalSize = 0;
            results = new double[TotalSize];
            ExpectedResults = new double[TotalSize];

            int i = 0;

            foreach (var dataSet in dataSets) {
                TotalSize += dataSet.Size;

                foreach (var data in dataSet.Data) {
                    ExpectedResults[i] = data.ExpectedResult;
                    i++;
                }
            }
            
            overallVector = new Matrix(sampleSize);
        }

        public double GetResult(Matrix matrix, out Matrix vector, out double[] results) {
            double totalError = 0d;
            int i = 0;

            vector = overallVector;
            results = this.results;
            MatrixExtensions.Zero(vector);
            
            foreach (var dataSet in DataSets) {
                totalError += dataSet.GetResult(matrix, vector, out double[] resultsForSet);

                for (int j = 0; j < dataSet.Size; j++) {
                    results[i] = resultsForSet[j];
                    i++;
                }
            }

            return 1d - Math.Sqrt(totalError / i);
        }
    }
}