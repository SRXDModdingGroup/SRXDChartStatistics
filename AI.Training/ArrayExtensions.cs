using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AI.Training {
    public static class ArrayExtensions {
        public static double[] Random(int size, Random random) {
            double[] array = new double[size];

            for (int i = 0; i < size; i++)
                array[i] = random.NextDouble();

            array.Normalize();

            return array;
        }

        public static List<(string, double)> EnumerateValues(double[] array, int inputSize, int dimensions) {
            int counter = 0;
            var stack = new Stack<int>();
            var builder = new StringBuilder();
            var list = new List<(string, double)>();

            for (int i = 0; i < inputSize; i++) {
                stack.Push(i);
                Recurse(i, 1);
                stack.Pop();
            }

            list.Add(("()", array[counter]));

            return list;

            void Recurse(int start, int depth) {
                if (depth < dimensions) {
                    for (int i = start; i < inputSize; i++) {
                        stack.Push(i);
                        Recurse(i, depth + 1);
                        stack.Pop();
                    }
                }

                int j = 0;

                foreach (int k in stack.Reverse()) {
                    builder.Append(k);

                    if (j < stack.Count - 1)
                        builder.Append(", ");

                    j++;
                }

                list.Add(($"({builder})", array[counter]));
                builder.Clear();
                counter++;
            }
        }

        internal static void Zero(this double[] array) {
            for (int i = 0; i < array.Length; i++)
                array[i] = 0d;
        }

        internal static void Normalize(this double[] array) {
            double scale = 1d / array.Magnitude();

            for (int i = 0; i < array.Length; i++)
                array[i] *= scale;
        }

        internal static void AddWeighted(this double[] target, double weight, double[] source) {
            for (int i = 0; i < target.Length; i++)
                target[i] += weight * source[i];
        }

        internal static double Magnitude(this double[] array) {
            double sum = 0d;

            for (int i = 0; i < array.Length; i++)
                sum += Math.Abs(array[i]);

            return sum;
        }
    }
}