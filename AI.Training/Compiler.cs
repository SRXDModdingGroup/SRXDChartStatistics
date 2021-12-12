using System;

namespace AI.Training {
    public class Compiler : Processing.Compiler, IBackpropagator<double[], double, ArrayModel> {
        private double cachedSum = 0d;
        
        public Compiler(int inputSize, int dimensions) : base(inputSize, dimensions) { }

        public void Backpropagate(double output, double dF_dO, double[] input, double[] dIn, ArrayModel model, ArrayModel dModel)
            => throw new NotImplementedException();

        public void BackpropagateFinal(double output, double dF_dO, double[] input, ArrayModel model, ArrayModel dModel) {
            double dF_dS = Math.Exp(SHARPNESS * cachedSum);
            
            dF_dS = dF_dO * dF_dS / (1f + dF_dS);
                
            double[] dArray = dModel.Array;
            int counter = 0;
            

            for (int i = 0; i < InputSize; i++)
                Recurse(input[i], i, 1);

            dArray[counter] += dF_dS;

            void Recurse(double product, int start, int depth) {
                if (depth < Dimensions) {
                    for (int i = start; i < InputSize; i++)
                        Recurse(input[i] * product, i, depth + 1);
                }

                dArray[counter] += dF_dS * product;
                counter++;
            }
        }

        public double GetResult(double[] input, ArrayModel model) {
            double[] array = model.Array;
            double sum = 0d;
            int counter = 0;

            for (int i = 0; i < InputSize; i++)
                Recurse(input[i], i, 1);

            sum += array[counter];
            cachedSum = sum;

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