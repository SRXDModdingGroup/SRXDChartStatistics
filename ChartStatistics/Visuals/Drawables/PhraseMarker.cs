using System.Drawing;
using System.Globalization;

namespace ChartStatistics {
    public class PhraseMarker : Drawable {
        private static readonly Pen PEN = new Pen(Color.Cyan);
        private static readonly Brush BRUSH = new SolidBrush(Color.Cyan);
        private static readonly Font FONT = new Font("Consolas", 10);
        private static readonly float HEIGHT = 20f;
        
        private float y;
        private int index;
        private float value;

        public PhraseMarker(float x, float y, int index, float value) : base(x, x, DrawLayer.Label) {
            this.y = y;
            this.index = index;
            this.value = value;
        }
        
        public override void Draw(GraphicsPanel panel, Graphics graphics) {
            float drawX = panel.TimeToX(Start);
            float drawY = panel.ValueToY(y);
            
            graphics.DrawLine(PEN, drawX, drawY, drawX, drawY + HEIGHT);
            graphics.DrawString(index.ToString(), FONT, BRUSH, drawX, drawY + HEIGHT);
            graphics.DrawString(value.ToString("0.00", CultureInfo.InvariantCulture), FONT, BRUSH, drawX, drawY + 2f * HEIGHT);
        }
    }
}