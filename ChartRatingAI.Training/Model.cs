using System.IO;
using AI.Processing;
using ArrayModel = AI.Training.ArrayModel;

namespace ChartRatingAI.Training {
    public class Model : Processing.Model, IModel<Model> {
        public new ArrayModel ValueCompilerModel { get; }
        public new ArrayModel WeightCompilerModel { get; }
        
        public Model(ArrayModel valueCompilerModel, ArrayModel weightCompilerModel) : base(valueCompilerModel, weightCompilerModel) {
            ValueCompilerModel = valueCompilerModel;
            WeightCompilerModel = weightCompilerModel;
        }
        
        public new static Model Deserialize(BinaryReader reader) {
            var valueCompilerModel = ArrayModel.Deserialize(reader);
            var weightCompilerModel = ArrayModel.Deserialize(reader);

            return new Model(valueCompilerModel, weightCompilerModel);
        }

        public void Serialize(BinaryWriter writer) {
            ValueCompilerModel.Serialize(writer);
            WeightCompilerModel.Serialize(writer);
        }

        public void Zero() {
            ValueCompilerModel.Zero();
            WeightCompilerModel.Zero();
        }

        public void AddWeighted(double weight, Model source) {
            ValueCompilerModel.AddWeighted(weight, source.ValueCompilerModel);
            WeightCompilerModel.AddWeighted(weight, source.WeightCompilerModel);
        }

        public double Magnitude() => ValueCompilerModel.Magnitude() + WeightCompilerModel.Magnitude();
    }
}