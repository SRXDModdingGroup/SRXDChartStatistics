using ChartHelper.Types;

namespace ChartMetrics;

public class WheelPathPoint {
    public float Time { get; }
            
    public float LanePosition { get; }
            
    public float NetPosition { get; }
            
    public NoteColor CurrentColor { get; }
    
    public bool FirstInPath { get; }

    public WheelPathPoint(float time, float lanePosition, float netPosition, NoteColor currentColor, bool firstInPath) {
        Time = time;
        LanePosition = lanePosition;
        NetPosition = netPosition;
        CurrentColor = currentColor;
        FirstInPath = firstInPath;
    }
}