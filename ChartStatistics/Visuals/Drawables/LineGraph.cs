using System.Collections.Generic;
using System.Drawing;
using Util;

namespace ChartStatistics; 

public class LineGraph : Drawable {
    private static readonly Pen PEN = new Pen(Color.FromArgb(127, 255, 255, 255));
        
    private float bottom;
    private float top;
    private IList<PointD> data;

    public LineGraph(double start, double end, float bottom, float top, IList<PointD> data) : base(start, end, DrawLayer.LineGraph) {
        this.bottom = bottom;
        this.top = top;
        this.data = data;
    }
        
    public override void Draw(GraphicsPanel panel, Graphics graphics) {
        int first = 0;
        int last = data.Count - 1;
            
        for (int i = 0; i < data.Count; i++) {
            var point = data[i];

            if (point.X < panel.Scroll)
                first = i;
            else if (point.X > panel.RightBound) {
                last = i;

                break;
            }
        }

        var points = new PointF[last - first + 1];

        for (int i = first, j = 0; i <= last; i++, j++) {
            var point = data[i];

            points[j] = new PointF(panel.TimeToX(point.X), panel.ValueToY(bottom + (top - bottom) * (float) point.Y));
        }
            
        if (points.Length > 1)
            graphics.DrawLines(PEN, points);
    }

    public void SetData(IList<PointD> data) => this.data = data;
}