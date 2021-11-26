using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChartAutoRating {
    public partial class Form1 : Form {
        private static readonly int SPACING = 80;
        private static readonly int PADDING = 8;
        private static readonly int BOX_SIZE = 8;
        private static readonly SolidBrush BRUSH = new SolidBrush(Color.Black);
        private static readonly Color CLEAR_COLOR = Color.FromArgb(16, 16, 24);

        private BufferedGraphics buffer;
        
        public Form1() {
            InitializeComponent();
            Size = new Size(2 * PADDING + (Program.GROUP_COUNT - 1) * SPACING + (1 + Program.METRIC_COUNT) * BOX_SIZE + 18,
                2 * PADDING + Program.CALCULATOR_COUNT * BOX_SIZE + 38);
        }

        public void Draw(DrawInfoItem[][] drawInfo, double best, double worst) {
            if (buffer == null) {
                buffer?.Dispose();
                buffer = BufferedGraphicsManager.Current.Allocate(panel1.CreateGraphics(), panel1.Bounds);
            }
            
            var graphics = buffer.Graphics;
            
            graphics.Clear(CLEAR_COLOR);

            for (int i = 0; i < Program.GROUP_COUNT; i++) {
                var group = drawInfo[i];
                int startX = i * SPACING + PADDING;

                for (int j = 0; j < Program.CALCULATOR_COUNT; j++) {
                    var info = group[j];
                    int y = PADDING + j * BOX_SIZE;
                    double interp = (info.Fitness - worst) / (best - worst);

                    if (interp < 0d)
                        interp = 0d;

                    if (interp > 1d)
                        interp = 1d;
                    
                    int value = (int) (255d * interp);

                    DrawBox(0, Color.FromArgb(value, value, value));

                    for (int k = 0; k < Program.METRIC_COUNT; k++) {
                        var weights = info.CurveWeights[k];
                        double max = Math.Max(weights.W0, Math.Max(weights.W1, weights.W2));

                        if (max == 0d)
                            max = 1d;
                        
                        double scale = 255d * Math.Sqrt(weights.Magnitude) / max;
                    
                        DrawBox(k + 1, Color.FromArgb(
                            (int) (scale * weights.W0),
                            (int) (scale * weights.W1),
                            (int) (scale * weights.W2)));
                    }

                    void DrawBox(int n, Color color) {
                        BRUSH.Color = color;
                        graphics.FillRectangle(BRUSH, startX + n * BOX_SIZE, y, BOX_SIZE, BOX_SIZE);
                    }
                }
            }
            
            panel1.Invalidate();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            buffer?.Render(e.Graphics);
        }
    }
}