using ChartAutoRating;

namespace ChartRatingTrainer {
    public class DrawInfoItem {
        public double Fitness { get; set; }
                
        public CurveWeights[] CurveWeights { get; }

        public DrawInfoItem() => CurveWeights = new CurveWeights[Calculator.METRIC_COUNT];
    }
}