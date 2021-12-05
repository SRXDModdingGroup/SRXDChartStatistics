using System;
using System.Drawing;
using System.Windows.Forms;
using ChartMetrics;
using MatrixAI.Processing;

namespace ChartRatingTrainer {
    public partial class Form1 : Form {
        private static readonly int METRIC_COUNT = ChartProcessor.DifficultyMetrics.Count;
        private static readonly int PADDING = 8;
        private static readonly int BOX_SIZE = 8;
        private static readonly int EXPECTED_RETURNED_SIZE = 512;

        private static readonly Pen PEN = new Pen(Color.FromArgb(64, Color.White));
        private static readonly SolidBrush BRUSH = new SolidBrush(Color.Black);
        private static readonly Color CLEAR_COLOR = Color.FromArgb(16, 16, 24);

        private int spacingX;

        private int width;
        private int expectedReturnedStart;
        private BufferedGraphics buffer;
        
        public Form1() {
            InitializeComponent();
            
            spacingX = (METRIC_COUNT + 2) * BOX_SIZE + PADDING;
            width = (Program.POPULATION_SIZE - 1) * spacingX + (METRIC_COUNT + 2) * BOX_SIZE;
            expectedReturnedStart = 2 * PADDING + METRIC_COUNT * BOX_SIZE;
            Size = new Size(2 * PADDING + width + 18, expectedReturnedStart + EXPECTED_RETURNED_SIZE + PADDING + 38);
        }

        public void Draw(double fitness, Matrix matrix, PointF[] expectedReturned, double best, double worst) {
            if (buffer == null) {
                buffer?.Dispose();
                buffer = BufferedGraphicsManager.Current.Allocate(panel1.CreateGraphics(), panel1.Bounds);
            }
            
            var graphics = buffer.Graphics;
            
            graphics.Clear(CLEAR_COLOR);

            // for (int i = 0; i < Program.POPULATION_SIZE; i++) {
            //     int startX = i * spacingX + PADDING;
            //     double interp = (fitness - worst) / (best - worst);
            //
            //     if (interp < 0d)
            //         interp = 0d;
            //
            //     if (interp > 1d)
            //         interp = 1d;
            //         
            //     int value = (int) (255d * interp);
            //
            //     DrawBox(0, 0, Color.FromArgb(value, value, value));
            //
            //     for (int row = 0; row < METRIC_COUNT; row++) {
            //         for (int column = row; column < METRIC_COUNT; column++)
            //             DrawBoxForCoefficients(column + 1, matrix.ValueCoefficients[row, column]);
            //
            //         DrawBoxForCoefficients(METRIC_COUNT + 1, matrix.WeightCoefficients[row]);
            //
            //         void DrawBoxForCoefficients(int index, Coefficients coefficients) {
            //             double magnitude = coefficients.Magnitude;
            //
            //             coefficients /= Math.Max(Math.Max(Math.Max(Math.Max(coefficients.X1, coefficients.X2), coefficients.X3), coefficients.X4), coefficients.X5);
            //             
            //             double r = 0.5d * ((coefficients.X1 + 2d * coefficients.X2) / 3d + 1d);
            //             double g = 0.5d * ((coefficients.X3 + 2d * coefficients.X4) / 3d + 1d);
            //             double b = 0.5d * ((coefficients.X5 + 2d * coefficients.X6) / 3d + 1d);
            //             double scale = 225d * Math.Min(magnitude, 1d);
            //             
            //             DrawBox(row, index, Color.FromArgb(
            //                 (int) (scale * r),
            //                 (int) (scale * g),
            //                 (int) (scale * b)));
            //         }
            //     }
            //     
            //     void DrawBox(int row, int column, Color color) {
            //         BRUSH.Color = color;
            //         graphics.FillRectangle(BRUSH, startX + column * BOX_SIZE, PADDING + row * BOX_SIZE, BOX_SIZE, BOX_SIZE);
            //     }
            // }
            
            BRUSH.Color = Color.White;
            graphics.DrawLine(PEN, PADDING, expectedReturnedStart + EXPECTED_RETURNED_SIZE, PADDING + width, expectedReturnedStart);

            foreach (var point in expectedReturned) {
                int x = PADDING + (int) (point.X * width);
                int y = expectedReturnedStart + (int) ((1d - point.Y) * EXPECTED_RETURNED_SIZE);
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