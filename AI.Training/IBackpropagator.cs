using AI.Processing;

namespace AI.Training {
    public interface IBackpropagator<in TIn, TOut, in TModel> : IAlgorithm<TIn, TOut, TModel> where TModel : class {
        void Backpropagate(TOut output, TOut dF_dO, TIn input, TIn dIn, TModel model, TModel dModel);

        void BackpropagateFinal(TOut output, TOut dF_dO, TIn input, TModel model, TModel dModel);
    }
}