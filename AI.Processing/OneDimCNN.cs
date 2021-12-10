using System;
using System.Threading.Tasks;

namespace AI.Processing {
    public class OneDimCNN : IAlgorithm<double[][,], double[,], OneDimCNNModel> {
        protected int LayerCount { get; }
        
        protected int[] KernelWidths { get; }

        public OneDimCNN(int layerCount, int[] kernelWidths) {
            KernelWidths = kernelWidths;
            LayerCount = layerCount;
        }

        public double[,] GetResult(double[][,] input, OneDimCNNModel model) {
            Convolve(input, model.Layers, LayerCount, KernelWidths);

            return input[LayerCount];
        }

        private static void Convolve(double[][,] input, double[][][,] layers, int layerCount, int[] kernelWidths) {
            for (int i = 0; i < layerCount; i++) {
                double[,] target = input[i + 1];
                double[,] source = input[i];
                double[][,] layer = layers[i];
                int kernelWidth = kernelWidths[i];
                int targetChannels = target.GetLength(0);
                int sourceChannels = source.GetLength(0);
                int sourceLength = source.GetLength(1);
                int targetLength = sourceLength - kernelWidth;

                Parallel.For(0, targetChannels, j => {
                    double[,] kernel = layer[j];

                    for (int k = 0; k < targetLength; k++) {
                        double sum = 0d;

                        for (int l = 0; l < sourceChannels; l++) {
                            for (int m = k, n = 0; n < kernelWidth; m++, n++)
                                sum += kernel[l, n] * source[l, m];
                        }

                        target[j, k] = sum;
                    }
                });
            }
        }
    }
}