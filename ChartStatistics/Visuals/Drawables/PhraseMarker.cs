using System.Drawing;
using System.Globalization;

namespace ChartStatistics {
    public class PhraseMarker : Drawable {
        private static readonly Pen PEN = new Pen(Color.Cyan);
        private static readonly Brush BRUSH = new SolidBrush(Color.Cyan);
        private static readonly Font FONT = new Font("Consolas", 10);
        
        private float y;
        private int index;
        private float value;

        public PhraseMarker(double x, float y, int index, float value) : base(x, x, DrawLayer.Label) {
            this.y = y;
            this.index = index;
            this.value = value;
        }
        
        public override void Draw(GraphicsPanel panel, Graphics graphics) {
            float height = panel.ValueToY(0.06f);
            float drawX = panel.TimeToX(Start);
            float drawY = panel.ValueToY(y);
            
            graphics.DrawLine(PEN, drawX, drawY, drawX, drawY + height);
            graphics.DrawString(index.ToString(), FONT, BRUSH, drawX, drawY + 1.1f * height);
            graphics.DrawString(value.ToString("0.00", CultureInfo.InvariantCulture), FONT, BRUSH, drawX, drawY + 1.1f * height + 15);
        }
    }
}