using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChartRatingTrainer {
    public partial class Form1 : Form {
        private static readonly int COLUMNS = 4;
        private static readonly int PADDING = 8;
        private static readonly int BOX_SIZE = 8;
        
        private static readonly SolidBrush BRUSH = new SolidBrush(Color.Black);
        private static readonly Color CLEAR_COLOR = Color.FromArgb(16, 16, 24);

        private int spacingX;
        private int spacingY;
        private BufferedGraphics buffer;
        
        public Form1() {
            InitializeComponent();

            int rows = Program.POPULATION_SIZE / COLUMNS;
            
            spacingX = (Calculator.METRIC_COUNT + 2) * BOX_SIZE + PADDING;
            spacingY = Calculator.METRIC_COUNT * BOX_SIZE + PADDING;
            Size = new Size(2 * PADDING + (COLUMNS - 1) * spacingX + (Calculator.METRIC_COUNT + 2) * BOX_SIZE + 18,
                2 * PADDING + (rows - 1) * spacingY + Calculator.METRIC_COUNT * BOX_SIZE + 38);
        }

        public void Draw(DrawInfoItem[] drawInfo, double best, double worst) {
            if (buffer == null) {
                buffer?.Dispose();
                buffer = BufferedGraphicsManager.Current.Allocate(panel1.CreateGraphics(), panel1.Bounds);
            }
            
            var graphics = buffer.Graphics;
            
            graphics.Clear(CLEAR_COLOR);

            for (int i = 0; i < Program.POPULATION_SIZE; i++) {
                var info = drawInfo[i];  
                int startX = i % COLUMNS * spacingX + PADDING;
                int startY = i / COLUMNS * spacingY + PADDING;
                double interp = (info.Fitness - worst) / (best - worst);

                if (interp < 0d)
                    interp = 0d;

                if (interp > 1d)
                    interp = 1d;
                    
                int value = (int) (255d * interp);

                DrawBox(0, 0, Color.FromArgb(value, value, value));
                DrawBox(1, 0, info.Color);

                for (int row = 0; row < Calculator.METRIC_COUNT; row++) {
                    for (int column = row; column < Calculator.METRIC_COUNT + 1; column++) {
                        var curve = info.Curves[row, column];
                        double scale = 0d;

                        if (curve.Magnitude > 0d) {
                            double max = Math.Max(curve.W0, Math.Max(curve.W1, curve.W2));

                            scale = 255d * Math.Sqrt(max) / curve.Magnitude;
                        }
                    
                        DrawBox(row, column + 1, Color.FromArgb(
                            (int) (scale * curve.W0),
                            (int) (scale * curve.W1),
                            (int) (scale * curve.W2)));
                    }
                }

                void DrawBox(int row, int column, Color color) {
                    BRUSH.Color = color;
                    graphics.FillRectangle(BRUSH, startX + column * BOX_SIZE, startY + row * BOX_SIZE, BOX_SIZE, BOX_SIZE);
                }
            }
            
            panel1.Invalidate();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            buffer?.Render(e.Graphics);
        }
    }
}