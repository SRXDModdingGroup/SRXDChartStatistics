using AI.Processing;

namespace ChartRatingAI.Processing {
    public class Algorithm : IAlgorithm<Data, double, Model> {
        protected Compiler ValueCompiler { get; set; }
        protected Compiler WeightCompiler { get; set; }
        
        protected Algorithm() { }

        public Algorithm(int sampleSize, int dimensions) {
            ValueCompiler = new Compiler(sampleSize, dimensions);
            WeightCompiler = new Compiler(sampleSize, dimensions);
        }
        
        public double GetResult(Data input, Model model) {
            double sumValue = 0d;
            double sumWeight = 0d;
            var samples = input.Samples;
            var valueCompilerModel = model.ValueCompilerModel;
            var weightCompilerModel = model.WeightCompilerModel;

            for (int i = 0; i < samples.Length; i++) {
                var sample = samples[i];
                double[] values = sample.Values;
                double weight = sample.Weight * WeightCompiler.GetResult(values, valueCompilerModel);

                sumValue += weight * ValueCompiler.GetResult(values, weightCompilerModel);
                sumWeight += weight;
            }

            return sumValue / sumWeight;
        }
    }
}