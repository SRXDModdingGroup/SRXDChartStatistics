using AI.Processing;

namespace ChartRatingAI.Processing {
    public class Algorithm : IAlgorithm<Data, double, Model> {
        private Compiler valueCompiler;
        private Compiler weightCompiler;

        public Algorithm(int sampleSize, int dimensions) {
            valueCompiler = new Compiler(sampleSize, dimensions);
            weightCompiler = new Compiler(sampleSize, dimensions);
        }
        
        public double GetResult(Data input, Model model) {
            
        }
    }
}