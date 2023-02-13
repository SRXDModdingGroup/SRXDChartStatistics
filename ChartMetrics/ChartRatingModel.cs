using System.Collections.Generic;

namespace ChartMetrics; 

public class ChartRatingModel {
    public static ChartRatingModel Empty { get; } = new(new Dictionary<string, ChartRatingModelParameters>());
    
    public IReadOnlyDictionary<string, ChartRatingModelParameters> ParametersPerMetric { get; }

    public ChartRatingModel(IReadOnlyDictionary<string, ChartRatingModelParameters> parametersPerMetric) => ParametersPerMetric = parametersPerMetric;
}