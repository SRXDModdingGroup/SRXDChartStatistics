namespace ChartAutoRating {
    public readonly struct RelevantChartInfo {
        public string Title { get; }
        
        public int DifficultyRating { get; }

        public RelevantChartInfo(string title, int difficultyRating) {
            Title = title;
            DifficultyRating = difficultyRating;
        }
    }
}