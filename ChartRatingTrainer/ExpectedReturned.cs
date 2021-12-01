using System;
using System.Collections.Generic;

namespace ChartRatingTrainer {
    public class ExpectedReturned : IComparable<ExpectedReturned> {
        public int Index { get; set; }
            
        public double Expected { get; set; }
        
        public double ReturnedValue { get; set; }
            
        public double ReturnedPosition { get; set; }

        public int CompareTo(ExpectedReturned other) => ReturnedPosition.CompareTo(other.ReturnedPosition);
    }
}