// using AI.Processing;
//
// namespace AI.Training {
//     public class OneDimCNN : Processing.OneDimCNN, IBackpropagator<double[][,], double[,], OneDimCNNModel> {
//         public OneDimCNN(int layerCount, int[] kernelWidths) : base(layerCount, kernelWidths) { }
//         
//         public void Backpropagate(double[,] output, double[,] dF_dO, double[][,] input, double[][,] dIn, OneDimCNNModel model, OneDimCNNModel dModel) {
//             throw new System.NotImplementedException();
//         }
//
//         public void BackpropagateFinal(double[,] output, double[,] dF_dO, double[][,] input, OneDimCNNModel model, OneDimCNNModel dModel) {
//             throw new System.NotImplementedException();
//         }
//     }
// }