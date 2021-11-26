namespace ChartAutoRating {
    public readonly struct DataSample {
        public double Value { get; }
        
        public double Weight { get; }

        public DataSample(double value, double weight) {
            Value = value;
            Weight = weight;
        }
    }
}