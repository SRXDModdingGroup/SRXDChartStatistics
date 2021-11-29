using System.Drawing;

namespace ChartRatingTrainer {
    public class DrawInfoItem {
        public double Fitness { get; set; }

        public Color Color { get; set; }
                
        public Curve[,] Curves { get; }

        public DrawInfoItem() => Curves = new Curve[Calculator.METRIC_COUNT, Calculator.METRIC_COUNT + 1];
    }
}