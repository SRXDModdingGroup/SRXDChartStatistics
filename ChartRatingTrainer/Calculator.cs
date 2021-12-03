using System;
using System.IO;
using System.Threading.Tasks;
using MatrixAI.Processing;

namespace ChartRatingTrainer {
    public class Calculator {
        private Matrix matrix;
        
        private Calculator(Matrix matrix) {
            this.matrix = matrix;
        }

        public static Calculator Deserialize(BinaryReader reader) => new Calculator(Matrix.Deserialize(reader));

        public void Serialize(BinaryWriter writer) => matrix.Serialize(writer);

        public static Calculator Random(int matrixSize, Random random) {
            var calculator = new Calculator(new Matrix(matrixSize));
            var matrix = calculator.matrix;
            
            for (int i = 0; i < matrixSize; i++) {
                for (int j = i; j < matrixSize; j++)
                    matrix.ValueCoefficients[i, j] = Coefficients.Random(random);

                matrix.WeightCoefficients[i] = Coefficients.Random(random);
            }

            Matrix.Normalize(matrix, matrix);

            return calculator;
        }
        
        public void CacheResults(DataSet dataSet) {
            var expectedReturned = dataSet.ExpectedReturned;

            dataSet.InitValuePairs();
            Parallel.For(0, dataSet.Size, i => expectedReturned[i].Returned = dataSet.Datas[i].GetResult(matrix));

            int count = expectedReturned.Length;
            double sx = 0d;
            double sy = 0d;
            double sxx = 0d;
            double sxy = 0d;
            
            foreach (var pair in expectedReturned) {
                double x = pair.Expected;
                double y = pair.Returned;

                sx += x;
                sy += y;
                sxx += x * x;
                sxy += x * y;
            }

            dataSet.Scale = (count * sxy - sx * sy) / (count * sxx - sx * sx);
            dataSet.Bias = (sxy * sx - sy * sxx) / (sx * sx - count * sxx);

            foreach (var pair in expectedReturned)
                pair.Returned = (pair.Returned - dataSet.Bias) / dataSet.Scale;
        }

        public double CalculateFitness(DataSet[] dataSets) {
            double sum = 0d;
            int count = 0;

            foreach (var dataSet in dataSets) {
                CacheResults(dataSet);
                
                var valuePairs = dataSet.ExpectedReturned;
                
                for (int i = 0; i < dataSet.Size; i++) {
                    var pair = valuePairs[i];
                    double error = pair.Returned - pair.Expected;
                    
                    sum += error * error;
                }
                
                count += dataSet.Size;
            }

            return 1d - Math.Sqrt(sum / count);
        }
    }
}