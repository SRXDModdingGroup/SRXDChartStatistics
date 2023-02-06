namespace ChartMetrics;

public class MetricPoint {
    public double Time { get; }
    
    public double Value { get; }

    public MetricPoint(double time, double value) {
        Time = time;
        Value = value;
    }
}