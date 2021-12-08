using System.IO;

namespace AI.Processing {
    public class Compiler : IAlgorithm<double[], double, ArrayModel> {
        public int InputSize { get; }

        public int Dimensions { get; }
        
        public int ModelSize { get; }

        public Compiler(int inputSize, int dimensions) {
            InputSize = inputSize;
            Dimensions = dimensions;
            
            int num = 1;

            for (int i = inputSize + 1; i < inputSize + dimensions + 1; i++)
                num *= i;

            int den = 1;

            for (int i = 2; i <= dimensions; i++)
                den *= i;
            
            ModelSize = num / den;
        }

        public double GetResult(double[] input, ArrayModel model) {
            double[] array = model.Array;
            double sum = 0d;
            int counter = 0;

            for (int i = 0; i < InputSize; i++)
                Recurse(input[i], i, 1);

            sum += array[counter];

            return sum;

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