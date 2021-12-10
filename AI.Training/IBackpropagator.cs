using AI.Processing;

namespace AI.Training {
    public interface IBackpropagator<in TIn, TOut, in TModel> : IAlgorithm<TIn, TOut, TModel> where TModel : class {
        void Backpropagate(TOut outVector, TIn input, TModel model, TIn inVector, TModel modelVector);

        void BackpropagateFinal(TOut outVector, TIn input, TModel model, TModel modelVector);
    }
}