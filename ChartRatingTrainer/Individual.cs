using System;
using MatrixAI.Processing;

namespace ChartRatingTrainer {
    public class Individual : IComparable<Individual> {
        public Matrix Matrix { get; }
            
        public double Fitness { get; set; }

        public Individual(Matrix matrix) {
            Matrix = matrix;
        }

        public int CompareTo(Individual other) => -Fitness.CompareTo(other.Fitness);
    }
}