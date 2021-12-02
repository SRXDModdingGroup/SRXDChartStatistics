using System;
using System.Collections.Generic;
using ChartHelper;

namespace ChartMetrics {
    internal class Acceleration : PathMetric {
        public override string Description => "The total change in speed / direction over the course of a pattern";

        protected override float ValueForPath(IList<WheelPath.Point> exact, IList<WheelPath.Point> simplified) {
            if (simplified.Count < 2)
                return 0f;

            var first = simplified[0];
            var second = simplified[1];
            
            float sum = Math.Abs((second.NetPosition - first.NetPosition) / (second.Time - first.Time));

            for (int i = 0; i < simplified.Count - 2; i++) {
                var start = simplified[i];
                var mid = simplified[i + 1];
                var end = simplified[i + 2];
                    
                sum += Math.Abs((end.NetPosition - mid.NetPosition) / (end.Time - mid.Time) - (mid.NetPosition - start.NetPosition) / (mid.Time - start.Time));
            }

            var last = simplified[simplified.Count - 1];
            var secondToLast = simplified[simplified.Count - 2];
            
            sum += Math.Abs((last.NetPosition - secondToLast.NetPosition) / (last.Time - secondToLast.Time));

            return sum;
        }

        protected override float ValueForSpin(Note note) => 0f;
    }
}