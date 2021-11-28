using System;

namespace ChartRatingTrainer {
    public class Individual : IComparable<Individual> {
        public Calculator Calculator { get; }
            
        public double Fitness { get; set; }
            
        public double CrossChance { get; set; }
            
        public double KillChance { get; set; }
            
        public Individual Next { get; set; }

        public Individual(Calculator calculator) {
            Calculator = calculator;
        }

        public int CompareTo(Individual other) => -Fitness.CompareTo(other.Fitness);
    }
}