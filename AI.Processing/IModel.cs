namespace AI.Processing {
    public interface IModel<in T> where T : IModel<T> {
        void Zero();

        void Normalize(double magnitude);

        void Add(T source);
        
        void AddWeighted(double weight, T source);
    }
}