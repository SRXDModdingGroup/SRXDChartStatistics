namespace ChartAutoRating {
    public readonly struct DataSample {
        public int DifficultyRating { get; }
            
        public float[] Metrics { get; }

        public DataSample(int difficultyRating, float[] metrics) {
            DifficultyRating = difficultyRating;
            Metrics = metrics;
        }
    }
}