using System.Collections.Generic;

namespace ChartMetrics {
    public abstract class Metric {
        internal readonly struct Point {
            public float Time { get; }
            public float Value { get; }

            public Point(float time, float value) {
                Time = time;
                Value = value;
            }
        }

        public string Name => GetType().Name;
        
        public abstract string Description { get; }

        internal abstract IList<Point> Calculate(ChartProcessor processor);
    }
}