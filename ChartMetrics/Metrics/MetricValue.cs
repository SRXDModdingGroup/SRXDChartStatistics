namespace ChartMetrics; 

public class MetricValue {
    public double Time { get; }
    
    public double Value { get; }

    public MetricValue(double time, double value) {
        Time = time;
        Value = value;
    }
}