using System.IO;

namespace MatrixAI.Processing {
    public class Matrix {
        public int SampleSize { get; }
        
        public int TotalSize { get; }

        public int Dimensions { get; }
        
        public double[] Coefficients { get; }

        public Matrix(int sampleSize, int dimensions) {
            SampleSize = sampleSize;
            
            int num = 1;

            for (int i = sampleSize + 1; i < sampleSize + dimensions + 1; i++)
                num *= i;

            int den = 1;

            for (int i = 2; i <= dimensions; i++)
                den *= i;
            
            TotalSize = num / den;
            Dimensions = dimensions;
            Coefficients = new double[TotalSize];
        }

        private Matrix(int sampleSize, int totalSize, int dimensions) {
            SampleSize = sampleSize;
            TotalSize = totalSize;
            Dimensions = dimensions;
            Coefficients = new double[totalSize];
        }

        public static Matrix Deserialize(BinaryReader reader) {
            int sampleSize = reader.ReadInt32();
            int totalSize = reader.ReadInt32();
            int dimensions = reader.ReadInt32();
            var matrix = new Matrix(sampleSize, totalSize, dimensions);
            
            for (int i = 0; i < totalSize; i++)
                matrix.Coefficients[i] = reader.ReadDouble();

            return matrix;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(SampleSize);
            writer.Write(TotalSize);
            writer.Write(Dimensions);

            foreach (double coeff in Coefficients)
                writer.Write(coeff);
        }

        public double GetValue(double[] values) {
            double sum = 0d;
            int counter = 0;

            for (int i = 0; i < SampleSize; i++)
                Recurse(values[i], i, 1);

            sum += Coefficients[counter];

            return sum;

            void Recurse(double product, int start, int depth) {
                if (depth < Dimensions) {
                    for (int i = start; i < SampleSize; i++)
                        Recurse(values[i] * product, i, depth + 1);
                }
                
                sum += Coefficients[counter] * product;
                counter++;
            }
        }
    }
}