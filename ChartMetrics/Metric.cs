using System.Collections.Generic;

namespace ChartMetrics {
    public abstract class Metric {
        private static readonly Metric[] METRICS = {
            new Acceleration(),
            new MovementNoteDensity(),
            new OverallNoteDensity(),
            new PointValue(),
            new RequiredMovement(),
            new SpinDensity(),
            new TapBeatDensity()
        };
        
        public string Name => GetType().Name;
        
        public abstract string Description { get; }

        public abstract MetricResult Calculate(ChartData chartData);

        public static bool TryGetMetric(string name, out Metric metric) {
            name = name.ToLowerInvariant();
            
            for (int i = 0; i < METRICS.Length; i++) {
                metric = METRICS[i];

                if (metric.Name.ToLowerInvariant() == name)
                    return true;
            }

            metric = null;

            return false;
        }

        public static IReadOnlyList<Metric> GetAllMetrics() => METRICS;
    }
}