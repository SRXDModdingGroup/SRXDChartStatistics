using System;
using System.Collections.Generic;
using System.IO;
using ChartRatingAI.Processing;

namespace ChartRatingAI.Training {
    public class Data : Processing.Data {
        private double[] cachedValues;
        private double[] cachedWeights;

        private Data(string name, int size, int sampleSize) : base(name, size, sampleSize) {
            cachedValues = new double[size];
            cachedWeights = new double[size];
        }
        
        public new static Data Create(string name, int sampleSize, Func<int, IEnumerable<(double, double)>> selector) =>
            Create(name, sampleSize, selector, (n, ss, s) => new Data(n, s, ss));
        
        public static Data Deserialize(BinaryReader reader) {
            string name = reader.ReadString();
            int size = reader.ReadInt32();
            int sampleSize = reader.ReadInt32();
            var data = new Data(name, size, sampleSize);

            for (int i = 0; i < size; i++) {
                double[] newValues = new double[sampleSize];

                for (int j = 0; j < sampleSize; j++)
                    newValues[j] = reader.ReadDouble();

                double weight = reader.ReadDouble();
                
                data.SamplesArray[i] = new DataSample(newValues, weight);
            }

            return data;
        }
        
        public void Serialize(BinaryWriter writer) {
            writer.Write(Name);
            writer.Write(Size);
            writer.Write(SampleSize);

            foreach (var sample in SamplesArray) {
                double[] values = sample.Values;

                for (int j = 0; j < SampleSize; j++)
                    writer.Write(values[j]);
                
                writer.Write(sample.Weight);
            }
        }
    }
}