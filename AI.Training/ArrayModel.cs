using System;
using System.IO;
using AI.Processing;

namespace AI.Training {
    public class ArrayModel : Processing.ArrayModel, IModel<ArrayModel> {
        public ArrayModel(double[] array) : base(array) { }
        
        public new static ArrayModel Deserialize(BinaryReader reader) {
            int size = reader.ReadInt32();
            double[] array = new double[size];

            for (int i = 0; i < size; i++)
                array[i] = reader.ReadDouble();

            return new ArrayModel(array);
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(Array.Length);

            foreach (double value in Array)
                writer.Write(value);
        }

        public void Zero() {
            for (int i = 0; i < Array.Length; i++)
                Array[i] = 0d;
        }

        public void Normalize(double magnitude) {
            double sum = 0d;

            for (int i = 0; i < Array.Length; i++)
                sum += Math.Abs(Array[i]);

            double scale = magnitude / sum;

            for (int i = 0; i < Array.Length; i++)
                Array[i] *= scale;
        }
        
        public void Add(ArrayModel source) {
            for (int i = 0; i < Array.Length; i++)
                Array[i] += source.Array[i];
        }

        public void AddWeighted(double weight, ArrayModel source) {
            for (int i = 0; i < Array.Length; i++)
                Array[i] += weight * source.Array[i];
        }
    }
}