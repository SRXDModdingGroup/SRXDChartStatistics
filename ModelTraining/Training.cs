using System;
using System.Collections.Generic;
using ChartMetrics;
using Util;

namespace ModelTraining; 

public static class Training {
    private const int POOL_SIZE = 8;
    
    public static ChartRatingModel Train(IReadOnlyList<Dataset> datasets, IReadOnlyList<Metric> metrics, int iterations, double mutationAmount) {
        var best = new Parameters[metrics.Count];
        var random = new Random();

        for (int i = 0; i < metrics.Count; i++) {
            double max = 0d;

            foreach (var dataset in datasets) {
                foreach (var element in dataset.Elements) {
                    double value = element.RatingData[i];

                    if (value > max)
                        max = value;
                }
            }

            best[i] = new Parameters(max, 1d, 0d);
        }

        var pool = new List<Parameters[]>(POOL_SIZE);

        for (int i = 0; i < POOL_SIZE; i++) {
            var model = new Parameters[metrics.Count];
            
            Array.Copy(best, model, best.Length);
            pool.Add(model);
        }

        var ratings = new List<double>();
        int bestFitness = CalculateFitness(best, datasets, ratings);
        int totalPairCount = 0;

        foreach (var dataset in datasets) {
            int size = dataset.Elements.Count;

            totalPairCount += size * (size - 1) / 2;
        }

        for (int i = 1; i <= iterations; i++) {
            var bestInIteration = pool[0];
            int bestFitnessInIteration = 0;
            
            foreach (var model in pool) {
                Mutate(best, model, mutationAmount, random);

                int fitness = CalculateFitness(model, datasets, ratings);
                
                if (fitness <= bestFitnessInIteration)
                    continue;

                bestInIteration = model;
                bestFitnessInIteration = fitness;
            }

            if (bestFitnessInIteration >= bestFitness) {
                Array.Copy(bestInIteration, best, bestInIteration.Length);
                bestFitness = bestFitnessInIteration;
            }

            if (i % 1000 > 0)
                continue;
            
            Console.WriteLine($"Generation: {i}, Fitness: {(float) bestFitness / totalPairCount:0.0000}");

            for (int j = 0; j < metrics.Count; j++) {
                var parameters = best[j];
                
                Console.WriteLine($"{metrics[j].Name}: Coeff = {parameters.Coefficient}, Power = {parameters.Power}");
            }
            
            Console.WriteLine();
        }

        return ToChartRatingModel(best, metrics);
    }
    
    private static void Mutate(Parameters[] model, Parameters[] target, double amount, Random random) {
        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];

            target[i] = new Parameters(
                parameters.Maximum,
                MathU.Clamp(parameters.CoefficientParameter + amount * (2d * random.NextDouble() - 1d), 0d, 1d),
                MathU.Clamp(parameters.PowerParameter + amount * (2d * random.NextDouble() - 1d), -1d, 1d));
        }
    }
    
    private static int CalculateFitness(Parameters[] model, IReadOnlyList<Dataset> datasets, List<double> ratings) {
        int sum = 0;
        
        foreach (var dataset in datasets) {
            var elements = dataset.Elements;
            
            ratings.Clear();

            if (ratings.Capacity < elements.Count)
                ratings.Capacity = elements.Count;
        
            foreach (var element in elements)
                ratings.Add(Rate(element.RatingData, model));

            for (int i = 0; i < ratings.Count; i++) {
                for (int j = i + 1; j < ratings.Count; j++)
                    sum += ratings[j].CompareTo(ratings[i]);
            }
        }

        return sum;
    }

    private static double Rate(double[] ratingData, Parameters[] model) {
        double sum = 0d;

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            
            sum += Math.Pow(parameters.Coefficient * ratingData[i], parameters.Power);
        }
        
        return sum;
    }


    private static ChartRatingModel ToChartRatingModel(Parameters[] model, IReadOnlyList<Metric> metrics) {
        var modelParametersPerMetric = new Dictionary<string, ChartRatingModelParameters>();

        for (int i = 0; i < model.Length; i++) {
            var parameters = model[i];
            
            modelParametersPerMetric.Add(metrics[i].Name, new ChartRatingModelParameters(parameters.Coefficient, parameters.Power));
        }

        return new ChartRatingModel(modelParametersPerMetric);
    }
}