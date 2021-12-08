﻿using System.IO;
using AI.Processing;

namespace AI.Training {
    public class Compiler : Processing.Compiler, IBackpropagator<double[], double, ArrayModel> {
        public Compiler(int inputSize, int dimensions) : base(inputSize, dimensions) { }
        
        public static void SerializeModel(double[] model, BinaryWriter writer) {
            writer.Write(model.Length);
            
            foreach (double coeff in model)
                writer.Write(coeff);
        }

        public void Backpropagate(double outVector, double[] input, ArrayModel model, double[] inVector, ArrayModel modelVector) {
            double[] modelArray = model.Array;
            double[] vectorArray = modelVector.Array;
            int counter = 0;

            for (int i = 0; i < InputSize; i++)
                Recurse(input[i], i, 1);

            vectorArray[counter] += outVector;

            void Recurse(double product, int start, int depth) {
                if (depth < Dimensions) {
                    for (int i = start; i < InputSize; i++)
                        Recurse(input[i] * product, i, depth + 1);
                }
                
                if (product > 0d) {
                    vectorArray[counter] += outVector / product;
                    
                    for (int i = 0; i < Dimensions; i++)
                        inVector[i] += outVector * input[i] / (modelArray[counter] * product);
                }
                
                counter++;
            }
        }
        
        public void BackpropagateFinal(double outVector, double[] input, ArrayModel model, ArrayModel modelVector) {
            double[] modelArray = model.Array;
            int counter = 0;

            for (int i = 0; i < InputSize; i++)
                Recurse(input[i], i, 1);

            modelArray[counter] += outVector;

            void Recurse(double product, int start, int depth) {
                if (depth < Dimensions) {
                    for (int i = start; i < InputSize; i++)
                        Recurse(input[i] * product, i, depth + 1);
                }
                
                if (product > 0d)
                    modelArray[counter] += outVector / product;

                counter++;
            }
        }
    }
}