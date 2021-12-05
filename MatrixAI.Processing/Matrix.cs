using System.IO;

namespace MatrixAI.Processing {
    public class Matrix {
        public int SampleSize { get; }
        
        public int TotalSize { get; }
        
        public int Dimensions { get; }
        
        public Coefficients[] ValueCoefficients { get; }
        
        public Coefficients[] WeightCoefficients { get; }

        public Matrix(int sampleSize, int dimensions) {
            SampleSize = sampleSize;
            
            int num = 1;

            for (int i = sampleSize; i < sampleSize + dimensions; i++)
                num *= i;

            int den = 1;

            for (int i = 2; i <= dimensions; i++)
                den *= i;
            
            TotalSize = num / den;
            Dimensions = dimensions;
            ValueCoefficients = new Coefficients[TotalSize];
            WeightCoefficients = new Coefficients[sampleSize];
        }

        private Matrix(int sampleSize, int totalSize, int dimensions) {
            SampleSize = sampleSize;
            TotalSize = totalSize;
            Dimensions = dimensions;
            ValueCoefficients = new Coefficients[totalSize];
            WeightCoefficients = new Coefficients[sampleSize];
        }

        public static Matrix Deserialize(BinaryReader reader) {
            int sampleSize = reader.ReadInt32();
            int totalSize = reader.ReadInt32();
            int dimensions = reader.ReadInt32();
            var matrix = new Matrix(sampleSize, totalSize, dimensions);
            
            for (int i = 0; i < totalSize; i++) {
                double x1 = reader.ReadDouble();
                double x2 = reader.ReadDouble();
                double x3 = reader.ReadDouble();
                double x4 = reader.ReadDouble();
                double x5 = reader.ReadDouble();
                double x6 = reader.ReadDouble();

                matrix.ValueCoefficients[i] = new Coefficients(x1, x2, x3, x4, x5, x6);
            }
            
            for (int i = 0; i < sampleSize; i++) {
                double x1 = reader.ReadDouble();
                double x2 = reader.ReadDouble();
                double x3 = reader.ReadDouble();
                double x4 = reader.ReadDouble();
                double x5 = reader.ReadDouble();
                double x6 = reader.ReadDouble();

                matrix.WeightCoefficients[i] = new Coefficients(x1, x2, x3, x4, x5, x6);
            }

            return matrix;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(SampleSize);
            writer.Write(TotalSize);
            writer.Write(Dimensions);

            foreach (var coeff in ValueCoefficients) {
                writer.Write(coeff.X1);
                writer.Write(coeff.X2);
                writer.Write(coeff.X3);
                writer.Write(coeff.X4);
                writer.Write(coeff.X5);
                writer.Write(coeff.X6);
            }

            foreach (var coeff in WeightCoefficients) {
                writer.Write(coeff.X1);
                writer.Write(coeff.X2);
                writer.Write(coeff.X3);
                writer.Write(coeff.X4);
                writer.Write(coeff.X5);
                writer.Write(coeff.X6);
            }
        }

        public double GetValue(double[] values) {
            double sum = 0d;
            int counter = 0;

            for (int i = 0; i < SampleSize; i++)
                Recurse(values[i], i, 1);

            return sum;

            void Recurse(double product, int start, int depth) {
                if (depth == Dimensions) {
                    sum += Coefficients.Compute(product, ValueCoefficients[counter]);
                    counter++;

                    return;
                }

                for (int i = start; i < SampleSize; i++)
                    Recurse(values[i] * product, i, depth + 1);
            }
        }

        public double GetWeight(double[] values) {
            double sum = 0d;

            for (int i = 0; i < SampleSize; i++)
                sum += Coefficients.Compute(values[i], WeightCoefficients[i]);

            return sum;
        }
    }
}