// using System;
// using System.IO;
// using AI.Processing;
//
// namespace AI.Training {
//     public class OneDimCnnModel : Processing.OneDimCNNModel, IModel<OneDimCnnModel> {
//         public OneDimCnnModel(double[][][,] layers) : base(layers) { }
//
//         public new static OneDimCNNModel Deserialize(BinaryReader reader) {
//             int layerCount = reader.ReadInt32();
//
//             double[][][,] layers = new double[layerCount][][,];
//
//             for (int i = 0; i < layerCount; i++) {
//                 int kernelCount = reader.ReadInt32();
//                 int kernelRows = reader.ReadInt32();
//                 int kernelColumns = reader.ReadInt32();
//                 
//                 double[][,] layer = new double[kernelCount][,];
//
//                 for (int j = 0; j < kernelCount; j++) {
//                     double[,] kernel = new double[kernelRows, kernelColumns];
//
//                     for (int k = 0; k < kernelRows; k++) {
//                         for (int l = 0; l < kernelColumns; l++)
//                             kernel[k, l] = reader.ReadDouble();
//                     }
//
//                     layer[j] = kernel;
//                 }
//
//                 layers[i] = layer;
//             }
//
//             return new OneDimCNNModel(layers);
//         }
//
//         public void Serialize(BinaryWriter writer) {
//             writer.Write(Layers.Length);
//
//             foreach (double[][,] layer in Layers) {
//                 writer.Write(layer.Length);
//
//                 int kernelRows = layer[0].GetLength(0);
//                 int kernelColumns = layer[0].GetLength(1);
//                 
//                 writer.Write(kernelRows);
//                 writer.Write(kernelColumns);
//
//                 foreach (double[,] kernel in layer) {
//                     for (int i = 0; i < kernelRows; i++) {
//                         for (int j = 0; j < kernelColumns; j++)
//                             writer.Write(kernel[i, j]);
//                     }
//                 }
//             }
//         }
//
//         public void Zero() {
//             foreach (double[][,] layer in Layers) {
//                 int kernelRows = layer[0].GetLength(0);
//                 int kernelColumns = layer[0].GetLength(1);
//
//                 foreach (double[,] kernel in layer) {
//                     for (int i = 0; i < kernelRows; i++) {
//                         for (int j = 0; j < kernelColumns; j++)
//                             kernel[i, j] = 0d;
//                     }
//                 }
//             }
//         }
//
//         public void AddWeighted(double weight, OneDimCnnModel source) {
//             double[][][,] sourceLayers = source.Layers;
//
//             for (int i = 0; i < Layers.Length; i++) {
//                 double[][,] layer = Layers[i];
//                 double[][,] sourceLayer = sourceLayers[i];
//                 int kernelRows = layer[0].GetLength(0);
//                 int kernelColumns = layer[0].GetLength(1);
//
//                 for (int j = 0; j < layer.Length; j++) {
//                     double[,] kernel = layer[j];
//                     double[,] sourceKernel = sourceLayer[j];
//                     
//                     for (int k = 0; k < kernelRows; k++) {
//                         for (int l = 0; l < kernelColumns; l++)
//                             kernel[k, l] += weight * sourceKernel[k, l];
//                     }
//                 }
//             }
//         }
//
//         public double Magnitude() {
//             double sum = 0d;
//             
//             foreach (double[][,] layer in Layers) {
//                 int kernelRows = layer[0].GetLength(0);
//                 int kernelColumns = layer[0].GetLength(1);
//
//                 foreach (double[,] kernel in layer) {
//                     for (int i = 0; i < kernelRows; i++) {
//                         for (int j = 0; j < kernelColumns; j++)
//                             sum += Math.Abs(kernel[i, j]);
//                     }
//                 }
//             }
//
//             return sum;
//         }
//     }
// }