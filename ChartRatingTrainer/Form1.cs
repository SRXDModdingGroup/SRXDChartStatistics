using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChartRatingTrainer {
    public partial class Form1 : Form {
        private static readonly int PADDING = 8;
        private static readonly Pen PEN = new Pen(Color.FromArgb(64, Color.White));
        private static readonly SolidBrush BRUSH = new SolidBrush(Color.Black);
        private static readonly Color CLEAR_COLOR = Color.FromArgb(16, 16, 24);

        private BufferedGraphics buffer;
        
        public Form1() {
            InitializeComponent();
        }

        public void Draw(PointF[] expectedReturned, double slope, double intercept) {
            if (buffer == null) {
                buffer?.Dispose();
                buffer = BufferedGraphicsManager.Current.Allocate(panel1.CreateGraphics(), panel1.Bounds);
            }
            
            var graphics = buffer.Graphics;
            int width = panel1.Width;
            int height = panel1.Height;
            
            graphics.Clear(CLEAR_COLOR);
            BRUSH.Color = Color.White;

            int startY = (int) ((1d - intercept) * (height - 2 * PADDING));
            int endY = (int) ((1d - intercept - slope) * (height - 2 * PADDING));
            
            graphics.DrawLine(PEN, PADDING, PADDING + startY, width - PADDING, PADDING + endY);

            foreach (var point in expectedReturned) {
                int x = PADDING + (int) (point.X * (width - 2 * PADDING));
                int y = PADDING + (int) ((1d - point.Y) * (height - 2 * PADDING));
                float diff = point.Y - point.X;
                int gb = (int) (255f * (1f - Math.Min(32f * diff * diff, 1f)));
                
                BRUSH.Color = Color.FromArgb(255, gb, gb);
                graphics.FillRectangle(BRUSH, x, y, 2, 2);
            }
            
            panel1.Invalidate();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            buffer?.Render(e.Graphics);
        }
    }
}