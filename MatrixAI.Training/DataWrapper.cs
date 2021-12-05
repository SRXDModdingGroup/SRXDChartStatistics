using System.Collections.ObjectModel;
using System.IO;
using MatrixAI.Processing;

namespace MatrixAI.Training {
    public class DataWrapper {
        public string Name => data.Name;
        
        public double ExpectedResult { get; }

        public int SampleSize => data.SampleSize;

        public ReadOnlyCollection<DataSample> Samples => data.Samples;

        private Data data;
        private Matrix[] vectors;
        private Matrix overallVector;
        private int matrixDimensions;

        private DataWrapper(Data data, double expectedResult, int matrixDimensions) {
            this.data = data;
            ExpectedResult = expectedResult;
            vectors = new Matrix[data.Samples.Count];
            overallVector = new Matrix(data.SampleSize, matrixDimensions);
            this.matrixDimensions = matrixDimensions;
        }

        public static DataWrapper Create(Data data, double expectedResult, int matrixDimensions) {
            var wrapper = new DataWrapper(data, expectedResult, matrixDimensions);
            
            for (int i = 0; i < data.Samples.Count; i++) {
                var sample = data.Samples[i];
                var matrix = new Matrix(data.SampleSize, matrixDimensions);

                MatrixExtensions.GetVector(matrix, sample.Values);
                wrapper.vectors[i] = matrix;
            }

            return wrapper;
        }

        public static DataWrapper Deserialize(BinaryReader reader) {
            var data = Data.Deserialize(reader);
            double expectedResult = reader.ReadDouble();
            int matrixDimensions = reader.ReadInt32();
            var wrapper = new DataWrapper(data, expectedResult, matrixDimensions);
            
            for (int i = 0; i < data.Samples.Count; i++)
                wrapper.vectors[i] = Matrix.Deserialize(reader);

            return wrapper;
        }

        public void Serialize(BinaryWriter writer) {
            data.Serialize(writer);
            writer.Write(ExpectedResult);
            writer.Write(matrixDimensions);

            foreach (var vector in vectors)
                vector.Serialize(writer);
        }

        public void Clamp(int valueIndex, double max) => data.Clamp(valueIndex, max);

        public double GetQuantile(int valueIndex, double quantile) => data.GetQuantile(valueIndex, quantile);

        public double GetResult(Matrix matrix) => data.GetResult(matrix);

        public double GetResultAndVector(Matrix matrix, out Matrix vector) {
            double sumValue = 0d;
            double sumWeight = 0d;
            
            vector = overallVector;
            
            MatrixExtensions.Zero(vector);

            for (int i = 0; i < data.Samples.Count; i++) {
                var sample = data.Samples[i];
                var sampleVector = vectors[i];
                double value = matrix.GetValue(sample.Values);
                double weight = sample.Weight * matrix.GetWeight(sample.Values);

                for (int j = 0; j < vector.TotalSize; j++)
                    vector.ValueCoefficients[j] += weight * sampleVector.ValueCoefficients[j];

                for (int j = 0; j < vector.SampleSize; j++)
                    vector.WeightCoefficients[j] += sample.Weight * value * sampleVector.WeightCoefficients[j];

                sumValue += weight * value;
                sumWeight += weight;
            }

            double scale = 1d / sumWeight;

            for (int i = 0; i < vector.TotalSize; i++)
                vector.ValueCoefficients[i] *= scale;

            for (int i = 0; i < vector.SampleSize; i++)
                vector.WeightCoefficients[i] *= scale;

            return scale * sumValue;
        }
    }
}