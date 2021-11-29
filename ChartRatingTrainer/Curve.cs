using System;
using ChartAutoRating;

namespace ChartRatingTrainer {
    public readonly struct Curve {
        public double A { get; }
            
        public double B { get; }
            
        public double C { get; }
        
        public double D { get; }
            
        public double E { get; }
            
        public double F { get; }
            
        public double Magnitude { get; }

        public Curve(double a, double b, double c, double d, double e, double f) {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
            Magnitude = a + b + c + d + e + f;
        }

        public Coefficients ToCoefficients() => new Coefficients(
            3d * A + 5d * B,
            3d * (C - A) - 10d * B,
            A + 10d * (B + D) - 2d * C + E,
            -5d * B - 15d * D,
            B + 6d * D + F);

        public static Curve Random(Random random, double magnitude) {
            double w0 = random.NextDouble();
            double w1 = random.NextDouble();
            double w2 = random.NextDouble();
            double w3 = random.NextDouble();
            double w4 = random.NextDouble();
            double w5 = random.NextDouble();
            double scale = magnitude / (w0 + w1 + w2);

            return new Curve(
                scale * w0,
                scale * w1,
                scale * w2,
                scale * w3,
                scale * w4,
                scale * w5);
        }

        public static Curve Normalize(Curve cw) {
            double magnitude = cw.Magnitude;

            if (magnitude == 0d)
                return cw;
            
            return cw / cw.Magnitude;
        }

        public static Curve Clamp(Curve cw) => new Curve(
            Math.Max(0d, cw.A),
            Math.Max(0d, cw.B),
            Math.Max(0d, cw.C),
            Math.Max(0d, cw.D),
            Math.Max(0d, cw.E),
            Math.Max(0d, cw.F));

        public static Curve operator +(Curve a, Curve b) => new Curve(
            a.A + b.A,
            a.B + b.B,
            a.C + b.C,
            a.D + b.D,
            a.E + b.E,
            a.F + b.F);

        public static Curve operator *(double x, Curve cw) => new Curve(
            x * cw.A,
            x * cw.B,
            x * cw.C,
            x * cw.D,
            x * cw.E,
            x * cw.F);
        
        public static Curve operator /(Curve cw, double x) => 1d / x * cw;
    }
}