using System.Drawing;
using ChartHelper.Types;

namespace ChartStatistics {
    public class HoldSegment : Drawable {
        private static readonly Pen PEN = new Pen(Color.White);
        
        private float startY;
        private float endY;
        private bool isRed;
        private CurveType type;
        
        public HoldSegment(float start, float end, float startY, float endY, bool isRed, CurveType type) : base(start, end, DrawLayer.Hold) {
            this.startY = startY;
            this.endY = endY;
            this.isRed = isRed;
            this.type = type;
        }

        public override void Draw(GraphicsPanel panel, Graphics graphics) {
            if (isRed)
                PEN.Color = Color.Red;
            else
                PEN.Color = Color.Cyan;
            
            float x1 = panel.TimeToX(Start);
            float y1 = panel.ValueToY(startY);
            float x4 = panel.TimeToX(End);
            float y4 = panel.ValueToY(endY);

            switch (type) {
                case CurveType.Cosine:
                    graphics.DrawBezier(PEN,
                        x1, y1,
                        0.65f * x1 + 0.35f * x4, y1,
                        0.35f * x1 + 0.65f * x4, y4,
                        x4, y4);
                    
                    break;
                case CurveType.CurveOut:
                    graphics.DrawBezier(PEN,
                        x1, y1,
                        0.85f * x1 + 0.15f * x4, y1,
                        0.5f * x1 + 0.5f * x4, y1,
                        x4, y4);

                    break;
                case CurveType.CurveIn:
                    graphics.DrawBezier(PEN,
                        x1, y1,
                        0.5f * x1 + 0.5f * x4, y4,
                        0.15f * x1 + 0.85f * x4, y4,
                        x4, y4);
                    
                    break;
                case CurveType.Linear:
                    graphics.DrawLine(PEN, x1, y1, x4, y4);
                    
                    break;
                case CurveType.Angular:
                    float xMid = panel.TimeToX(End - 0.05f);
                    
                    graphics.DrawLines(PEN, new [] {
                        new PointF(x1, y1),
                        new PointF(xMid, y1),
                        new PointF(xMid, y4),
                        new PointF(x4, y4)
                    });
                    
                    break;
            }
        }
    }
}