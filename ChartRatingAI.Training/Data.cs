using System;
using System.Collections.Generic;
using System.IO;
using ChartRatingAI.Processing;

namespace ChartRatingAI.Training {
    public class Data : Processing.Data {
        private double[] cachedValues;
        private double[] cachedWeights;

        public Data(string name, int sampleSize, DataSample[] samples) : base(name, sampleSize, samples) { }

        private Data(string name, int size, int sampleSize) : base(name, size, sampleSize) {
            cachedValues = new double[size];
            cachedWeights = new double[size];
        }
        
        public new static Data Deserialize(BinaryReader reader) =>
            Deserialize(reader, (name, size, sampleSize) => new Data(name, size, sampleSize));
        
        public void Serialize(BinaryWriter writer) {
            writer.Write(Name);
            writer.Write(Size);
            writer.Write(SampleSize);

            foreach (var sample in Samples) {
                double[] values = sample.Values;

                for (int j = 0; j < SampleSize; j++)
                    writer.Write(values[j]);
                
                writer.Write(sample.Weight);
            }
        }
    }
}