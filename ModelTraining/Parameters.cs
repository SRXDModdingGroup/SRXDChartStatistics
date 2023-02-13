using System;

namespace ModelTraining; 

public readonly struct Parameters {
    public double Maximum { get; }
    
    public double CoefficientParameter { get;}
    
    public double PowerParameter { get;}
    
    public double Coefficient { get; }
    
    public double Power { get; }

    public Parameters(double maximum, double coefficientParameter, double powerParameter) {
        Maximum = maximum;
        CoefficientParameter = coefficientParameter;
        PowerParameter = powerParameter;
        Coefficient = CoefficientParameter / Maximum;
        Power = Math.Exp(PowerParameter);
    }
}