using MatrixAI.Processing;

namespace MatrixAI.Training {
    public static class MatrixExtensions {
        public static void Zero(Matrix target) {
            for (int i = 0; i < target.Size; i++) {
                for (int j = i; j < target.Size; j++)
                    target.ValueCoefficients[i, j] = Coefficients.Zero;

                target.WeightCoefficients[i] = Coefficients.Zero;
            }
        }
        
        public static void Add(Matrix target, Matrix source) {
            for (int i = 0; i < target.Size; i++) {
                for (int j = i; j < target.Size; j++)
                    target.ValueCoefficients[i, j] += source.ValueCoefficients[i, j];

                target.WeightCoefficients[i] += source.WeightCoefficients[i];
            }
        }
        
        public static void AddWeighted(Matrix target, double weight, Matrix source) {
            for (int i = 0; i < target.Size; i++) {
                for (int j = i; j < target.Size; j++)
                    target.ValueCoefficients[i, j] += weight * source.ValueCoefficients[i, j];

                target.WeightCoefficients[i] += weight * source.WeightCoefficients[i];
            }
        }

        public static void Multiply(double factor, Matrix target) {
            for (int i = 0; i < target.Size; i++) {
                for (int j = i; j < target.Size; j++)
                    target.ValueCoefficients[i, j] *= factor;

                target.WeightCoefficients[i] *= factor;
            }
        }

        public static void Normalize(Matrix target) {
            double sum = 0d;
            
            for (int i = 0; i < target.Size; i++) {
                for (int j = i; j < target.Size; j++)
                    sum += target.ValueCoefficients[i, j].Magnitude;
            }

            double scale = 1d / sum;
            
            for (int i = 0; i < target.Size; i++) {
                for (int j = i; j < target.Size; j++)
                    target.ValueCoefficients[i, j] *= scale;
            }

            sum = 0d;

            for (int i = 0; i < target.Size; i++)
                sum += target.WeightCoefficients[i].Magnitude;

            scale = 1d / sum;

            for (int i = 0; i < target.Size; i++)
                target.WeightCoefficients[i] *= scale;
        }

        public static void GetVector(Matrix target, double[] values) {
            for (int i = 0; i < target.Size; i++) {
                double x1 = values[i];
                double x2 = x1 * x1;
                double x3 = x1 * x2;
                double x4 = x1 * x3;
                double x5 = x1 * x4;

                target.WeightCoefficients[i] = new Coefficients(x1, x2, x3, x4, x5);
                
                for (int j = i; j < target.Size; j++) {
                    x1 = values[i] * values[j];
                    x2 = x1 * x1;
                    x3 = x1 * x2;
                    x4 = x1 * x3;
                    x5 = x1 * x4;

                    target.ValueCoefficients[i, j] = new Coefficients(x1, x2, x3, x4, x5);
                }
            }
        }
    }
}