using System;
using System.Drawing;
using MatrixAI.Processing;

namespace ChartRatingTrainer {
    public class Individual : IComparable<Individual> {
        public Matrix Matrix { get; }
        
        public Color IdColor { get; set; }
            
        public double Fitness { get; set; }

        public Individual(Matrix matrix, Color idColor) {
            Matrix = matrix;
            IdColor = idColor;
        }

        public int CompareTo(Individual other) => -Fitness.CompareTo(other.Fitness);
    }
}