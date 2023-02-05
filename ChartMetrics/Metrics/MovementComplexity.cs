using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartMetrics {
    public class MovementComplexity : PathMetric {
        public override string Description => "The deviation between the exact and simplified movement path";

        protected override float ValueForPath(IList<WheelPathPoint> exact, IList<WheelPathPoint> simplified) {
            if (exact.Count < 3)
                return 0f;

            float sum = 0f;

            for (int i = 1; i < exact.Count - 1; i++) {
                float diff = exact[i].LanePosition - simplified[i].LanePosition;

                sum += 0.5f * (exact[i + 1].Time - exact[i - 1].Time) * diff * diff;
            }

            return sum;
        }

        protected override float ValueForSpin(Note note) => 0f;
    }
}