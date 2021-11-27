using System;

namespace ChartAutoRating {
    internal readonly struct DataSample : IComparable<DataSample> {
        public double Value { get; }
        
        public double Weight { get; }

        public DataSample(double value, double weight) {
            Value = value;
            Weight = weight;
        }

        public int CompareTo(DataSample other) => Value.CompareTo(other.Value);
    }
}