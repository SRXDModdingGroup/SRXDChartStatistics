using System.Collections.Generic;

namespace ChartAutoRating {
    internal class DataSample {
        public double[] Values { get; }

        public double Weight { get; }

        public Matrix Vector { get; }

        public DataSample(double[] values, double weight) {
            Values = values;
            Weight = weight;
            Vector = new Matrix(Values.Length);
            Matrix.GetVector(Vector, Values);
        }

        public class Comparer : IComparer<DataSample> {
            private int metricIndex;

            public Comparer(int metricIndex) {
                this.metricIndex = metricIndex;
            }

            public int Compare(DataSample x, DataSample y) => x.Values[metricIndex].CompareTo(y.Values[metricIndex]);
        }
    }
}