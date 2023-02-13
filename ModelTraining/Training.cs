using System;
using System.Collections.Generic;
using ChartMetrics;

namespace ModelTraining; 

public static class Training {
    private const int POOL_SIZE = 8;

    public static double Rate(List<double> ratingData, TrainableModel model) {
        double sum = 0d;
        var parametersPerMetric = model.ParametersPerMetric;

        for (int i = 0; i < parametersPerMetric.Count; i++) {
            var parameters = parametersPerMetric[i];
            
            sum += Math.Pow(parameters.Coefficient * ratingData[i], parameters.Power);
        }
        
        return sum;
    }
    
    public static double CalculateFitness(TrainableModel model, List<Dataset> datasets) {
        int sum = 0;
        int totalCount = 0;
        
        foreach (var dataset in datasets) {
            var ratings = new List<double>(dataset.Elements.Count);
        
            foreach (var element in dataset.Elements)
                ratings.Add(Rate(element.RatingData, model));

            for (int i = 0; i < ratings.Count; i++) {
                for (int j = i + 1; j < ratings.Count; j++)
                    sum += ratings[j].CompareTo(ratings[i]);
            }

            totalCount += dataset.Elements.Count;
        }

        return (double) sum / totalCount;
    }

    public static ChartRatingModel Train(List<Dataset> datasets, List<Metric> metrics, int iterations, double mutationAmount) {
        var initialModelParametersPerMetric = new List<TrainingParameters>();
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
            
            initialModelParametersPerMetric.Add(new TrainingParameters(max, 1d, 0d));
        }

        var pool = new TrainableModel[POOL_SIZE];

        for (int i = 0; i < POOL_SIZE; i++)
            pool[i] = new TrainableModel(new List<TrainingParameters>(initialModelParametersPerMetric));
        
        var best = pool[0];

        for (int i = 0; i < iterations; i++) {
            double bestFitness = 0d;
            
            best = pool[0];
            
            foreach (var model in pool) {
                model.Mutate(mutationAmount, random);

                double fitness = CalculateFitness(model, datasets);
                
                if (fitness <= bestFitness)
                    continue;

                best = model;
                bestFitness = fitness;
            }
        }

        return best.ToChartRatingModel(metrics);
    }
}