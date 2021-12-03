using System.IO;

namespace ChartAutoRating {
    public class Matrix {
        public int Size { get; }
        
        public Coefficients[,] ValueCoefficients { get; }
        
        public Coefficients[] WeightCoefficients { get; }

        public Matrix(int size) {
            Size = size;
            ValueCoefficients = new Coefficients[size, size];
            WeightCoefficients = new Coefficients[size];
        }

        public static Matrix Deserialize(BinaryReader reader) {
            int size = reader.ReadInt32();

            var network = new Matrix(size);
            
            for (int i = 0; i < size; i++) {
                for (int j = i; j < size; j++) {
                    double x1 = reader.ReadDouble();
                    double x2 = reader.ReadDouble();
                    double x3 = reader.ReadDouble();
                    double x4 = reader.ReadDouble();
                    double x5 = reader.ReadDouble();

                    network.ValueCoefficients[i, j] = new Coefficients(x1, x2, x3, x4, x5);
                }
            }
            
            for (int i = 0; i < size; i++) {
                double x1 = reader.ReadDouble();
                double x2 = reader.ReadDouble();
                double x3 = reader.ReadDouble();
                double x4 = reader.ReadDouble();
                double x5 = reader.ReadDouble();

                network.WeightCoefficients[i] = new Coefficients(x1, x2, x3, x4, x5);
            }

            return network;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(Size);

            for (int i = 0; i < Size; i++) {
                for (int j = i; j < Size; j++) {
                    var coeff = ValueCoefficients[i, j];

                    writer.Write(coeff.X1);
                    writer.Write(coeff.X2);
                    writer.Write(coeff.X3);
                    writer.Write(coeff.X4);
                    writer.Write(coeff.X5);
                }
            }
            
            for (int i = 0; i < Size; i++) {
                var coeff = WeightCoefficients[i];

                writer.Write(coeff.X1);
                writer.Write(coeff.X2);
                writer.Write(coeff.X3);
                writer.Write(coeff.X4);
                writer.Write(coeff.X5);
            }
        }

        public static void Add(Matrix target, Matrix a, Matrix b) {
            for (int i = 0; i < target.Size; i++) {
                for (int j = i; j < target.Size; j++)
                    target.ValueCoefficients[i, j] = a.ValueCoefficients[i, j] + b.ValueCoefficients[i, j];

                target.WeightCoefficients[i] = a.WeightCoefficients[i] + b.WeightCoefficients[i];
            }
        }

        public static void Multiply(Matrix target, double factor, Matrix source) {
            for (int i = 0; i < target.Size; i++) {
                for (int j = i; j < target.Size; j++)
                    target.ValueCoefficients[i, j] = factor * source.ValueCoefficients[i, j];

                target.WeightCoefficients[i] = factor * source.WeightCoefficients[i];
            }
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

        public double GetValue(double[] values) {
            double sum = 0d;
            
            for (int i = 0; i < Size; i++) {
                for (int j = i; j < Size; j++)
                    sum += Coefficients.Compute(values[i] * values[j], ValueCoefficients[i, j]);
            }

            return sum;
        }

        public double GetWeight(double[] values) {
            double sum = 0d;

            for (int i = 0; i < sum; i++)
                sum += Coefficients.Compute(values[i], WeightCoefficients[i]);

            return sum;
        }
    }
}