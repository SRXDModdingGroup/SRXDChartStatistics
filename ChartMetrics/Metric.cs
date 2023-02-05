using System.Collections.Generic;

namespace ChartMetrics {
    public abstract class Metric {
        public string Name => GetType().Name;
        
        public abstract string Description { get; }

        internal abstract IList<MetricPoint> Calculate(ChartProcessor processor);
    }
}