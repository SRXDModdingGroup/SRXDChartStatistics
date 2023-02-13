using System;
using System.Collections.Generic;
using ChartMetrics;
using Util;

namespace ModelTraining; 

public class TrainableModel {
    public IReadOnlyList<TrainingParameters> ParametersPerMetric => parametersPerMetric;

    private List<TrainingParameters> parametersPerMetric;

    public TrainableModel(List<TrainingParameters> parametersPerMetric) => this.parametersPerMetric = parametersPerMetric;

    public void Mutate(double amount, Random random) {
        foreach (var parameters in parametersPerMetric) {
            parameters.CoefficientParameter = MathU.Clamp(parameters.Coefficient + amount * (2d * random.NextDouble() - 1d), 0d, 1d);
            parameters.PowerParameter = MathU.Clamp(parameters.Power + amount * (2d * random.NextDouble() - 1d), -1d, 1d);
            parameters.Update();
        }
    }

    public void CopyTo(TrainableModel target) {
        for (int i = 0; i < parametersPerMetric.Count; i++)
            parametersPerMetric[i].CopyTo(target.parametersPerMetric[i]);
    }

    public ChartRatingModel ToChartRatingModel(List<Metric> metrics) {
        var modelParametersPerMetric = new Dictionary<string, ChartRatingModelParameters>();

        for (int i = 0; i < parametersPerMetric.Count; i++) {
            var parameters = parametersPerMetric[i];
            
            modelParametersPerMetric.Add(metrics[i].Name, new ChartRatingModelParameters(parameters.Coefficient, parameters.Power));
        }

        return new ChartRatingModel(modelParametersPerMetric);
    }
}