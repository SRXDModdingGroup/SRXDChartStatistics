using AI.Processing;

namespace AI.Training {
    public class OneDimCNN : Processing.OneDimCNN, IBackpropagator<double[][,], double[,], OneDimCNNModel> {
        public OneDimCNN(int layerCount, int[] kernelWidths) : base(layerCount, kernelWidths) { }
        
        public void Backpropagate(double[,] outVector, double[][,] input, OneDimCNNModel model, double[][,] inVector, OneDimCNNModel modelVector) {
            throw new System.NotImplementedException();
        }

        public void BackpropagateFinal(double[,] outVector, double[][,] input, OneDimCNNModel model, OneDimCNNModel modelVector) {
            throw new System.NotImplementedException();
        }
    }
}