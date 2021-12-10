using System.IO;

namespace AI.Processing {
    public class OneDimCNNModel {
        public double[][][,] Layers { get; }

        public OneDimCNNModel(double[][][,] layers) => Layers = layers;

        public static OneDimCNNModel Deserialize(BinaryReader reader) {
            int layerCount = reader.ReadInt32();

            double[][][,] layers = new double[layerCount][][,];

            for (int i = 0; i < layerCount; i++) {
                int kernelCount = reader.ReadInt32();
                int kernelRows = reader.ReadInt32();
                int kernelColumns = reader.ReadInt32();
                
                double[][,] layer = new double[kernelCount][,];

                for (int j = 0; j < kernelCount; j++) {
                    double[,] kernel = new double[kernelRows, kernelColumns];

                    for (int k = 0; k < kernelRows; k++) {
                        for (int l = 0; l < kernelColumns; l++)
                            kernel[k, l] = reader.ReadDouble();
                    }

                    layer[j] = kernel;
                }

                layers[i] = layer;
            }

            return new OneDimCNNModel(layers);
        }
    }
}