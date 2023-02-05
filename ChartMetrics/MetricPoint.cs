namespace ChartMetrics;

public readonly struct MetricPoint {
    public float Time { get; }
    
    public float Value { get; }

    public MetricPoint(float time, float value) {
        Time = time;
        Value = value;
    }
}