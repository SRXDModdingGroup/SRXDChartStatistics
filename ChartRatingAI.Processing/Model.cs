using AI.Processing;

namespace ChartRatingAI.Processing {
    public class Model : IModel<Model> {
        public ArrayModel ValueCompilerModel { get; }
        
        public ArrayModel WeightCompilerModel { get; }

        public Model(ArrayModel valueCompilerModel, ArrayModel weightCompilerModel) {
            ValueCompilerModel = valueCompilerModel;
            WeightCompilerModel = weightCompilerModel;
        }

        public void Zero() {
            ValueCompilerModel.Zero();
            WeightCompilerModel.Zero();
        }

        public void Normalize(double magnitude) {
            ValueCompilerModel.Normalize(magnitude);
            WeightCompilerModel.Normalize(magnitude);
        }

        public void Add(Model source) {
            ValueCompilerModel.Add(source.ValueCompilerModel);
            WeightCompilerModel.Add(source.WeightCompilerModel);
        }

        public void AddWeighted(double weight, Model source) {
            ValueCompilerModel.AddWeighted(weight, source.ValueCompilerModel);
            WeightCompilerModel.AddWeighted(weight, source.WeightCompilerModel);
        }
    }
}