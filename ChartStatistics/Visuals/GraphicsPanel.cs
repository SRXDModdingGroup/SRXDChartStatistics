using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChartStatistics {
    public class GraphicsPanel {
        private static readonly float SCROLL_SENSITIVITY = 0.5f;
        private static readonly float ZOOM_SENSITIVITY = 0.125f;
        private static readonly int MAX_ZOOM = 6;

        private float scroll;
        public float Scroll {
            get => scroll;
            set {
                scroll = value;
                RightBound = XToTime(panel.Width);
                Redraw();
            }
        }
        
        private float zoom;
        public float Zoom {
            get => zoom;
            set {
                zoom = value;
                zoomFactor = (float) Math.Pow(2f, zoom);
                horizontalScaleFactor = panel.Height * zoomFactor;
                RightBound = XToTime(panel.Width);
                Redraw();
            }
        }
        
        public float RightBound { get; private set; }

        public int Width => panel.Width;

        public int Height => panel.Height;

        private bool needsNewBuffer = false;
        private float zoomFactor;
        private float horizontalScaleFactor;
        private SortedDictionary<Drawable.DrawLayer, HashSet<Drawable>> layers;
        private BufferedGraphics buffer;
        private Panel panel;

        public GraphicsPanel(Panel panel) {
            this.panel = panel;
            layers = new SortedDictionary<Drawable.DrawLayer, HashSet<Drawable>>();
            Zoom = 0f;

            panel.Paint += Panel_Paint;
            panel.MouseWheel += Panel_MouseWheel;
            panel.Resize += Panel_Resize;
        }

        public void AddDrawable(Drawable drawable) {
            if (!layers.TryGetValue(drawable.Layer, out var drawables)) {
                drawables = new HashSet<Drawable>();
                layers.Add(drawable.Layer, drawables);
            }
            
            drawables.Add(drawable);
        }

        public void RemoveDrawable(Drawable drawable) {
            if (!layers.TryGetValue(drawable.Layer, out var drawables))
                return;
            
            drawables.Remove(drawable);

            if (drawables.Count == 0)
                layers.Remove(drawable.Layer);
        }

        public void Clear() {
            foreach (var layer in layers) 
                layer.Value.Clear();
            
            layers.Clear();
        }

        public void Redraw() => panel.Invalidate();

        public float TimeToX(float time) => horizontalScaleFactor * (time - scroll);

        public float XToTime(float x) => x / horizontalScaleFactor + scroll;

        public float ValueToY(float value) => panel.Height * value;

        public float YToValue(float y) => y / panel.Height;

        private void Draw(Graphics graphics) {
            if (buffer == null || needsNewBuffer) {
                buffer?.Dispose();
                buffer = BufferedGraphicsManager.Current.Allocate(graphics, panel.Bounds);
                needsNewBuffer = false;
            }
            else {
                buffer.Render(graphics);
                buffer.Graphics.Clear(Color.Black);
            }

            foreach (var layer in layers) {
                foreach (var drawable in layer.Value) {
                    if (drawable.Start < RightBound || drawable.End > Scroll)
                        drawable.Draw(this, buffer.Graphics);
                }
            }
            
            buffer.Render(graphics);
        }
        
        private void Panel_Paint(object sender, PaintEventArgs e) => Program.Execute(() => Draw(e.Graphics));

        private void Panel_MouseWheel(object sender, MouseEventArgs e) {
            if (Control.ModifierKeys.HasFlag(Keys.Shift)) {
                float oldScaleFactor = horizontalScaleFactor;

                Zoom = Math.Max(-MAX_ZOOM, Math.Min(Zoom + ZOOM_SENSITIVITY * Math.Sign(e.Delta), MAX_ZOOM));
                Scroll = Math.Max(0f, Scroll + e.X / oldScaleFactor - e.X / horizontalScaleFactor);
            }
            else
                Scroll = Math.Max(0f, Scroll + SCROLL_SENSITIVITY * Math.Sign(e.Delta) / zoomFactor);
        }

        private void Panel_Resize(object sender, EventArgs e) {
            needsNewBuffer = true;
            horizontalScaleFactor = panel.Height * zoomFactor;
            RightBound = XToTime(panel.Width);
            panel.Invalidate();
        }
    }
}