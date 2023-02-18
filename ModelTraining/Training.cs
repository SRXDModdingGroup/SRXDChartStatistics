using System;
using System.Collections.Generic;
using System.Threading;
using ChartMetrics;
using Util;

namespace ModelTraining; 

public static class Training {
    private const int POPULATION_SIZE = 128;

    public static double Rate(double[] model, DataElement element) => Rate(model, element.RatingData);

    public static double CalculateCost(double[] model, Superset superset) {
        int totalSize = 0;

        foreach (var dataset in superset.Datasets)
            totalSize += dataset.Elements.Count;

        return Math.Sqrt(CalculateCost(model, superset.Datasets) / totalSize);
    }

    public static double[] TrainGenetic(Superset superset, int iterations, double mutationAmount, CancellationToken ct) {
        var datasets = superset.Datasets;
        int metricCount = superset.MetricCount;
        var random = new Random();
        var population = new ModelFitnessPair[POPULATION_SIZE];
        int largest = 0;
        
        foreach (var dataset in datasets) {
            int count = dataset.Elements.Count;

            if (count > largest)
                largest = count;
        }

        double[] initialModel = GetInitialModel(superset);
        double initialFitness = CalculateCost(initialModel, datasets);

        for (int i = 0; i < POPULATION_SIZE; i++) {
            double[] model = new double[metricCount];
            
            initialModel.CopyTo(model, 0);
            population[i] = new ModelFitnessPair(model, initialFitness);
        }
        
        Array.Sort(population);

        double[] vector = new double[metricCount];

        for (int i = 1; i <= iterations && !ct.IsCancellationRequested; i++) {
            for (int k = 0; k < POPULATION_SIZE / 2; k++) {
                var source = population[k];
                var target = population[k + POPULATION_SIZE / 2];

                RandomVector(vector, random);
                AddToModel(source.Model, vector, target.Model, mutationAmount);
                target.Cost = CalculateCost(target.Model, datasets);
            }
            
            Array.Sort(population);
        }

        return population[0].Model;
    }
    
    public static double[] TrainGradient(Superset superset, int iterations, double startTimestep, double endTimestep, CancellationToken ct) {
        var datasets = superset.Datasets;
        int metricCount = superset.MetricCount;
        int totalSize = 0;
        
        foreach (var dataset in datasets)
            totalSize += dataset.Elements.Count;

        double[] gradient = new double[metricCount];
        double[] model = GetInitialModel(superset);
        
        for (int i = 1; i <= iterations && !ct.IsCancellationRequested; i++) {
            CalculateGradient(model, datasets, gradient);
            AddToModel(model, gradient, model, -MathU.Remap(i, 1d, iterations, startTimestep, endTimestep) / totalSize);
        }
        
        return model;
    }

    private static void AddToModel(double[] source, double[] vector, double[] target, double amount) {
        for (int i = 0; i < source.Length; i++)
            target[i] = source[i] + amount * vector[i];

        NormalizeModel(target);
    }

    private static void NormalizeModel(double[] model) {
        for (int i = 0; i < model.Length; i++)
            model[i] = MathU.Clamp(model[i], 0d, 1d);
    }

    private static void NormalizeVector(double[] vector) {
        double sum = 0d;

        foreach (double coefficient in vector)
            sum += coefficient;

        sum = Math.Sqrt(sum);

        for (int i = 0; i < vector.Length; i++)
            vector[i] /= sum;
    }

    private static void RandomVector(double[] vector, Random random) {
        for (int i = 0; i < vector.Length; i++)
            vector[i] = 2d * random.NextDouble() - 1d;

        NormalizeVector(vector);
    }

    private static void CalculateGradient(double[] model, List<Dataset> datasets, double[] gradient) {
        for (int i = 0; i < gradient.Length; i++)
            gradient[i] = 0d;

        foreach (var dataset in datasets) {
            var elements = dataset.Elements;

            foreach (var element in elements) {
                double difference = Rate(model, element.RatingData) - element.Difficulty;
                double[] ratingData = element.RatingData;

                for (int i = 0; i < gradient.Length; i++)
                    gradient[i] += difference * ratingData[i];
            }
        }
    }

    private static double CalculateCost(double[] model, List<Dataset> datasets) {
        double cost = 0;
        
        foreach (var dataset in datasets) {
            var elements = dataset.Elements;

            foreach (var element in elements) {
                double diff = Rate(model, element.RatingData) - element.Difficulty;

                cost += diff * diff;
            }
        }

        return cost;
    }

    private static double Rate(double[] model, double[] ratingData) {
        double sum = 0d;

        for (int i = 0; i < model.Length; i++)
            sum += model[i] * ratingData[i];

        return sum;
    }

    private static double[] GetInitialModel(Superset superset) {
        double[] model = new double[superset.MetricCount];
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

            model[i] = (sumXY - totalCount * meanX * meanY) / (sumXX - totalCount * meanX * meanX);
        }
        
        NormalizeModel(model);

        return model;
    }

    private static ChartRatingModel ToChartRatingModel(double[] model, double[] normalizationFactors, IReadOnlyList<Metric> metrics) {
        var modelParametersPerMetric = new Dictionary<string, ChartRatingModelParameters>();

        for (int i = 0; i < model.Length; i++)
            modelParametersPerMetric.Add(metrics[i].Name, new ChartRatingModelParameters(normalizationFactors[i], model[i]));

        return new ChartRatingModel(modelParametersPerMetric);
    }

    private class ModelFitnessPair : IComparable<ModelFitnessPair> {
        public double[] Model;
        public double Cost;

        public ModelFitnessPair(double[] model, double cost) {
            Model = model;
            Cost = cost;
        }

        public int CompareTo(ModelFitnessPair other) => Cost.CompareTo(other.Cost);
    }
}