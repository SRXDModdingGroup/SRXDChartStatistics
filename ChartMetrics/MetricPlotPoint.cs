namespace ChartMetrics; 

public class MetricPlotPoint {
    public double Time { get; }
    
    public double Value { get; }

    public MetricPlotPoint(double time, double value) {
        Time = time;
        Value = value;
    }
}