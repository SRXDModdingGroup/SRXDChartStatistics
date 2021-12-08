using System;
using System.Threading.Tasks;
using AI.Processing;

namespace AI.Training {
    public abstract class DataSet<TData> where TData : class {
        public int Size { get; }
        
        public int BatchCount { get; }

        public DataPair<TData, double>[] Data { get; }

        protected int BatchSize { get; }
        
        protected double[] Results { get; }

        protected DataSet(int size, int batchCount) {
            Size = size;
            BatchCount = batchCount;
            Data = new DataPair<TData, double>[size];
            BatchSize = Size / BatchCount + 1;
            Results = new double[size];
        }
        
        public void Backpropagate<TModel>(IBackpropagator<TData, double, TModel>[] instances, TModel model, TModel[] vectors,
            double approachFactor, double vectorMagnitude, Random random) where TModel : class, IModel<TModel> {
            Shuffle(random);

            for (int i = 0; i < BatchCount; i++) {
                int batchStart = BatchSize * i;
                int batchEnd = Math.Min(batchStart + BatchSize, Size);
                int groupSize = (batchEnd - batchStart) / instances.Length + 1;
                
                Parallel.For(0, instances.Length, GetResultsInGroup);
                GetRegression(batchStart, batchEnd, out double scale, out double bias);

                for (int j = batchStart; j < batchEnd; j++) {
                    double error = Data[j].ExpectedResult - scale * (Results[j] + bias);
                    
                    Parallel.For(0, instances.Length, k => GetVectorForGroup(k, Math.Sign(error) * error * error));
                }

                var overallVector = vectors[0];

                for (int j = 1; j < vectors.Length; j++)
                    overallVector.Add(vectors[i]);
                
                overallVector.Normalize(vectorMagnitude);
                model.AddWeighted(approachFactor, overallVector);
                model.Normalize(1d);

                void GetResultsInGroup(int j) {
                    var instance = instances[j];
                    int groupStart = j * groupSize;
                    int groupEnd = Math.Min(groupStart + groupSize, Size);

                    for (int k = groupStart; k < groupEnd; k++)
                        Results[k] = instance.GetResult(Data[k].Data, model);
                }

                void GetVectorForGroup(int j, double outVector) {
                    var instance = instances[j];
                    var vector = vectors[j];
                    int groupStart = j * groupSize;
                    int groupEnd = Math.Min(groupStart + groupSize, Size);
                    
                    vector.Zero();
                    
                    for (int k = groupStart; k < groupEnd; k++)
                        instance.BackpropagateFinal(outVector, Data[k].Data, model, vector);
                }
            }
        }
        
        public double GetFitnessAndResults<TModel>(IAlgorithm<TData, double, TModel>[] instances, TModel model,
            out double scale, out double bias, out double[] results) {
            int groupSize = Size / instances.Length + 1;

            Parallel.For(0, instances.Length, GetResultsInGroup);
            GetRegression(0, Size, out scale, out bias);

            double sumError = 0d;

            for (int i = 0; i < Size; i++) {
                double result = scale * (Results[i] + bias);
                double error = Data[i].ExpectedResult - result;
                
                Results[i] = result;
                sumError += error * error;
            }

            results = Results;

            return 1d - Math.Sqrt(sumError / Size);

            void GetResultsInGroup(int i) {
                var instance = instances[i];
                int groupStart = i * groupSize;
                int groupEnd = Math.Min(groupStart + groupSize, Size);

                for (int j = groupStart; j < groupEnd; j++)
                    Results[j] = instance.GetResult(Data[j].Data, model);
            }
        }

        public double[] GetResults<TModel>(IAlgorithm<TData, double, TModel>[] instances, TModel model, out double scale, out double bias) {
            int groupSize = Size / instances.Length + 1;

            Parallel.For(0, instances.Length, GetResultsInGroup);
            GetRegression(0, Size, out scale, out bias);

            for (int i = 0; i < Size; i++)
                Results[i] = scale * (Results[i] + bias);

            return Results;

            void GetResultsInGroup(int i) {
                var instance = instances[i];
                int groupStart = i * groupSize;
                int groupEnd = Math.Min(groupStart + groupSize, Size);

                for (int j = groupStart; j < groupEnd; j++)
                    Results[j] = instance.GetResult(Data[j].Data, model);
            }
        }
        
        private void Shuffle(Random random) {
            for (int i = Size - 1; i > 2; i--) {
                int index = random.Next(i - 1);

                (Data[i], Data[index]) = (Data[index], Data[i]);
            }
        }

        private void GetRegression(int start, int end, out double scale, out double bias) {
            int count = end - start;
            double sx = 0d;
            double sy = 0d;
            double sxx = 0d;
            double sxy = 0d;

            for (int i = start; i < end; i++) {
                double expected = Data[i].ExpectedResult;
                double returned = Results[i];

                sx += expected;
                sy += returned;
                sxx += expected * expected;
                sxy += expected * returned;
            }

            scale = (count * sxx - sx * sx) / (count * sxy - sx * sy);
            bias = (sy * sxx - sxy * sx) / (sx * sx - count * sxx);
        }
    }
}