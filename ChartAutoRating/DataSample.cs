using System;
using System.Collections.Generic;

namespace ChartAutoRating {
    internal readonly struct DataSample {
        public double[] Values { get; }
        
        public double Weight { get; }

        public DataSample(double[] values, double weight) {
            Values = values;
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