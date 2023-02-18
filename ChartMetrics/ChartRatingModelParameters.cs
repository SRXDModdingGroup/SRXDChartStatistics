namespace ChartMetrics; 

public class ChartRatingModelParameters {
    public double NormalizationFactor { get; }
    
    public double Coefficient { get; }

    public ChartRatingModelParameters(double normalizationFactor, double coefficient) {
        NormalizationFactor = normalizationFactor;
        Coefficient = coefficient;
    }
}