namespace ChartStatistics {
    public static class Util {
        public static float Lerp(float a, float b, float t) => (1f - t) * a + t * b;

        public static float Remap(float value, float fromStart, float fromEnd, float toStart, float toEnd) => toStart + (toEnd - toStart) * (value - fromStart) / (fromEnd - fromStart);
    }
}