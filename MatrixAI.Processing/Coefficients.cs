using System;

namespace MatrixAI.Processing {
    public readonly struct Coefficients {
        public static Coefficients Zero { get; } = new Coefficients(0d, 0d, 0d, 0d, 0d);
        
        public double X1 { get; }
            
        public double X2 { get; }
            
        public double X3 { get; }
        
        public double X4 { get; }
            
        public double X5 { get; }
        
        public double Magnitude { get; }
            
        public Coefficients(double x1, double x2, double x3, double x4, double x5) {
            X1 = x1;
            X2 = x2;
            X3 = x3;
            X4 = x4;
            X5 = x5;
            Magnitude = x1 + x2 + x3 + x4 + x5;
        }

        public static Coefficients Random(Random random) => new Coefficients(
            2d * random.NextDouble() - 1d,
            2d * random.NextDouble() - 1d,
            2d * random.NextDouble() - 1d,
            2d * random.NextDouble() - 1d,
            2d * random.NextDouble() - 1d);

        public static double Compute(double x, Coefficients c) => x * (c.X1 + x * (c.X2 + x * (c.X3 + x * (c.X4 + x * c.X5))));

        public static Coefficients operator +(Coefficients a, Coefficients b) => new Coefficients(
            a.X1 + b.X1,
            a.X2 + b.X2,
            a.X3 + b.X3,
            a.X4 + b.X4,
            a.X5 + b.X5);

        public static Coefficients operator *(double x, Coefficients c) => new Coefficients(
            x * c.X1,
            x * c.X2,
            x * c.X3,
            x * c.X4,
            x * c.X5);
        public static Coefficients operator *(Coefficients c, double x) => x * c;

        public static Coefficients operator /(Coefficients c, double x) => 1d / x * c;
    }
}