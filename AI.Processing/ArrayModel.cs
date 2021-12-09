using System.IO;

namespace AI.Processing {
    public class ArrayModel {
        public double[] Array { get; }

        protected ArrayModel(double[] array) => Array = array;

        public static ArrayModel Deserialize(BinaryReader reader) {
            int size = reader.ReadInt32();
            double[] array = new double[size];

            for (int i = 0; i < size; i++)
                array[i] = reader.ReadDouble();

            return new ArrayModel(array);
        }
    }
}