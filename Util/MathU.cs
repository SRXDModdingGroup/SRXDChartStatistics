using System;

namespace Util {
    public static class MathU {
        private static readonly float ALMOST_EQUALS_THRESHOLD = 0.00048828125f;

        public static bool AlmostEquals(float a, float b) => Math.Abs(a - b) < ALMOST_EQUALS_THRESHOLD;

        public static float Lerp(float a, float b, float t) => (1f - t) * a + t * b;

        public static float Remap(float value, float fromStart, float fromEnd, float toStart, float toEnd) => toStart + (toEnd - toStart) * (value - fromStart) / (fromEnd - fromStart);
        public static double Remap(double value, double fromStart, double fromEnd, double toStart, double toEnd) => toStart + (toEnd - toStart) * (value - fromStart) / (fromEnd - fromStart);

        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(value, max));
        public static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
    }
}