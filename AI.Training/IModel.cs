namespace AI.Processing {
    public interface IModel<in T> where T : IModel<T> {
        void Zero();
        
        void AddWeighted(double weight, T source);
        
        double Magnitude();
    }
}