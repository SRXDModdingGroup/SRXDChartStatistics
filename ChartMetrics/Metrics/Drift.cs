using System;
using System.Collections.Generic;
using ChartHelper;

namespace ChartMetrics {
    internal class Drift : PathMetric {
        public override string Description => "The distance that a pattern strays from a centered position";

        protected override float ValueForPath(IList<WheelPath.Point> exact, IList<WheelPath.Point> simplified) {
            if (simplified.Count < 2)
                return 0f;
                
            float sum = 0f;

            for (int i = 0; i < simplified.Count - 1; i++) {
                var start = simplified[i];
                var end = simplified[i + 1];
                    
                sum += Math.Abs(0.5f * (end.NetPosition + start.NetPosition)) * (end.Time - start.Time);
            }

            return sum;
        }

        protected override float ValueForSpin(Note note) => 0f;
    }
}