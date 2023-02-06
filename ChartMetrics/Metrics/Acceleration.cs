using System;
using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartMetrics {
    internal class Acceleration : PathMetric {
        public override string Description => "The total change in speed / direction over the course of a pattern";

        protected override void AddPointsForPath(List<MetricPoint> points, ref double sum, IReadOnlyList<WheelPathPoint> path, int start, int end) {
            double speedBefore = 0d;
            
            for (int i = start; i < end; i++) {
                var current = path[i];
                var next = path[i + 1];
                double speedAfter = (next.NetPosition - current.NetPosition) / (next.Time - current.Time);

                sum += Math.Abs(speedAfter - speedBefore);
                points.Add(new MetricPoint(current.Time, sum));
                speedBefore = speedAfter;
            }

            sum += Math.Abs(speedBefore);
            points.Add(new MetricPoint(path[end].Time, sum));
        }

        protected override double GetValueForSpin(Note note) => 160d;
    }
}