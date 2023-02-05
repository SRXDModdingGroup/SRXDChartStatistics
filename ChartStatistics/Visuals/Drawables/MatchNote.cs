using System.Drawing;

namespace ChartStatistics {
    public class MatchNote : Drawable {
        private static readonly Brush BRUSH_BLUE = new SolidBrush(Color.Cyan);
        private static readonly Brush BRUSH_RED = new SolidBrush(Color.Red);

        private Brush brush;
        private float y;
        
        public MatchNote(double x, float y, bool isRed) : base(x, x, DrawLayer.Match) {
            if (isRed)
                brush = BRUSH_RED;
            else
                brush = BRUSH_BLUE;

            this.y = y;
        }

        public override void Draw(GraphicsPanel panel, Graphics graphics) {
            float height = panel.ValueToY(0.04f);
            
            graphics.FillRectangle(brush, panel.TimeToX(Start) - 1, panel.ValueToY(y) - 0.5f * height, 2, height);
        }
    }
}