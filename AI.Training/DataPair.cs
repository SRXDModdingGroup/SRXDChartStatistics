namespace AI.Training {
    public class DataPair<TData, TResult> {
        public TData Data { get; }
        
        public TResult ExpectedResult { get; }

        public DataPair(TData data, TResult expectedResult) {
            Data = data;
            ExpectedResult = expectedResult;
        }
    }
}