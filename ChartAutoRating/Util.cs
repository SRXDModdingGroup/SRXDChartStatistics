using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartAutoRating {
    public static class Util {
        public static void Normalize(float[] values) {
            float sum = 0f;
            
            foreach (float value in values)
                sum += Math.Abs(value);

            for (int i = 0; i < values.Length; i++)
                values[i] /= sum;
        }
    }
}