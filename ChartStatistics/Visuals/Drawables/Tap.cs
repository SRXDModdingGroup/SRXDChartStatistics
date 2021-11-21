using System.Drawing;

namespace ChartStatistics {
    public class Tap : Drawable {
        private static readonly Brush BRUSH_BLUE = new SolidBrush(Color.Cyan);
        private static readonly Brush BRUSH_RED = new SolidBrush(Color.Red);
        
        private Brush brush;
        private float y;
        
        public Tap(float start, float y, bool isRed) : base(start, start, DrawLayer.Tap) {
            if (isRed)
                brush = BRUSH_RED;
            else
                brush = BRUSH_BLUE;

            this.y = y;
        }

        public override void Draw(GraphicsPanel panel, Graphics graphics) {
            float height = panel.ValueToY(0.08f);
            
            graphics.FillRectangle(brush, panel.TimeToX(Start) - 1, panel.ValueToY(y) - 0.5f * height, 2, height);
        }
    }
}