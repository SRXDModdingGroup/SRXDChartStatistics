using System;

namespace ChartAutoRating {
    public class CalculatorInfo : IComparable<CalculatorInfo> {
        public Calculator Calculator { get; }
            
        public double Fitness { get; set; }
            
        public double CrossChance { get; set; }
            
        public double KillChance { get; set; }
            
        public bool Keep { get; set; }
            
        public CalculatorInfo Next { get; set; }

        public CalculatorInfo(Calculator calculator) {
            Calculator = calculator;
        }

        public int CompareTo(CalculatorInfo other) => -Fitness.CompareTo(other.Fitness);
    }
}