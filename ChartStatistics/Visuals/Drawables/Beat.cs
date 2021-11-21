using System.Drawing;

namespace ChartStatistics {
    public class Beat : Drawable {
        private static readonly Brush BRUSH = new SolidBrush(Color.LightGreen);
        
        private float bottom;
        private float top;
        
        public Beat(float x, float bottom, float top) : base(x, x, DrawLayer.Beat) {
            this.bottom = bottom;
            this.top = top;
        }

        public override void Draw(GraphicsPanel panel, Graphics graphics) {
            float x = panel.TimeToX(Start);
            float y = panel.ValueToY(bottom);
            
            graphics.FillRectangle(BRUSH, x, y, 1f, panel.ValueToY(top) - y);
        }
    }
}