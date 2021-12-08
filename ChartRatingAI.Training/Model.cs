using System.IO;
using AI.Processing;
using ArrayModel = AI.Training.ArrayModel;

namespace ChartRatingAI.Training {
    public class Model : Processing.Model, IModel<Model> {
        private ArrayModel valueCompilerModel;
        private ArrayModel weightCompilerModel;
        
        public Model(ArrayModel valueCompilerModel, ArrayModel weightCompilerModel) : base(valueCompilerModel, weightCompilerModel) {
            this.valueCompilerModel = valueCompilerModel;
            this.weightCompilerModel = weightCompilerModel;
        }
        
        public new static Model Deserialize(BinaryReader reader) {
            var valueCompilerModel = ArrayModel.Deserialize(reader);
            var weightCompilerModel = ArrayModel.Deserialize(reader);

            return new Model(valueCompilerModel, weightCompilerModel);
        }

        public void Serialize(BinaryWriter writer) {
            valueCompilerModel.Serialize(writer);
            weightCompilerModel.Serialize(writer);
        }

        public void Zero() {
            valueCompilerModel.Zero();
            weightCompilerModel.Zero();
        }

        public void Normalize(double magnitude) {
            valueCompilerModel.Normalize(magnitude);
            weightCompilerModel.Normalize(magnitude);
        }

        public void Add(Model source) {
            valueCompilerModel.Add(source.valueCompilerModel);
            weightCompilerModel.Add(source.weightCompilerModel);
        }

        public void AddWeighted(double weight, Model source) {
            valueCompilerModel.AddWeighted(weight, source.valueCompilerModel);
            weightCompilerModel.AddWeighted(weight, source.weightCompilerModel);
        }
    }
}