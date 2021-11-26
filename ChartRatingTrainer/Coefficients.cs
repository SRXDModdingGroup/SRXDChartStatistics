namespace ChartRatingTrainer {
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

        public Coefficients(Coefficients c) : this(c.X1, c.X2, c.X3) { } 

        public Coefficients(double w0, double w1, double w2, double w3) {
            X1 = 2d * w0 + w1;
            X2 = -w0 + w2 + 3d * w3;
            X3 = -2d * w3;
            Magnitude = X1 + X2 + X3;
        }

        public Coefficients(CurveWeights cw) {
            X1 = 3d * cw.W0;
            X2 = 3d * (cw.W2 - cw.W0);
            X3 = cw.W0 + cw.W1 - 2d * cw.W2;
            Magnitude = X1 + X2 + X3;
        }

        public static Coefficients Normalize(Coefficients c) => c / c.Magnitude;

        public static Coefficients operator *(double x, Coefficients c) => new Coefficients(x * c.X1, x * c.X2, x * c.X3);

        public static Coefficients operator /(Coefficients c, double x) => new Coefficients(c.X1 / x, c.X2 / x, c.X3 / x);
    }
}