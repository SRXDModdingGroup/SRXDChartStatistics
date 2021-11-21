using System;
using System.Collections.Generic;

namespace ChartStatistics {
    public static class Util {
        private static readonly float ALMOST_EQUALS_THRESHOLD = 0.00048828125f;

        public static bool AlmostEquals(float a, float b) => Math.Abs(a - b) < ALMOST_EQUALS_THRESHOLD;

        public static float Lerp(float a, float b, float t) => (1f - t) * a + t * b;

        public static float Remap(float value, float fromStart, float fromEnd, float toStart, float toEnd) => toStart + (toEnd - toStart) * (value - fromStart) / (fromEnd - fromStart);

        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(value, max));

        public static float Median(IList<float> values) {
            float median = values[values.Count / 2];

            if (values.Count % 2 == 0)
                return 0.5f * (median + values[values.Count / 2 + 1]);

            return median;
        }

        public static List<int> Subdivide(IList<int> set, Func<int, int, int, float> quantityToMaximize, Func<int, int, int, float, bool> endCondition) {
            var newSet = new List<int> { 0 };

            Recurse(0, set.Count - 1);

            return newSet;

            void Recurse(int start, int end) {
                if (end - start < 2)
                    return;

                int bestIndex = 0;
                float bestValue = 0f;

                for (int i = start + 1; i < end; i++) {
                    float value = quantityToMaximize(start, end, i);
                    
                    if (value <= bestValue)
                        continue;

                    bestIndex = i;
                    bestValue = value;
                }
                
                if (endCondition(start, end, bestIndex, bestValue))
                    return;
                
                Recurse(start, bestIndex);
                newSet.Add(bestIndex);
                Recurse(bestIndex, end);
            }
        }
    }
}