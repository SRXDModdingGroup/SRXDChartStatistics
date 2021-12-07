using System.Collections.ObjectModel;
using System.IO;
using AI.Processing;

namespace AI.Training {
    public class DataWrapper {
        public string Name => data.Name;

        public int Size => data.Size;

        public double ExpectedResult { get; }

        public double Weight { get; set; }

        public int SampleSize => data.SampleSize;

        public ReadOnlyCollection<DataSample> Samples { get; }

        private Data data;
        private Matrix sampleValueVector;
        private Matrix sampleWeightVector;
        private Matrix overallValueVector;
        private Matrix overallWeightVector;

        public DataWrapper(Data data, double expectedResult, int matrixDimensions) {
            this.data = data;
            ExpectedResult = expectedResult;
            Samples = new ReadOnlyCollection<DataSample>(data.Samples);
            sampleValueVector = new Matrix(data.SampleSize, matrixDimensions);
            sampleWeightVector = new Matrix(data.SampleSize, matrixDimensions);
            overallValueVector = new Matrix(data.SampleSize, matrixDimensions);
            overallWeightVector = new Matrix(data.SampleSize, matrixDimensions);
        }

        public static DataWrapper Deserialize(BinaryReader reader, int matrixDimensions) {
            var data = Data.Deserialize(reader);
            double expectedResult = reader.ReadDouble();

            return new DataWrapper(data, expectedResult, matrixDimensions);
        }

        public void Serialize(BinaryWriter writer) {
            data.Serialize(writer);
            writer.Write(ExpectedResult);
        }

        public void Clamp(int valueIndex, double max) => data.Clamp(valueIndex, max);

        public void Normalize(double[] scales, double[] powers) => data.Normalize(scales, powers);

        public void GetVectors(Matrix valueMatrix, Matrix weightMatrix, double weightScale, out Matrix valueVector, out Matrix weightVector) {
            valueVector = overallValueVector;
            weightVector = overallWeightVector;
            MatrixExtensions.Zero(valueVector);
            MatrixExtensions.Zero(weightVector);

            for (int i = 0; i < data.Samples.Count; i++) {
                var sample = data.Samples[i];
                double value = valueMatrix.GetValueAndVector(sampleValueVector, sample.Values);
                double weight = sample.Weight * weightMatrix.GetValueAndVector(sampleWeightVector, sample.Values);

                MatrixExtensions.AddWeighted(valueVector, weightScale * weight, sampleValueVector);
                MatrixExtensions.AddWeighted(weightVector, weightScale * sample.Weight * value, sampleWeightVector);
            }
        }

        public double GetMaxValue(int valueIndex) {
            double max = 0d;

            foreach (var sample in data.Samples) {
                double value = sample.Values[valueIndex];

                if (value > max)
                    max = value;
            }

            return max;
        }

        public double GetResult(Matrix valueMatrix, Matrix weightMatrix, out double weightScale) => data.GetResult(valueMatrix, weightMatrix, out weightScale);

        public double GetQuantile(int valueIndex, double quantile) => data.GetQuantile(valueIndex, quantile);
    }
}