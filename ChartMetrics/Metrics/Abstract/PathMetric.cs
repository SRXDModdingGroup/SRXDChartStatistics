using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartMetrics; 

public abstract class PathMetric : Metric {
    protected abstract void AddPointsForPath(List<MetricPoint> points, ref double sum, IReadOnlyList<WheelPathPoint> path, int start, int end);
        
    public override MetricResult Calculate(ChartData chartData) {
        var notes = chartData.Notes;
        var path = chartData.SimplifiedPath.Points;
        var points = new List<MetricPoint>();
            
        if (notes.Count == 0 || path.Count == 0)
            return MetricResult.Empty;
            
        int pathStartIndex = 0;
        double sum = 0d;

        for (int i = 0; i < path.Count; i++) {
            if (i < path.Count - 1 && !path[i + 1].FirstInPath)
                continue;
                
            AddPointsForPath(points, ref sum, path, pathStartIndex, i);
            pathStartIndex = i + 1;
        }

        return new MetricResult(points);
    }
}