using System;
using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartMetrics {
    internal class RequiredMovement : PathMetric {
        public override string Description => "The minimum amount of movement required to hit every positional note in a pattern";

        protected override float ValueForPath(IList<WheelPathPoint> exact, IList<WheelPathPoint> simplified) {
            if (simplified.Count < 2)
                return 0f;
                
            float sum = 0f;

            for (int i = 0; i < simplified.Count - 1; i++)
                sum += Math.Abs(simplified[i + 1].NetPosition - simplified[i].NetPosition);

            return sum;
        }

        protected override float ValueForSpin(Note note) => 16f;
    }
}