using System.Drawing;

namespace ChartStatistics {
    public class Label : Drawable {
        private static readonly Brush BRUSH = new SolidBrush(Color.Cyan);
        private static readonly Font FONT = new Font("Consolas", 10);

        private float x;
        private float y;
        private string label;

        public Label(float x, float y, string label) : base(0f, float.PositiveInfinity, DrawLayer.Label) {
            this.x = x;
            this.y = y;
            this.label = label;
        }

        public void SetLabel(string label) => this.label = label;
        
        public override void Draw(GraphicsPanel panel, Graphics graphics) {
            graphics.DrawString(label, FONT, BRUSH, x, y + 10f);
        }
    }
}