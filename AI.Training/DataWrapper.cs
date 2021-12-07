﻿using System.Collections.ObjectModel;
using System.IO;
using AI.Processing;

namespace AI.Training {
    public class DataWrapper {
        public string Name => data.Name;

        public double ExpectedResult { get; }

        internal int Size => data.Size;

        internal ReadOnlyCollection<DataSample> Samples => data.Samples;

        private Data data;
        private Matrix sampleValueVector;
        private Matrix sampleWeightVector;
        private Matrix overallValueVector;
        private Matrix overallWeightVector;

        internal DataWrapper(Data data, double expectedResult, int matrixDimensions) {
            this.data = data;
            ExpectedResult = expectedResult;
            sampleValueVector = new Matrix(data.SampleSize, matrixDimensions);
            sampleWeightVector = new Matrix(data.SampleSize, matrixDimensions);
            overallValueVector = new Matrix(data.SampleSize, matrixDimensions);
            overallWeightVector = new Matrix(data.SampleSize, matrixDimensions);
        }

        internal static DataWrapper Deserialize(BinaryReader reader, int matrixDimensions) {
            var data = Data.Deserialize(reader);
            double expectedResult = reader.ReadDouble();

            return new DataWrapper(data, expectedResult, matrixDimensions);
        }

        internal void Serialize(BinaryWriter writer) {
            data.Serialize(writer);
            writer.Write(ExpectedResult);
        }

        internal void Clamp(int valueIndex, double max) => data.Clamp(valueIndex, max);

        internal void Normalize(double[] scales, double[] powers) => data.Normalize(scales, powers);

        internal double GetMaxValue(int valueIndex) {
            double max = 0d;

            foreach (var sample in data.Samples) {
                double value = sample.Values[valueIndex];

                if (value > max)
                    max = value;
            }

            return max;
        }

        internal double GetResult(Matrix valueMatrix, Matrix weightMatrix) => data.GetResult(valueMatrix, weightMatrix);

        internal double GetQuantile(int valueIndex, double quantile) => data.GetQuantile(valueIndex, quantile);

        internal double GetResultAndVectors(Matrix valueMatrix, Matrix weightMatrix, out double weightScale, out Matrix valueVector, out Matrix weightVector) {
            double sumValue = 0d;
            double sumWeight = 0d;
            
            valueVector = overallValueVector;
            weightVector = overallWeightVector;
            valueVector.Zero();
            weightVector.Zero();

            for (int i = 0; i < data.Samples.Count; i++) {
                var sample = data.Samples[i];
                double value = valueMatrix.GetValueAndVector(sampleValueVector, sample.Values);
                double weight = sample.Weight * weightMatrix.GetValueAndVector(sampleWeightVector, sample.Values);

                valueVector.AddWeighted(weight, sampleValueVector);
                weightVector.AddWeighted(sample.Weight * value, sampleWeightVector);
                sumValue += weight * value;
                sumWeight += weight;
            }
            
            weightScale = 1d / sumWeight;

            return weightScale * sumValue;
        }
    }
}