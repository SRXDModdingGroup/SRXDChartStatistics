using System;

namespace AI.Processing {
    public class ArrayModel : IModel<ArrayModel> {
        public double[] Array { get; }

        public ArrayModel(double[] array) => Array = array;

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