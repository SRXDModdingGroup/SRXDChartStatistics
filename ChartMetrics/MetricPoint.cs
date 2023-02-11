namespace ChartMetrics;

public class MetricPoint {
    public double Time { get; }
    
    public double Value { get; }
    
    public bool Interpolate { get; }

    public MetricPoint(double time, double value, bool interpolate) {
        Time = time;
        Value = value;
        Interpolate = interpolate;
    }
}