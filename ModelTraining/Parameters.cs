namespace ModelTraining; 

public readonly struct Parameters {
    public double Coefficient { get; }
    
    public double Power { get; }

    public Parameters(double coefficient, double power) {
        Coefficient = coefficient;
        Power = power;
    }

    public static Parameters operator +(Parameters a, Parameters b) => new(a.Coefficient + b.Coefficient, a.Power + b.Power);
    
    public static Parameters operator -(Parameters a, Parameters b) => new(a.Coefficient - b.Coefficient, a.Power - b.Power);

    public static Parameters operator *(Parameters a, double b) => new(b * a.Coefficient, b * a.Power);
    public static Parameters operator *(double a, Parameters b) => new(a * b.Coefficient, a * b.Power);

    public static Parameters operator /(Parameters a, double b) => new(a.Coefficient / b, a.Power / b);
}