using System;
using ChartAutoRating;

namespace ChartRatingTrainer {
    public readonly struct Curve {
        public double W0 { get; }
            
        public double W1 { get; }
            
        public double W2 { get; }
            
        public double Magnitude { get; }

        public Curve(double w0, double w1, double w2) {
            W0 = w0;
            W1 = w1;
            W2 = w2;
            Magnitude = w0 + w1 + w2;
        }

        public Curve(Curve cw) : this(cw.W0, cw.W1, cw.W2) { }

        public Coefficients ToCoefficients() => new Coefficients(3d * W0, 3d * (W2 - W0), W0 + W1 - 2d * W2);

        public static Curve Random(Random random, double magnitude) {
            double w0 = random.NextDouble();
            double w1 = random.NextDouble();
            double w2 = random.NextDouble();
            double scale = magnitude / (w0 + w1 + w2);

            return new Curve(scale * w0, scale * w1, scale * w2);
        }

        public static Curve Normalize(Curve cw) {
            double magnitude = cw.Magnitude;

            if (magnitude == 0d)
                return cw;
            
            return cw / cw.Magnitude;
        }

        public static Curve operator +(Curve a, Curve b) => new Curve(a.W0 + b.W0, a.W1 + b.W1, a.W2 + b.W2);

        public static Curve operator *(double x, Curve cw) => new Curve(x * cw.W0, x * cw.W1, x * cw.W2);
            

        public static Curve operator /(Curve cw, double x) => new Curve(cw.W0 / x, cw.W1 / x, cw.W2 / x);
    }
}