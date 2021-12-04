using System.IO;

namespace MatrixAI.Processing {
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