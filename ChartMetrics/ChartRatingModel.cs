using System.Collections.Generic;

namespace ChartMetrics; 

public class ChartRatingModel {
    public static ChartRatingModel Empty { get; }

    static ChartRatingModel() {
        var parametersPerMetric = new SortedDictionary<string, ChartRatingModelParameters>();

        foreach (var metric in Metric.GetAllMetrics()) {
            if (metric is PointValue)
                continue;
            
            parametersPerMetric.Add(metric.Name, new ChartRatingModelParameters(0d, 1d));
        }

        Empty = new ChartRatingModel(parametersPerMetric);
    }
    
    public IReadOnlyDictionary<string, ChartRatingModelParameters> ParametersPerMetric => parametersPerMetric;

    private SortedDictionary<string, ChartRatingModelParameters> parametersPerMetric;

    public ChartRatingModel(SortedDictionary<string, ChartRatingModelParameters> parametersPerMetric) => this.parametersPerMetric = parametersPerMetric;
}