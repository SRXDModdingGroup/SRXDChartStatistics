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
                double value = valueCompiler.GetResult(values, weightCompilerModel);
                double weight = sample.Weight * weightCompiler.GetResult(values, valueCompilerModel);
                
                input.CachedValues[i] = value;
                input.CachedWeights[i] = weight;
            });
            
            double sumValue = 0d;
            double sumWeight = 0d;

            for (int i = 0; i < input.Size; i++) {
                double weight = input.CachedWeights[i];
                
                sumValue += weight * input.CachedValues[i];
                sumWeight += weight;
            }

            input.CachedSumWeight = sumWeight;

            return sumValue / sumWeight;
        }

        public void Backpropagate(double outVector, Data input, Model model, Data inVector, Model modelVector) =>
            throw new System.NotImplementedException();

        public void BackpropagateFinal(double outVector, Data input, Model model, Model modelVector) {
            var samples = input.Samples;
            var valueCompilerModel = model.ValueCompilerModel;
            var weightCompilerModel = model.WeightCompilerModel;
            var valueCompilerVector = modelVector.ValueCompilerModel;
            var weightCompilerVector = modelVector.WeightCompilerModel;
            double[] cachedValues = input.CachedValues;
            double[] cachedWeights = input.CachedWeights;

            outVector /= input.CachedSumWeight;
            
            for (int i = 0; i < input.Size; i++) {
                var sample = samples[i];
                double[] values = sample.Values;
                int j = i;
                
                Parallel.Invoke(
                    () => valueCompiler.BackpropagateFinal(outVector * cachedWeights[j], values, valueCompilerModel, valueCompilerVector),
                    () => weightCompiler.BackpropagateFinal(outVector * cachedValues[j] * sample.Weight, values, weightCompilerModel, weightCompilerVector));
            }
        }
    }
}