using System;

namespace ModelTraining; 

public class TrainingParameters {
    public double Maximum { get; set; }
    
    public double CoefficientParameter { get; set; }
    
    public double PowerParameter { get; set; }
    
    public double Coefficient { get; private set; }
    
    public double Power { get; private set; }

    public TrainingParameters(double maximum, double coefficient, double power) {
        Maximum = maximum;
        Coefficient = coefficient;
        Power = power;
        Update();
    }

    public void Update() {
        Coefficient = CoefficientParameter / Maximum;
        Power = Math.Exp(PowerParameter);
    }

    public void CopyTo(TrainingParameters other) {
        other.Maximum = Maximum;
        other.CoefficientParameter = CoefficientParameter;
        other.PowerParameter = PowerParameter;
        other.Coefficient = Coefficient;
        other.Power = Power;
    }
}