namespace ModelTraining; 

public readonly struct Parameters {
    public double Coefficient { get; }
    
    public double Power { get; }

    public Parameters(double coefficient, double power) {
        Coefficient = coefficient;
        Power = power;
    }
}