using System;
using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartMetrics {
    internal class Drift : PathMetric {
        public override string Description => "The distance that a pattern strays from a centered position";

        protected override void AddPointsForPath(List<MetricPoint> points, ref double sum, IReadOnlyList<WheelPathPoint> path, int start, int end) {
            points.Add(new MetricPoint(path[start].Time, sum));

            double position = 0d;
            
            for (int i = start + 1; i <= end; i++) {
                var previous = path[i - 1];
                var current = path[i];
                double positionDiff = current.NetPosition - previous.NetPosition;

                sum += (position + 0.5f * positionDiff) * (current.Time - previous.Time);
                points.Add(new MetricPoint(current.Time, sum));
                position += positionDiff;
            }
        }

        protected override double GetValueForSpin(Note note) => 0f;
    }
}