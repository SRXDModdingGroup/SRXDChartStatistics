using System;
using System.Diagnostics;

namespace AI.Processing {
    public class Compiler : IAlgorithm<double[], double, ArrayModel> {
        protected static readonly double SHARPNESS = 8d;
        protected static readonly double INV_SHARPNESS = 1d / SHARPNESS;
        
        protected int InputSize { get; }

        protected int Dimensions { get; }

        public Compiler(int inputSize, int dimensions) {
            InputSize = inputSize;
            Dimensions = dimensions;
        }

        public double GetResult(double[] input, ArrayModel model) {
            double[] array = model.Array;
            double sum = 0d;
            int counter = 0;

            for (int i = 0; i < InputSize; i++)
                Recurse(input[i], i, 1);

            sum += array[counter];

            return INV_SHARPNESS * Math.Log(1d + Math.Exp(SHARPNESS * sum));

            void Recurse(double product, int start, int depth) {
                if (depth < Dimensions) {
                    for (int i = start; i < InputSize; i++)
                        Recurse(input[i] * product, i, depth + 1);
                }
                
                sum += array[counter] * product;
                counter++;
            }
        }
    }
}