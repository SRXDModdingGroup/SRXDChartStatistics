using System;

namespace ChartRatingTrainer {
    public class ExpectedReturned : IComparable<ExpectedReturned> {
        public string Name { get; }
            
        public double Expected { get; }
        
        public double Returned { get; }

        public ExpectedReturned(string name, double expected, double returned) {
            Name = name;
            Expected = expected;
            Returned = returned;
        }

        public int CompareTo(ExpectedReturned other) => Returned.CompareTo(other.Returned);
    }
}