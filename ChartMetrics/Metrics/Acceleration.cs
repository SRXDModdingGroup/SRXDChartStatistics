using System;
using System.Collections.Generic;
using ChartHelper.Types;
using Util;

namespace ChartMetrics; 

public class Acceleration : PathMetric {
    public override string Description => "The total change in speed / direction over the course of a pattern";

    protected override void AddPointsForPath(List<MetricPoint> points, ref double sum, IReadOnlyList<WheelPathPoint> path, int start, int end) {
        double speedBefore = 0d;
        
        for (int i = start; i < end; i++) {
            var current = path[i];
            var next = path[i + 1];
            double speedAfter = MathU.Clamp((next.NetPosition - current.NetPosition) / Math.Max(0.0001d, next.Time - current.Time), -100d, 100d);

            sum += Math.Abs(speedAfter - speedBefore);
            points.Add(new MetricPoint(current.Time, sum, false));
            speedBefore = speedAfter;
        }

        sum += Math.Abs(speedBefore);
        points.Add(new MetricPoint(path[end].Time, sum, false));
    }
}