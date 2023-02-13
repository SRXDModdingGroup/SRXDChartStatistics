using System.Drawing;

namespace ChartStatistics; 

public class ValueLabel : Drawable {
    private static readonly Pen PEN = new Pen(Color.Cyan);
    private static readonly Brush BRUSH = new SolidBrush(Color.Cyan);
    private static readonly Font FONT = new Font("Consolas", 10);
        
    private float y;
    private string label;
        
    public ValueLabel(float y, string label) : base(0d, double.PositiveInfinity, DrawLayer.Label) {
        this.y = y;
        this.label = label;
    }
        
    public override void Draw(GraphicsPanel panel, Graphics graphics) {
        float panelY = panel.ValueToY(y);
        float rightBound = panel.TimeToX(panel.RightBound);
            
        graphics.DrawString(label, FONT, BRUSH, 0f, panelY - 18f);
        graphics.DrawLine(PEN, 0f, panelY, rightBound, panelY);
    }
}