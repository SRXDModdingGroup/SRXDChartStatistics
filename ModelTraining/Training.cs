using System;
using System.Collections.Generic;
using ChartMetrics;
using Util;

namespace ModelTraining; 

public static class Training {
    private const int POOL_SIZE = 32;
    private const double COEFFICIENT_MIN = 0.01d;
    private const double POWER_MIN = 0.25d;
    private const double POWER_MAX = 4d;

    public static double Rate(double[] ratingData, Parameters[] model) {
        double sum = 0d;

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            
            sum += parameters.Coefficient * Math.Pow(ratingData[i], parameters.Power);
        }
        
        return sum;
    }
    
    public static Parameters[] Train(IReadOnlyList<Dataset> datasets, IReadOnlyList<Metric> metrics, int iterations, double mutationAmount) {
        var random = new Random();
        var pool = new List<ModelFitnessPair>(POOL_SIZE);
        var ratings = new List<double>();

        for (int i = 0; i < POOL_SIZE; i++) {
            var model = new Parameters[metrics.Count];
            
            for (int j = 0; j < metrics.Count; j++)
                model[j] = new Parameters(MathU.Lerp(COEFFICIENT_MIN, 1d, random.NextDouble()), MathU.Lerp(POWER_MIN, POWER_MAX, random.NextDouble()));
            
            Normalize(model);
            pool.Add(new ModelFitnessPair(model, CalculateFitness(model, datasets, ratings)));
        }
        
        pool.Sort();

        int totalPairCount = 0;

        foreach (var dataset in datasets) {
            int size = dataset.Elements.Count;

            totalPairCount += size * (size - 1) / 2;
        }

        var vector = new Parameters[metrics.Count];

        for (int i = 1; i <= iterations; i++) {
            for (int j = 0; j < POOL_SIZE / 2; j++) {
                var source = pool[j];
                var target = pool[j + POOL_SIZE / 2];
                
                Mutate(source.Model, target.Model, mutationAmount, vector, random);
                Normalize(target.Model);
                target.Fitness = CalculateFitness(target.Model, datasets, ratings);
            }
            
            pool.Sort();
            
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                break;

            if (i % 1000 > 0)
                continue;

            var best = pool[0];
            
            Console.WriteLine($"Generation: {i}, Fitness: {best.Fitness / totalPairCount:0.0000}");

            for (int j = 0; j < metrics.Count; j++) {
                var parameters = best.Model[j];
                
                Console.WriteLine($"{metrics[j].Name}: Coeff = {parameters.Coefficient}, Power = {parameters.Power}");
            }
            
            Console.WriteLine();
        }

        return pool[0].Model;
    }
    
    private static void Mutate(Parameters[] model, Parameters[] target, double amount, Parameters[] vector, Random random) {
        double sum = 0d;

        for (int i = 0; i < vector.Length; i++) {
            double coefficient = 2d * random.NextDouble() - 1d;
            double power = 2d * random.NextDouble() - 1d;

            sum += Math.Abs(coefficient) + Math.Abs(power);
            vector[i] = new Parameters(coefficient, power);
        }

        double factor = random.NextDouble() * amount / sum;
        
        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            var delta = vector[i];

            target[i] = new Parameters(MathU.Clamp(parameters.Coefficient + factor * delta.Coefficient, COEFFICIENT_MIN, 1d),
                MathU.Clamp(parameters.Power + factor * delta.Power, POWER_MIN, POWER_MAX));
        }
    }

    private static void Normalize(Parameters[] model) {
        double sum = 0d;

        foreach (var parameters in model)
            sum += parameters.Coefficient;

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            
            model[i] = new Parameters(parameters.Coefficient / sum, parameters.Power);
        }
    }
    
    private static double CalculateFitness(Parameters[] model, IReadOnlyList<Dataset> datasets, List<double> ratings) {
        double sum = 0;
        
        foreach (var dataset in datasets) {
            var elements = dataset.Elements;
            
            ratings.Clear();

            if (ratings.Capacity < elements.Count)
                ratings.Capacity = elements.Count;
        
            foreach (var element in elements)
                ratings.Add(Rate(element.RatingData, model));

            for (int i = 0; i < ratings.Count; i++) {
                for (int j = i + 1; j < ratings.Count; j++) {
                    double diff = ratings[j] - ratings[i];

                    sum += MathU.Clamp(0.01d * diff + 0.99d * Math.Sign(diff), -1d, 1d);
                }
            }
        }

        return sum;
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
}