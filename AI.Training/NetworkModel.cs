using System.IO;
using AI.Processing;

namespace AI.Training {
    public class NetworkModel : Processing.NetworkModel, IModel<NetworkModel> {
        public NetworkModel(double[][,] weights, int[] layerSizes) : base(weights, layerSizes) { }
        
        public new static NetworkModel Deserialize(BinaryReader reader) {
            int layerCount = reader.ReadInt32();
            int[] layerSizes = new int[layerCount];

            for (int i = 0; i < layerCount; i++)
                layerSizes[i] = reader.ReadInt32();

            double[][,] layers = new double[layerCount - 1][,];

            for (int i = 0; i < layerCount - 1; i++) {
                int rows = layerSizes[i + 1];
                int columns = layerSizes[i] + 1;
                double[,] matrix = new double[rows, columns];

                for (int j = 0; j < rows; j++) {
                    for (int k = 0; k < columns; k++)
                        matrix[j, k] = reader.ReadInt32();
                }

                layers[i] = matrix;
            }

            return new NetworkModel(layers, layerSizes);
        }

        public void Serialize(BinaryWriter writer) {
            int layerCount = LayerSizes.Length;
            
            writer.Write(layerCount);

            foreach (int size in LayerSizes)
                writer.Write(size);

            for (int i = 0; i < Weights.Length; i++) {
                int rows = LayerSizes[i + 1];
                int columns = LayerSizes[i] + 1;
                double[,] matrix = Weights[i];

                for (int j = 0; j < rows; j++) {
                    for (int k = 0; k < columns; k++)
                        writer.Write(matrix[j, k]);
                }
            }
        }
        
        public void Zero() {
            for (int i = 0; i < Weights.Length; i++) {
                int rows = LayerSizes[i + 1];
                int columns = LayerSizes[i] + 1;
                double[,] matrix = Weights[i];

                for (int j = 0; j < rows; j++) {
                    for (int k = 0; k < columns; k++)
                        matrix[j, k] = 0d;
                }
            }
        }

        public void Normalize(double magnitude) {
            double sum = 0d;
            
            for (int i = 0; i < Weights.Length; i++) {
                int rows = LayerSizes[i + 1];
                int columns = LayerSizes[i] + 1;
                double[,] matrix = Weights[i];

                for (int j = 0; j < rows; j++) {
                    for (int k = 0; k < columns; k++)
                        sum += matrix[j, k];
                }
            }

            double scale = 1d / sum;
            
            for (int i = 0; i < Weights.Length; i++) {
                int rows = LayerSizes[i + 1];
                int columns = LayerSizes[i] + 1;
                double[,] matrix = Weights[i];

                for (int j = 0; j < rows; j++) {
                    for (int k = 0; k < columns; k++)
                        matrix[j, k] *= scale;
                }
            }
        }

        public void Add(NetworkModel source) {
            double[][,] sourceWeights = source.Weights;
            
            for (int i = 0; i < Weights.Length; i++) {
                int rows = LayerSizes[i + 1];
                int columns = LayerSizes[i] + 1;
                double[,] matrix = Weights[i];
                double[,] sourceMatrix = sourceWeights[i];

                for (int j = 0; j < rows; j++) {
                    for (int k = 0; k < columns; k++)
                        matrix[j, k] += sourceMatrix[j, k];
                }
            }
        }

        public void AddWeighted(double weight, NetworkModel source) {
            double[][,] sourceWeights = source.Weights;
            
            for (int i = 0; i < Weights.Length; i++) {
                int rows = LayerSizes[i + 1];
                int columns = LayerSizes[i] + 1;
                double[,] matrix = Weights[i];
                double[,] sourceMatrix = sourceWeights[i];

                for (int j = 0; j < rows; j++) {
                    for (int k = 0; k < columns; k++)
                        matrix[j, k] += weight * sourceMatrix[j, k];
                }
            }
        }
    }
}