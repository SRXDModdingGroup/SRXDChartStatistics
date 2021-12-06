using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixAI.Processing;

namespace MatrixAI.Training {
    public static class MatrixExtensions {
        public static Matrix Random(int sampleSize, int dimensions, Random random) {
            var matrix = new Matrix(sampleSize, dimensions);

            for (int i = 0; i < matrix.TotalSize; i++)
                matrix.Coefficients[i] = random.NextDouble();
            
            Normalize(matrix);

            return matrix;
        }

        public static Matrix Identity(int sampleSize, int dimensions) {
            var matrix = new Matrix(sampleSize, dimensions);
            int counter = 0;

            for (int i = 0; i < sampleSize; i++) {
                if (matrix.Dimensions > 1) {
                    for (int j = i; j < matrix.SampleSize; j++)
                        Recurse(j, 2);
                }

                matrix.Coefficients[counter] = 1d;
                counter++;
            }

            Normalize(matrix);

            return matrix;

            void Recurse(int start, int depth) {
                if (depth < matrix.Dimensions) {
                    for (int i = start; i < matrix.SampleSize; i++)
                        Recurse(i, depth + 1);
                }
                
                counter++;
            }
        }

        public static List<(string, double)> EnumerateValues(Matrix matrix) {
            int counter = 0;
            var stack = new Stack<int>();
            var builder = new StringBuilder();
            var list = new List<(string, double)>();
            
            for (int i = 0; i < matrix.SampleSize; i++) {
                stack.Push(i);
                Recurse(i, 1);
                stack.Pop();
            }

            list.Add(("()", matrix.Coefficients[counter]));

            return list;
            
            void Recurse(int start, int depth) {
                if (depth < matrix.Dimensions) {
                    for (int i = start; i < matrix.SampleSize; i++) {
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
                
                list.Add(($"({builder})", matrix.Coefficients[counter]));
                builder.Clear();
                counter++;
            }
        }

        internal static void Zero(Matrix target) {
            for (int i = 0; i < target.TotalSize; i++)
                target.Coefficients[i] = 0d;
        }

        internal static void Normalize(Matrix target) {
            double sum = 0d;
            
            for (int i = 0; i < target.TotalSize; i++)
                sum += Math.Abs(target.Coefficients[i]);

            double scale = 1d / sum;
            
            for (int i = 0; i < target.TotalSize; i++)
                target.Coefficients[i] *= scale;
        }

        internal static void AddWeighted(Matrix target, double weight, Matrix source) {
            for (int i = 0; i < target.TotalSize; i++)
                target.Coefficients[i] += weight * source.Coefficients[i];
        }

        internal static double GetValueAndVector(this Matrix matrix, Matrix vector, double[] values) {
            double sum = 0d;
            int counter = 0;

            for (int i = 0; i < vector.SampleSize; i++)
                Recurse(values[i], i, 1);

            sum += matrix.Coefficients[counter];
            vector.Coefficients[counter] = 1d;

            return sum;

            void Recurse(double product, int start, int depth) {
                if (depth < vector.Dimensions) {
                    for (int i = start; i < vector.SampleSize; i++)
                        Recurse(values[i] * product, i, depth + 1);
                }

                sum += matrix.Coefficients[counter] * product;
                vector.Coefficients[counter] = product;
                counter++;
            }
        }
    }
}