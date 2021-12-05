using System;
using MatrixAI.Processing;

namespace MatrixAI.Training {
    public static class MatrixExtensions {
        public static void Zero(Matrix target) {
            for (int i = 0; i < target.TotalSize; i++)
                target.ValueCoefficients[i] = Coefficients.Zero;

            for (int i = 0; i < target.SampleSize; i++)
                target.WeightCoefficients[i] = Coefficients.Zero;
        }
        
        public static void AddWeighted(Matrix target, double weight, Matrix source) {
            for (int i = 0; i < target.TotalSize; i++)
                target.ValueCoefficients[i] += weight * source.ValueCoefficients[i];

            for (int i = 0; i < target.SampleSize; i++)
                target.WeightCoefficients[i] += weight * source.WeightCoefficients[i];
        }

        public static void Normalize(Matrix target) {
            double sum = 0d;
            
            for (int i = 0; i < target.TotalSize; i++)
                sum += target.ValueCoefficients[i].Magnitude;

            double scale = 1d / sum;
            
            for (int i = 0; i < target.TotalSize; i++)
                target.ValueCoefficients[i] *= scale;

            sum = 0d;

            for (int i = 0; i < target.SampleSize; i++)
                sum += target.WeightCoefficients[i].Magnitude;

            scale = 1d / sum;

            for (int i = 0; i < target.SampleSize; i++)
                target.WeightCoefficients[i] *= scale;
        }

        public static void GetVector(Matrix target, double[] values) {
            int counter = 0;

            for (int i = 0; i < target.SampleSize; i++)
                Recurse(values[i], i, 1);

            for (int i = 0; i < target.SampleSize; i++) {
                double x1 = values[i];
                double x2 = x1 * x1;
                double x3 = x1 * x2;
                double x4 = x1 * x3;
                double x5 = x1 * x4;
                double x6 = x1 * x5;

                target.WeightCoefficients[i] = new Coefficients(x1, x2, x3, x4, x5, x6);
            }

            void Recurse(double product, int start, int depth) {
                if (depth == target.Dimensions) {
                    double x2 = product * product;
                    double x3 = product * x2;
                    double x4 = product * x3;
                    double x5 = product * x4;
                    double x6 = product * x5;
                    
                    target.ValueCoefficients[counter] = new Coefficients(product, x2, x3, x4, x5, x6);
                    counter++;

                    return;
                }
                
                for (int i = start; i < target.SampleSize; i++)
                    Recurse(values[i] * product, i, depth + 1);
            }
        }

        public static Matrix Random(int sampleSize, int dimensions, Random random) {
            var matrix = new Matrix(sampleSize, dimensions);

            for (int i = 0; i < matrix.TotalSize; i++)
                matrix.ValueCoefficients[i] = Coefficients.Random(random);

            for (int i = 0; i < sampleSize; i++)
                matrix.WeightCoefficients[i] = Coefficients.Random(random);

            Normalize(matrix);
            
            return matrix;
        }
    }
}