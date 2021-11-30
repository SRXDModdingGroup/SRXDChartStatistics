namespace ChartAutoRating {
    public readonly struct Coefficients {
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

        public static double Compute(double x, Coefficients c) => x * (c.X1 + x * (c.X2 + x * (c.X3 + x * (c.X4 + x * c.X5))));

        public static Coefficients operator *(double x, Coefficients c) => new Coefficients(
            x * c.X1,
            x * c.X2,
            x * c.X3,
            x * c.X4,
            x * c.X5);

        public static Coefficients operator /(Coefficients c, double x) => 1d / x * c;
    }
}