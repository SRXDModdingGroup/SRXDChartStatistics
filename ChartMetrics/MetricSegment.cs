namespace ChartMetrics; 

public class MetricSegment {
    public double StartTime { get; }
    
    public double EndTime { get; }
    
    public double Duration { get; }
    
    public double Total { get; }
    
    public double Average { get; }

    public MetricSegment(double startTime, double endTime, double total) {
        StartTime = startTime;
        EndTime = endTime;
        Duration = endTime - startTime;
        Total = total;
        Average = total / Duration;
    }
}