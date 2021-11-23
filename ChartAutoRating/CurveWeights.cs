namespace ChartAutoRating {
    public readonly struct CurveWeights {
        public float W0 { get; }
            
        public float W1 { get; }
            
        public float W2 { get; }
            
        public float W3 { get; }
            
        public float Magnitude { get; }

        public CurveWeights(float w0, float w1, float w2, float w3) {
            W0 = w0;
            W1 = w1;
            W2 = w2;
            W3 = w3;
            Magnitude = w0 + w1 + w2 + w3;
        }

        public CurveWeights(CurveWeights cw) : this(cw.W0, cw.W1, cw.W2, cw.W3) { }

        public static CurveWeights Normalize(CurveWeights cw) => cw / cw.Magnitude;

        public static CurveWeights operator *(float x, CurveWeights cw) => new CurveWeights(x * cw.W0, x * cw.W1, x * cw.W2, x * cw.W3);
            

        public static CurveWeights operator /(CurveWeights cw, float x) => new CurveWeights(cw.W0 / x, cw.W1 / x, cw.W2 / x, cw.W3 / x);
    }
}