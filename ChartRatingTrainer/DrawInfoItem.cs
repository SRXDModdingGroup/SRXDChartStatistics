using System.Drawing;

namespace ChartRatingTrainer {
    public class DrawInfoItem {
        public double Fitness { get; set; }

        public Color Color { get; set; }
                
        public Curve[,] ValueCurves { get; }
        
        public Curve[] WeightCurves { get; }

        public DrawInfoItem() {
            ValueCurves = new Curve[Calculator.METRIC_COUNT, Calculator.METRIC_COUNT];
            WeightCurves = new Curve[Calculator.METRIC_COUNT];
        }
    }
}