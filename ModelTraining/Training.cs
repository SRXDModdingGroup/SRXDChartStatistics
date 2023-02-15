using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChartMetrics;
using Util;

namespace ModelTraining; 

public static class Training {
    private const int POPULATION_SIZE = 128;
    private const int HALF_POPULATION_SIZE = POPULATION_SIZE / 2;
    private const double COEFFICIENT_MIN = 0.01d;
    private const double POWER_MIN = 0.25d;
    private const double POWER_MAX = 4d;
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

        return CalculateFitness(model, datasets, new double[largest]) / totalPairCount;
    }
    
    public static Parameters[] TrainGenetic(Superset superset, IReadOnlyList<Metric> metrics, int iterations, double mutationAmount) {
        var datasets = superset.Datasets;
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
            threadInfos[i] = new ThreadInfo(metrics.Count, largest, 0);

        for (int i = 0; i < POPULATION_SIZE; i++) {
            var model = new Parameters[metrics.Count];
            
            for (int j = 0; j < metrics.Count; j++)
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

            var best = population[0];
            
            Console.WriteLine($"Generation: {i}, Fitness: {CalculateFitness(best.Model, datasets, threadInfos[0].Ratings) / totalPairCount:0.0000}");

            for (int j = 0; j < metrics.Count; j++) {
                var parameters = best.Model[j];
                
                Console.WriteLine($"{metrics[j].Name}: Coeff = {parameters.Coefficient}, Power = {parameters.Power}");
            }
            
            Console.WriteLine();
        }

        return population[0].Model;
    }
    
    public static Parameters[] TrainGradient(Superset superset, IReadOnlyList<Metric> metrics, int iterations, double acceleration, double minTimestep, double maxTimestep, out bool quit) {
        var datasets = superset.Datasets;
        var random = new Random();
        var model = new Parameters[metrics.Count];
        double fitness = 0d;
        double timestep = maxTimestep;
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

        for (int i = 0; i < metrics.Count; i++)
            model[i] = new Parameters(MathU.Lerp(COEFFICIENT_MIN, 1d, random.NextDouble()), MathU.Lerp(POWER_MIN, POWER_MAX, random.NextDouble()));
        
        NormalizeModel(model);

        var vector = new Parameters[metrics.Count];
        var momentum = new Parameters[metrics.Count];

        for (int i = 1; i <= iterations; i++) {
            CalculateGradient(model, datasets, vector, ratings, dRatings_dParams);
            InterpolateVector(vector, momentum, momentum, Math.Exp(-timestep * acceleration));
            AddToModel(model, momentum, model, timestep / totalPairCount);

            double newFitness = CalculateFitness(model, datasets, ratings);

            if (newFitness > fitness)
                timestep = Math.Min(1.0001d * timestep, maxTimestep);
            else
                timestep = Math.Max(minTimestep, timestep / 1.0001d);

            fitness = newFitness;
            
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter) {
                quit = true;

                return model;
            }

            // if (i % 1000 > 0)
            //     continue;
            
            // Console.WriteLine($"Generation: {i}, Fitness: {fitness / totalPairCount:0.00000}, VectorAmount: {timestep:0.00000}");
            //
            // for (int j = 0; j < metrics.Count; j++) {
            //     var parameters = model[j];
            //     
            //     Console.WriteLine($"{metrics[j].Name}: Coeff = {parameters.Coefficient:0.00000}, Power = {parameters.Power:0.00000}");
            // }
            //
            // Console.WriteLine();
        }

        quit = false;
        
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

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];

            parameters = new Parameters(
                MathU.Clamp(parameters.Coefficient, COEFFICIENT_MIN, 1d),
                MathU.Clamp(parameters.Power, POWER_MIN, POWER_MAX));

            model[i] = parameters;
            sum += parameters.Coefficient;
        }

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            
            model[i] = new Parameters(parameters.Coefficient / sum, parameters.Power);
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

    private static void CalculateGradient(Parameters[] model, IReadOnlyList<Dataset> datasets, Parameters[] vector, double[] ratings, List<Parameters[]> dRatings_dParams) {
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
                    
                for (int j = i + 1; j < elements.Count; j++) {
                    double secondRating = ratings[j];
                    double dFitness_dDiff = DSoftSign(secondRating - firstRating);
                    var dSecondRating_dParams = dRatings_dParams[j];
                    
                    for (int k = 0; k < vector.Length; k++)
                        vector[k] += dFitness_dDiff * (dSecondRating_dParams[k] - dFirstRating_dParams[k]);
                }
            }
        }
    }
    
    private static double Rate(double[] ratingData, Parameters[] model) {
        double sum = 0d;

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            
            sum += parameters.Coefficient * Math.Pow(ratingData[i], parameters.Power);
        }
        
        return sum;
    }
    
    private static double CalculateFitness(Parameters[] model, IReadOnlyList<Dataset> datasets, double[] ratings) {
        double sum = 0;
        
        foreach (var dataset in datasets) {
            var elements = dataset.Elements;

            for (int i = 0; i < elements.Count; i++)
                ratings[i] = Rate(elements[i].RatingData, model);

            for (int i = 0; i < elements.Count; i++) {
                for (int j = i + 1; j < elements.Count; j++)
                    sum += ratings[j].CompareTo(ratings[i]);
            }
        }

        return sum;
    }
    
    private static double CalculateFitnessSmooth(Parameters[] model, IReadOnlyList<Dataset> datasets, double[] ratings) {
        double sum = 0;
        
        foreach (var dataset in datasets) {
            var elements = dataset.Elements;

            for (int i = 0; i < elements.Count; i++)
                ratings[i] = Rate(elements[i].RatingData, model);

            for (int i = 0; i < elements.Count; i++) {
                for (int j = i + 1; j < elements.Count; j++)
                    sum += SoftSign(ratings[j] - ratings[i]);
            }
        }

        return sum;
    }

    private static double SoftSign(double x) => 2d * x / (Math.Abs(x) + 1d);
    
    private static double DSoftSign(double x) => 2d / ((Math.Abs(x) + 1d) * (Math.Abs(x) + 1d));

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