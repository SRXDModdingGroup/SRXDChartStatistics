namespace AI.Processing {
    public interface IModel<in T> where T : IModel<T> {
        void Zero();

        void Multiply(double factor);

        void Add(T source);
        
        void AddWeighted(double weight, T source);
        
        double Magnitude();
    }
}