namespace ChartMetrics; 

public class ChartRatingModelParameters {
    public double NormalizationFactor { get; }
    
    public double Coefficient { get; }
    
    public double Power { get; }

    public ChartRatingModelParameters(double normalizationFactor, double coefficient, double power) {
        NormalizationFactor = normalizationFactor;
        Coefficient = coefficient;
        Power = power;
    }
}