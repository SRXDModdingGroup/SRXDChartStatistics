using System;
using System.Drawing;

namespace ChartStatistics {
    public class Grid : Drawable {
        private static readonly Pen PEN = new Pen(Color.Black);
        
        private float bottom;
        private float top;
        private float horizontalBars;
        private int ticksPerMajorTick;
        
        public Grid(float bottom, float top, float horizontalBars, int ticksPerMajorTick) : base(0f, float.PositiveInfinity, 0) {
            this.top = top;
            this.bottom = bottom;
            this.horizontalBars = horizontalBars;
            this.ticksPerMajorTick = ticksPerMajorTick;
        }

        public override void Draw(GraphicsPanel panel, Graphics graphics) {
            int j = (int) Math.Ceiling(panel.Scroll);

            float bottomY = panel.ValueToY(bottom);
            float topY = panel.ValueToY(top);
            
            for (int i = j; i < panel.RightBound; i++) {
                if (j % ticksPerMajorTick == 0)
                    PEN.Color = Color.DimGray;
                else
                    PEN.Color = Color.FromArgb(255, 32, 32, 32);

                float x = panel.TimeToX(i);

                graphics.DrawLine(PEN, x, bottomY, x, topY);
                j++;
            }
            
            PEN.Color = Color.FromArgb(255, 32, 32, 32);

            float spacing = (top - bottom) / (horizontalBars - 1);
            
            for (int i = 0; i < horizontalBars; i++) {
                float y = panel.ValueToY(bottom + spacing * i);
                
                graphics.DrawLine(PEN, 0f, y, panel.Width, y);
            }
        }
    }
}