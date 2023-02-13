using System;
using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartMetrics; 

public class RequiredMovement : PathMetric {
    public override string Description => "The minimum amount of movement required to hit every positional note in a pattern";

    protected override void AddPointsForPath(List<MetricPoint> points, ref double sum, IReadOnlyList<WheelPathPoint> path, int start, int end) {
        points.Add(new MetricPoint(path[start].Time, sum, false));
            
        for (int i = start + 1; i <= end; i++) {
            var previous = path[i - 1];
            var current = path[i];

            sum += Math.Abs(current.NetPosition - previous.NetPosition);
            points.Add(new MetricPoint(current.Time, sum, true));
        }
    }

    protected override double GetValueForSpin(Note note) => 8f;
}