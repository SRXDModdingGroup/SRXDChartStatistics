using System.Collections.Generic;

namespace ChartMetrics; 

public class MetricResult {
    public IReadOnlyList<MetricPoint> Points => points;

    public MetricResult(IReadOnlyList<MetricPoint> points) => this.points = new List<MetricPoint>(points);

    private List<MetricPoint> points;
}