namespace ChartAutoRating {
    public partial class Calculator {
        public readonly struct Coefficients {
            public float X1 { get; }
            
            public float X2 { get; }
            
            public float X3 { get; }
            
            public float Magnitude { get; }
            
            public Coefficients(float x1, float x2, float x3) {
                X1 = x1;
                X2 = x2;
                X3 = x3;
                Magnitude = x1 + x2 + x3;
            }

            public Coefficients(Coefficients c) : this(c.X1, c.X2, c.X3) { } 

            public Coefficients(float w0, float w1, float w2, float w3) {
                X1 = 2f * w0 + w1;
                X2 = -w0 + w2 + 3f * w3;
                X3 = -2f * w3;
                Magnitude = X1 + X2 + X3;
            }

            public Coefficients(CurveWeights cw) : this(cw.W0, cw.W1, cw.W2, cw.W3) { }

            public static Coefficients Normalize(Coefficients c) => c / c.Magnitude;

            public static Coefficients operator *(float x, Coefficients c) => new Coefficients(x * c.X1, x * c.X2, x * c.X3);

            public static Coefficients operator /(Coefficients c, float x) => new Coefficients(c.X1 / x, c.X2 / x, c.X3 / x);
        }
    }
}