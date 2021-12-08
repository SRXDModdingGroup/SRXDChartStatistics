using System.IO;
using AI.Processing;

namespace ChartRatingAI.Processing {
    public class Model {
        public ArrayModel ValueCompilerModel { get; }
        
        public ArrayModel WeightCompilerModel { get; }

        public Model(ArrayModel valueCompilerModel, ArrayModel weightCompilerModel) {
            ValueCompilerModel = valueCompilerModel;
            WeightCompilerModel = weightCompilerModel;
        }

        public static Model Deserialize(BinaryReader reader) {
            var valueCompilerModel = ArrayModel.Deserialize(reader);
            var weightCompilerModel = ArrayModel.Deserialize(reader);

            return new Model(valueCompilerModel, weightCompilerModel);
        }
    }
}