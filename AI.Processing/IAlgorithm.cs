namespace AI.Processing {
    public interface IAlgorithm<in TIn, out TOut, in TModel> {
        TOut GetResult(TIn input, TModel model);
    }
}