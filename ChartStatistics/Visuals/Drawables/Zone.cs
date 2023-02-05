using System.Drawing;
using System.Drawing.Drawing2D;

namespace ChartStatistics {
    public class Zone : Drawable {
        private static readonly Brush BRUSH_RIGHT_SPIN = new HatchBrush(HatchStyle.ForwardDiagonal, Color.MediumPurple);
        private static readonly Brush BRUSH_LEFT_SPIN = new HatchBrush(HatchStyle.BackwardDiagonal, Color.Teal);
        private static readonly Brush BRUSH_SCRATCH = new HatchBrush(HatchStyle.Divot, Color.Orange);
        private static readonly Brush BRUSH_BEAT_HOLD = new SolidBrush(Color.FromArgb(64, 0, 255, 0));
        
        public enum ZoneType {
            RightSpin,
            LeftSpin,
            Scratch,
            BeatHold
        }

        private Brush brush;
        private float bottom;
        private float top;
        
        public Zone(double start, double end, DrawLayer layer, ZoneType type, float bottom, float top) : base(start, end, layer) {
            this.bottom = bottom;
            this.top = top;

            switch (type) {
                case ZoneType.RightSpin:
                    brush = BRUSH_RIGHT_SPIN;
                    
                    break;
                case ZoneType.LeftSpin:
                    brush = BRUSH_LEFT_SPIN;

                    break;
                case ZoneType.Scratch:
                    brush = BRUSH_SCRATCH;

                    break;
                case ZoneType.BeatHold:
                    brush = BRUSH_BEAT_HOLD;

                    break;
            }
        }

        public override void Draw(GraphicsPanel panel, Graphics graphics) {
            float startX = panel.TimeToX(Start);
            float width = panel.TimeToX(End) - startX;
            float startY = panel.ValueToY(bottom);
            
            graphics.FillRectangle(brush, startX, startY, width, panel.ValueToY(top) - startY);
        }
    }
}