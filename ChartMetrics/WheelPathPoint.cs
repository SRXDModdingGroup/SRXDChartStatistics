using ChartHelper.Types;

namespace ChartMetrics;

public class WheelPathPoint {
    public double Time { get; }
            
    public double LanePosition { get; }
            
    public double NetPosition { get; }
            
    public NoteColor CurrentColor { get; }
    
    public bool FirstInPath { get; }
    
    public bool IsHoldPoint { get; }

    public WheelPathPoint(double time, double lanePosition, double netPosition, NoteColor currentColor, bool firstInPath, bool isHoldPoint) {
        Time = time;
        LanePosition = lanePosition;
        NetPosition = netPosition;
        CurrentColor = currentColor;
        FirstInPath = firstInPath;
        IsHoldPoint = isHoldPoint;
    }
}