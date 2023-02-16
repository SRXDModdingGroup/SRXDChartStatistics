using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChartMetrics;
using Util;

namespace ModelTraining; 

public static class Training {
    private const int POPULATION_SIZE = 128;
    private const int HALF_POPULATION_SIZE = POPULATION_SIZE / 2;
    private const double COEFFICIENT_MIN = 0.01d;
    private const double POWER_MIN = 0.5d;
    private const double POWER_MAX = 2d;
    private const int THREAD_COUNT = 4;

    public static double Rate(DataElement element, Parameters[] model) => Rate(element.RatingData, model);
    
    public static double CalculateFitness(Parameters[] model, Superset superset) {
        var datasets = superset.Datasets;
        int largest = 0;
        int totalPairCount = 0;
        
        foreach (var dataset in datasets) {
            int count = dataset.Elements.Count;

            if (count > largest)
                largest = count;

            totalPairCount += count * (count - 1) / 2;
        }

        return 0.5d * ((double) CalculateFitnessFlat(model, datasets, new double[largest]) / totalPairCount + 1d);
    }
    
    public static Parameters[] TrainGenetic(Superset superset, int iterations, double mutationAmount) {
        var datasets = superset.Datasets;
        int metricCount = superset.MetricCount;
        var random = new Random();
        var population = new ModelFitnessPair[POPULATION_SIZE];
        var threadInfos = new ThreadInfo[THREAD_COUNT];
        int largest = 0;
        
        foreach (var dataset in datasets) {
            int count = dataset.Elements.Count;

            if (count > largest)
                largest = count;
        }

        for (int i = 0; i < THREAD_COUNT; i++)
            threadInfos[i] = new ThreadInfo(metricCount, largest, 0);

        for (int i = 0; i < POPULATION_SIZE; i++) {
            var model = new Parameters[metricCount];
            
            for (int j = 0; j < model.Length; j++)
                model[j] = new Parameters(MathU.Lerp(COEFFICIENT_MIN, 1d, random.NextDouble()), MathU.Lerp(POWER_MIN, POWER_MAX, random.NextDouble()));
            
            NormalizeModel(model);
            population[i] = new ModelFitnessPair(model, CalculateFitnessSmooth(model, datasets, threadInfos[0].Ratings));
        }
        
        Array.Sort(population);

        int totalPairCount = 0;

        foreach (var dataset in datasets)
            totalPairCount += dataset.PairCount;

        for (int i = 1; i <= iterations; i++) {
            Parallel.For(0, THREAD_COUNT, j => {
                var threadInfo = threadInfos[j];
                var vector = threadInfo.Vector;
                double[] ratings = threadInfo.Ratings;
                int endIndex;

                if (j < THREAD_COUNT - 1)
                    endIndex = (j + 1) * HALF_POPULATION_SIZE / THREAD_COUNT;
                else
                    endIndex = HALF_POPULATION_SIZE;

                for (int k = j * HALF_POPULATION_SIZE / THREAD_COUNT; k < endIndex; k++) {
                    var source = population[k];
                    var target = population[k + HALF_POPULATION_SIZE];
                    var sourceModel = source.Model;
                    var targetModel = target.Model;
                
                    RandomVector(vector, random);
                    AddToModel(sourceModel, vector, targetModel, mutationAmount);
                    target.Fitness = CalculateFitnessSmooth(targetModel, datasets, ratings);
                }
            });
            
            Array.Sort(population);
            
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                break;

            if (i % 1000 > 0)
                continue;
        }

        return population[0].Model;
    }
    
    public static Parameters[] TrainGradient(Superset superset, int iterations, double acceleration, double startTimestep, double endTimestep, CancellationToken ct) {
        var datasets = superset.Datasets;
        int metricCount = superset.MetricCount;
        var model = GetInitialModel(superset);
        
        Console.WriteLine(CalculateFitness(model, superset));
        
        int largest = 0;
        int totalPairCount = 0;
        
        foreach (var dataset in datasets) {
            int count = dataset.Elements.Count;

            if (count > largest)
                largest = count;
            
            totalPairCount += count * (count - 1) / 2;
        }

        double[] ratings = new double[largest];
        var dRatings_dParams = new List<Parameters[]>(largest);

        for (int i = 0; i < largest; i++)
            dRatings_dParams.Add(new Parameters[model.Length]);

        var vector = new Parameters[metricCount];
        var momentum = new Parameters[metricCount];

        for (int i = 1; i <= iterations && !ct.IsCancellationRequested; i++) {
            double timestep = MathU.Remap(i, 1d, iterations, startTimestep, endTimestep);
            
            CalculateGradient(model, datasets, vector, ratings, dRatings_dParams);
            InterpolateVector(momentum, vector, momentum, acceleration);
            AddToModel(model, momentum, model, timestep / totalPairCount);
        }
        
        return model;
    }

    private static void AddToModel(Parameters[] source, Parameters[] vector, Parameters[] target, double amount) {
        for (int i = 0; i < source.Length; i++)
            target[i] = source[i] + amount * vector[i];

        NormalizeModel(target);
    }

    private static void InterpolateVector(Parameters[] from, Parameters[] to, Parameters[] target, double t) {
        for (int i = 0; i < from.Length; i++)
            target[i] = (1d - t) * from[i] + t * to[i];
    }

    private static void NormalizeModel(Parameters[] model) {
        double sum = 0d;

        foreach (var parameters in model)
            sum += parameters.Coefficient;

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            
            model[i] = new Parameters(
                MathU.Clamp(parameters.Coefficient / sum, COEFFICIENT_MIN, 1d),
                MathU.Clamp(parameters.Power, POWER_MIN, POWER_MAX));
        }
    }

    private static void NormalizeVector(Parameters[] vector) {
        double sum = 0d;

        foreach (var parameters in vector)
            sum += parameters.Coefficient * parameters.Coefficient + parameters.Power * parameters.Power;

        sum = 1d / Math.Sqrt(sum);

        for (int i = 0; i < vector.Length; i++)
            vector[i] *= sum;
    }

    private static void RandomVector(Parameters[] vector, Random random) {
        for (int i = 0; i < vector.Length; i++) {
            double coefficient = 2d * random.NextDouble() - 1d;
            double power = 2d * random.NextDouble() - 1d;

            vector[i] = new Parameters(coefficient, power);
        }

        NormalizeVector(vector);
    }

    private static void CalculateGradient(Parameters[] model, List<Dataset> datasets, Parameters[] vector, double[] ratings, List<Parameters[]> dRatings_dParams) {
        for (int i = 0; i < vector.Length; i++)
            vector[i] = new Parameters();

        foreach (var dataset in datasets) {
            var elements = dataset.Elements;

            for (int i = 0; i < elements.Count; i++) {
                double[] ratingData = elements[i].RatingData;
                var dRating_dParams = dRatings_dParams[i];

                ratings[i] = Rate(ratingData, model);

                for (int j = 0; j < ratingData.Length; j++) {
                    double ratingDatum = ratingData[j];
                    var parameters = model[j];
                    double pow = Math.Pow(ratingDatum, parameters.Power);

                    dRating_dParams[j] = new Parameters(pow, parameters.Coefficient * pow * Math.Log(ratingDatum));
                }
            }

            for (int i = 0; i < elements.Count; i++) {
                double firstRating = ratings[i];
                var dFirstRating_dParams = dRatings_dParams[i];
                    
                for (int j = 0; j < i; j++) {
                    double secondRating = ratings[j];
                    double dFitness_dDiff = DSoftSign(firstRating - secondRating);
                    var dSecondRating_dParams = dRatings_dParams[j];
                    
                    for (int k = 0; k < vector.Length; k++)
                        vector[k] += dFitness_dDiff * (dFirstRating_dParams[k] - dSecondRating_dParams[k]);
                }
            }
        }
    }

    private static int CalculateFitnessFlat(Parameters[] model, List<Dataset> datasets, double[] ratings) {
        int sum = 0;
        
        foreach (var dataset in datasets) {
            var elements = dataset.Elements;

            for (int i = 0; i < elements.Count; i++) {
                ratings[i] = Rate(elements[i].RatingData, model);
                
                for (int j = 0; j < i; j++)
                    sum += ratings[i].CompareTo(ratings[j]);
            }
        }

        return sum;
    }

    private static double CalculateFitnessSmooth(Parameters[] model, List<Dataset> datasets, double[] ratings) {
        double sum = 0;
        
        foreach (var dataset in datasets) {
            var elements = dataset.Elements;

            for (int i = 0; i < elements.Count; i++) {
                ratings[i] = Rate(elements[i].RatingData, model);
                
                for (int j = 0; j < i; j++)
                    sum += SoftSign(ratings[i] - ratings[j]);
            }
        }

        return sum;
    }

    private static double Rate(double[] ratingData, Parameters[] model) {
        double sum = 0d;

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            
            sum += parameters.Coefficient * Math.Pow(ratingData[i], parameters.Power);
        }
        
        return sum;
    }

    private static double SoftSign(double x) => 2d * x / (Math.Abs(x) + 1d);
    
    private static double DSoftSign(double x) => 2d / ((Math.Abs(x) + 1d) * (Math.Abs(x) + 1d));

    private static Parameters[] GetInitialModel(Superset superset) {
        var model = new Parameters[superset.MetricCount];
        var datasets = superset.Datasets;

        for (int i = 0; i < model.Length; i++) {
            int totalCount = 0;
            double sumX = 0d;
            double sumY = 0d;
            double sumXX = 0d;
            double sumXY = 0d;

            foreach (var dataset in datasets) {
                var elements = dataset.Elements;

                foreach (var element in elements) {
                    double rating = element.RatingData[i];
                    double difficulty = element.Difficulty;

                    sumX += rating;
                    sumY += difficulty;
                    sumXX += rating * rating;
                    sumXY += rating * difficulty;
                }

                totalCount += elements.Count;
            }
            
            double meanX = sumX / totalCount;
            double meanY = sumY / totalCount;

            model[i] = new Parameters((sumXY - totalCount * meanX * meanY) / (sumXX - totalCount * meanX * meanX), 1d);
        }
        
        NormalizeModel(model);

        return model;
    }

    private static ChartRatingModel ToChartRatingModel(Parameters[] model, double[] normalizationFactors, IReadOnlyList<Metric> metrics) {
        var modelParametersPerMetric = new Dictionary<string, ChartRatingModelParameters>();

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            
            modelParametersPerMetric.Add(metrics[i].Name, new ChartRatingModelParameters(normalizationFactors[i], parameters.Coefficient, parameters.Power));
        }

        return new ChartRatingModel(modelParametersPerMetric);
    }

    private class ModelFitnessPair : IComparable<ModelFitnessPair> {
        public Parameters[] Model;
        public double Fitness;

        public ModelFitnessPair(Parameters[] model, double fitness) {
            Model = model;
            Fitness = fitness;
        }

        public int CompareTo(ModelFitnessPair other) => other.Fitness.CompareTo(Fitness);
    }

    private class ThreadInfo {
        public Parameters[] Vector;
        public double[] Ratings;
        public List<Parameters[]> DRatings_DParams;

        public ThreadInfo(int metricCount, int ratingsSize, int dRatings_dParamsSize) {
            Vector = new Parameters[metricCount];
            Ratings = new double[ratingsSize];
            DRatings_DParams = new List<Parameters[]>(dRatings_dParamsSize);

            for (int i = 0; i < dRatings_dParamsSize; i++)
                DRatings_DParams.Add(new Parameters[metricCount]);
        }
    }
}