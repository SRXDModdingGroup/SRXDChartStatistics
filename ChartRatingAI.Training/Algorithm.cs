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
            double sumValue = 0d;
            double sumWeight = 0d;
            var samples = input.Samples;
            var valueCompilerModel = model.ValueCompilerModel;
            var weightCompilerModel = model.WeightCompilerModel;

            for (int i = 0; i < input.Size; i++) {
                var sample = samples[i];
                double[] values = sample.Values;
                double value = valueCompiler.GetResult(values, weightCompilerModel);
                double weight = sample.Weight * weightCompiler.GetResult(values, valueCompilerModel);
                
                sumValue += weight * valueCompiler.GetResult(values, weightCompilerModel);
                sumWeight += weight;
                input.CachedValues[i] = value;
                input.CachedWeights[i] = weight;
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

            outVector *= input.CachedSumWeight / input.Size;
            
            for (int i = 0; i < input.Size; i++) {
                var sample = samples[i];
                double[] values = sample.Values;

                valueCompiler.BackpropagateFinal(outVector / cachedWeights[i], values, valueCompilerModel, valueCompilerVector);
                weightCompiler.BackpropagateFinal(outVector / (cachedValues[i] * sample.Weight), values, weightCompilerModel, weightCompilerVector);
            }
        }
    }
}