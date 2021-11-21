using System.Collections.Generic;

namespace ChartStatistics {
    public abstract class Metric {
        public readonly struct Point {
            public float Time { get; }
            public float Value { get; }

            public Point(float time, float value) {
                Time = time;
                Value = value;
            }
        }
        
        public abstract string Name { get; }
        
        public abstract string Description { get; }

        public abstract IList<Point> Calculate(ChartProcessor processor);
    }
}