using System.Collections.ObjectModel;
using System.IO;
using MatrixAI.Processing;

namespace MatrixAI.Training {
    public class DataWrapper {
        public double ExpectedResult { get; }

        public int SampleSize => data.SampleSize;

        public ReadOnlyCollection<DataSample> Samples => data.Samples;

        private Data data;
        private Matrix[] vectors;
        private Matrix overallVector;

        public DataWrapper(Data data, double expectedResult) {
            this.data = data;
            ExpectedResult = expectedResult;
            vectors = new Matrix[data.Samples.Count];
            overallVector = new Matrix(data.SampleSize);
        }

        public static DataWrapper Create(Data data, double expectedResult) {
            var wrapper = new DataWrapper(data, expectedResult);
            
            for (int i = 0; i < data.Samples.Count; i++) {
                var sample = data.Samples[i];
                var matrix = new Matrix(data.SampleSize);

                Matrix.GetVector(matrix, sample.Values);
                wrapper.vectors[i] = matrix;
            }

            return wrapper;
        }

        public static DataWrapper Deserialize(BinaryReader reader) {
            double expectedResult = reader.ReadDouble();
            var data = Data.Deserialize(reader);
            var wrapper = new DataWrapper(data, expectedResult);
            
            for (int i = 0; i < data.Samples.Count; i++)
                wrapper.vectors[i] = Matrix.Deserialize(reader);

            return wrapper;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(ExpectedResult);
            data.Serialize(writer);

            foreach (var vector in vectors)
                vector.Serialize(writer);
        }

        public double GetResultAndVector(Matrix matrix, out Matrix overallVector) {
            double sumValue = 0d;
            double sumWeight = 0d;
            
            overallVector = this.overallVector;

            for (int i = 0; i < data.SampleSize; i++) {
                for (int j = i; j < data.SampleSize; j++)
                    overallVector.ValueCoefficients[i, j] = new Coefficients(0d, 0d, 0d, 0d, 0d);

                overallVector.WeightCoefficients[i] = new Coefficients(0d, 0d, 0d, 0d, 0d);
            }

            for (int i = 0; i < data.Samples.Count; i++) {
                var sample = data.Samples[i];
                var vector = vectors[i];
                double value = 0d;
                double weight = 0d;

                for (int j = 0; j < data.SampleSize; j++) {
                    double a = sample.Values[j];

                    for (int k = j; k < data.SampleSize; k++)
                        value += Coefficients.Compute(a * sample.Values[k], matrix.ValueCoefficients[j, k]);
                }

                for (int j = 0; j < data.SampleSize; j++) {
                    double a = sample.Values[j];

                    weight += Coefficients.Compute(a * a, matrix.WeightCoefficients[j]);
                }

                weight *= sample.Weight;

                for (int j = 0; j < data.SampleSize; j++) {
                    for (int k = j; k < data.SampleSize; k++)
                        overallVector.ValueCoefficients[j, k] += weight * vector.ValueCoefficients[j, k];

                    overallVector.WeightCoefficients[j] += sample.Weight * value * vector.WeightCoefficients[j];
                }

                sumValue += weight * value;
                sumWeight += weight;
            }

            double scale = 1d / sumWeight;

            for (int i = 0; i < data.SampleSize; i++) {
                for (int j = i; j < data.SampleSize; j++)
                    overallVector.ValueCoefficients[i, j] *= scale;

                overallVector.WeightCoefficients[i] *= scale;
            }

            return scale * sumValue;
        }
    }
}