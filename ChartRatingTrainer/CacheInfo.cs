using ChartAutoRating;

namespace ChartRatingTrainer {
    public readonly struct CacheInfo {
        public RelevantChartInfo ChartInfo { get; }
            
        public Data Data { get; }

        public CacheInfo(RelevantChartInfo chartInfo, Data data) {
            ChartInfo = chartInfo;
            Data = data;
        }
    }
}