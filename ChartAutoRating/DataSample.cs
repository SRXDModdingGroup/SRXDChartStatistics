using System.Collections.Generic;

namespace ChartAutoRating {
    internal readonly struct DataSample {
        public double[] Values { get; }
        
        public double Time { get; }
        
        public double Weight { get; }

        public DataSample(double[] values, double time, double weight) {
            Values = values;
            Time = time;
            Weight = weight;
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