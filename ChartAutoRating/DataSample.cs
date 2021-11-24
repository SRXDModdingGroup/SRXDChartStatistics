namespace ChartAutoRating {
    public readonly struct DataSample {
        public int DifficultyRating { get; }
            
        public double[] Metrics { get; }

        public DataSample(int difficultyRating, double[] metrics) {
            DifficultyRating = difficultyRating;
            Metrics = metrics;
        }
    }
}