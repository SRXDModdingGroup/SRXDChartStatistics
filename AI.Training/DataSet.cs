using System;
using System.Threading.Tasks;
using AI.Processing;

namespace AI.Training {
    public abstract class DataSet<TData> where TData : class {
        public int Size { get; }
        
        public int BatchCount { get; }

        public DataPair<TData, double>[] Data { get; }

        private int batchSize { get; }

        private double[] results { get; }

        protected DataSet(int size, int batchCount) {
            Size = size;
            BatchCount = batchCount;
            Data = new DataPair<TData, double>[size];
            batchSize = Size / BatchCount + 1;
            results = new double[size];
        }

        public void Shuffle(Random random) {
            for (int i = Size - 1; i > 2; i--) {
                int index = random.Next(i - 1);

                (Data[i], Data[index]) = (Data[index], Data[i]);
            }
        }

        public double Backpropagate<TBackpropagator, TModel>(TBackpropagator algorithm, TModel model, TModel vector,
            double approachFactor, double minVectorMagnitude, out double[] results)
            where TBackpropagator : IBackpropagator<TData, double, TModel>
            where TModel : class, IModel<TModel> {
            double sumError = 0d;

            results = this.results;
            
            for (int i = 0; i < BatchCount; i++) {
                int batchStart = batchSize * i;
                int batchEnd = Math.Min(batchStart + batchSize, Size);
            
                vector.Zero();

                for (int j = batchStart; j < batchEnd; j++) {
                    var pair = Data[j];
                    var data = pair.Data;
                    double result = algorithm.GetResult(data, model);
                    double error = pair.ExpectedResult - result;

                    results[j] = result;
                    sumError += error * error;
                    algorithm.BackpropagateFinal(result, error, data, model, vector);
                }

                model.AddWeighted(approachFactor / vector.Magnitude(), vector);
            }

            return 1d - Math.Sqrt(sumError / Size);
        }
        
        public double GetResults<TAlgorithm, TModel>(TAlgorithm algorithm, TModel model, out double[] results)
            where TAlgorithm : IAlgorithm<TData, double, TModel> {
            double sumError = 0d;

            results = this.results;
            
            for (int i = 0; i < Size; i++) {
                var pair = Data[i];
                var data = pair.Data;
                double result = algorithm.GetResult(data, model);
                double error = pair.ExpectedResult - result;

                results[i] = result;
                sumError += error * error;
            }

            return 1d - Math.Sqrt(sumError / Size);
        }
    }
}