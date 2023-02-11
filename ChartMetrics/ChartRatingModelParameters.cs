namespace ChartMetrics; 

public class ChartRatingModelParameters {
    public double Coefficient { get; }
    
    public double Power { get; }

    public ChartRatingModelParameters(double coefficient, double power) {
        Coefficient = coefficient;
        Power = power;
    }
}