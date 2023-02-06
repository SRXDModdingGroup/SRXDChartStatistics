using System.Collections.Generic;

namespace ChartMetrics {
    public abstract class Metric {
        private static readonly Metric[] METRICS = {
            new Acceleration(),
            new Drift(),
            new MovementNoteDensity(),
            new OverallNoteDensity(),
            new PointValue(),
            new RequiredMovement(),
            new TapBeatDensity(),
            new SpinDensity()
        };
        
        public string Name => GetType().Name;
        
        public abstract string Description { get; }

        public abstract MetricResult Calculate(ChartData chartData);

        public static bool TryGetMetric(string name, out Metric metric) {
            for (int i = 0; i < METRICS.Length; i++) {
                metric = METRICS[i];

                if (metric.Name == name)
                    return true;
            }

            metric = null;

            return false;
        }

        public static IReadOnlyList<Metric> GetAllMetrics() => METRICS;
    }
}