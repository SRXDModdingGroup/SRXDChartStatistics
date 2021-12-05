using System;

namespace ChartRatingTrainer {
    public class ExpectedReturned : IComparable<ExpectedReturned> {
        public int Index { get; set; }
            
        public double Expected { get; set; }
        
        public double Returned { get; set; }

        public ExpectedReturned(int index, double expected, double returned) {
            Index = index;
            Expected = expected;
            Returned = returned;
        }

        public int CompareTo(ExpectedReturned other) => Returned.CompareTo(other.Returned);
    }
}