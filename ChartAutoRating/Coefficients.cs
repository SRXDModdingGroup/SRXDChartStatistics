namespace ChartAutoRating {
    public readonly struct Coefficients {
        public double X1 { get; }
            
        public double X2 { get; }
            
        public double X3 { get; }
            
        public double Magnitude { get; }
            
        public Coefficients(double x1, double x2, double x3) {
            X1 = x1;
            X2 = x2;
            X3 = x3;
            Magnitude = x1 + x2 + x3;
        }

        public static Coefficients Normalize(Coefficients c) => c / c.Magnitude;

        public static Coefficients operator *(double x, Coefficients c) => new Coefficients(x * c.X1, x * c.X2, x * c.X3);

        public static Coefficients operator /(Coefficients c, double x) => new Coefficients(c.X1 / x, c.X2 / x, c.X3 / x);
    }
}