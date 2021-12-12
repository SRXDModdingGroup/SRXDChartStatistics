using System;
using System.Threading.Tasks;
using AI.Training;

namespace ChartRatingAI.Training {
    public class Algorithm : Processing.Algorithm, IBackpropagator<Data, double, Model> {
        private Compiler valueCompiler;
        private Compiler weightCompiler;
        
        public Algorithm(int sampleSize, int dimensions) {
            valueCompiler = new Compiler(sampleSize, dimensions);
            weightCompiler = new Compiler(sampleSize, dimensions);
            ValueCompiler = valueCompiler;
            WeightCompiler = weightCompiler;
        }
        
        public double GetResult(Data input, Model model) {
            var samples = input.Samples;
            var valueCompilerModel = model.ValueCompilerModel;
            var weightCompilerModel = model.WeightCompilerModel;
            
            Parallel.For(0, input.Size, i => {
                var sample = samples[i];
                double[] values = sample.Values;
                double value = valueCompiler.GetResult(values, valueCompilerModel);
                double weight = weightCompiler.GetResult(values, weightCompilerModel);
                
                input.CachedValues[i] = value;
                input.CachedWeights[i] = weight;
            });
            
            double sumValue = 0d;
            double sumWeight = 0d;

            for (int i = 0; i < input.Size; i++) {
                double overallWeight = input.CachedWeights[i] * samples[i].Weight;
                
                sumValue += overallWeight * input.CachedValues[i];
                sumWeight += overallWeight;
            }

            input.SumWeight = sumWeight;
            
            if (sumValue <= 0d || sumWeight <= 0d)
                return 0d;

            return sumValue / sumWeight;
        }

        public void Backpropagate(double output, double dF_dO, Data input, Data dIn, Model model, Model dModel) =>
            throw new NotImplementedException();

        public void BackpropagateFinal(double output, double dF_dO, Data input, Model model, Model dModel) {
            if (output == 0d)
                return;
            
            var samples = input.Samples;
            var valueCompilerModel = model.ValueCompilerModel;
            var weightCompilerModel = model.WeightCompilerModel;
            var dValueCompilerModel = dModel.ValueCompilerModel;
            var dWeightCompilerModel = dModel.WeightCompilerModel;
            double[] values = input.CachedValues;
            double[] weights = input.CachedWeights;

            double factor = dF_dO / input.SumWeight;
            
            for (int i = 0; i < input.Size; i++) {
                var sample = samples[i];
                double[] sampleValues = sample.Values;
                double factor2 = factor * sample.Weight;

                valueCompiler.BackpropagateFinal(values[i], factor2 * weights[i],
                    sampleValues, valueCompilerModel, dValueCompilerModel);
                weightCompiler.BackpropagateFinal(weights[i], factor2 * (values[i] - output),
                    sampleValues, weightCompilerModel, dWeightCompilerModel);
            }
        }
    }
}