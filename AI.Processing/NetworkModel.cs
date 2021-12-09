using System.IO;

namespace AI.Processing {
    public class NetworkModel {
        public double[][,] Weights { get; }
        
        public int[] LayerSizes { get; }

        protected NetworkModel(double[][,] weights, int[] layerSizes) {
            Weights = weights;
            LayerSizes = layerSizes;
        }

        public static NetworkModel Deserialize(BinaryReader reader) {
            int layerCount = reader.ReadInt32();
            int[] layerSizes = new int[layerCount];

            for (int i = 0; i < layerCount; i++)
                layerSizes[i] = reader.ReadInt32();

            double[][,] layers = new double[layerCount - 1][,];

            for (int i = 0; i < layerCount - 1; i++) {
                int layerRows = layerSizes[i + 1];
                int layerColumns = layerSizes[i] + 1;
                double[,] layer = new double[layerRows, layerColumns];

                for (int j = 0; j < layerRows; j++) {
                    for (int k = 0; k < layerColumns; k++)
                        layer[j, k] = reader.ReadInt32();
                }
            }

            return new NetworkModel(layers, layerSizes);
        }
    }
}