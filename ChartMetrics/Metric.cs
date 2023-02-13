namespace ChartMetrics; 

public abstract class Metric {
    public string Name => GetType().Name;
        
    public abstract string Description { get; }

    public abstract MetricResult Calculate(ChartData chartData);
}