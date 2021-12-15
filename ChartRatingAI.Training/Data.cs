using System.IO;
using ChartRatingAI.Processing;

namespace ChartRatingAI.Training {
    public class Data : Processing.Data {
        public double[] CachedValues { get; }
        public double[] CachedWeights { get; }
        
        public double CachedSumWeight { get; set; }

        public Data(string name, int sampleSize, DataSample[] samples) : base(name, sampleSize, samples) {
            CachedValues = new double[Size];
            CachedWeights = new double[Size];
        }

        private Data(string name, int size, int sampleSize) : base(name, size, sampleSize) {
            CachedValues = new double[size];
            CachedWeights = new double[size];
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