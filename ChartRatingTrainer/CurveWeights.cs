using System;

namespace ChartRatingTrainer {
    public readonly struct CurveWeights {
        public double W0 { get; }
            
        public double W1 { get; }
            
        public double W2 { get; }
            
        public double Magnitude { get; }

        public CurveWeights(double w0, double w1, double w2) {
            W0 = w0;
            W1 = w1;
            W2 = w2;
            Magnitude = w0 + w1 + w2;
        }

        public CurveWeights(CurveWeights cw) : this(cw.W0, cw.W1, cw.W2) { }

        public static CurveWeights Random(Random random, double magnitude) {
            double w0 = random.NextDouble();
            double w1 = random.NextDouble();
            double w2 = random.NextDouble();
            double scale = magnitude / (w0 + w1 + w2);

            return new CurveWeights(scale * w0, scale * w1, scale * w2);
        }

        public static CurveWeights Normalize(CurveWeights cw) {
            double magnitude = cw.Magnitude;

            if (magnitude == 0d)
                return cw;
            
            return cw / cw.Magnitude;
        }

        public static CurveWeights operator +(CurveWeights a, CurveWeights b) => new CurveWeights(a.W0 + b.W0, a.W1 + b.W1, a.W2 + b.W2);

        public static CurveWeights operator *(double x, CurveWeights cw) => new CurveWeights(x * cw.W0, x * cw.W1, x * cw.W2);
            

        public static CurveWeights operator /(CurveWeights cw, double x) => new CurveWeights(cw.W0 / x, cw.W1 / x, cw.W2 / x);
    }
}