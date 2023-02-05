using System;
using System.Collections.Generic;
using System.Drawing;
using Util;

namespace ChartStatistics {
    public class BarGraph : Drawable {
        private static readonly Brush BRUSH = new SolidBrush(Color.DarkGray);
        private static readonly int GAP = 1;
        
        private float bottom;
        private float top;
        private IList<PointD> data;

        public BarGraph(double start, double end, float bottom, float top, IList<PointD> data) : base(start, end, DrawLayer.LineGraph) {
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

            var rects = new RectangleF[last - first + 1];

            for (int i = first, j = 0; i <= last; i++, j++) {
                var point = data[i];

                float startX = panel.TimeToX(point.X);
                float endX;

                if (i == last)
                    endX = panel.TimeToX(End);
                else
                    endX = panel.TimeToX(data[i + 1].X);

                float bottomY = panel.ValueToY(bottom);
                float topY = panel.ValueToY(bottom + (top - bottom) * (float) point.Y);

                rects[j] = new RectangleF(startX, topY, Math.Max(1f, endX - startX - GAP), bottomY - topY);
            }
            
            graphics.FillRectangles(BRUSH, rects);
        }

        public void SetData(IList<PointD> data) => this.data = data;
    }
}