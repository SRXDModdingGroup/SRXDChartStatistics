using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartMetrics; 

public class ChartData {
    public static ChartData Empty { get; } = new(new List<Note>(), WheelPath.Empty, WheelPath.Empty);
    
    public IReadOnlyList<Note> Notes { get; }
    
    public WheelPath ExactPath { get; }
    
    public WheelPath SimplifiedPath { get; }

    private ChartData(IReadOnlyList<Note> notes, WheelPath exactPath, WheelPath simplifiedPath) {
        Notes = notes;
        ExactPath = exactPath;
        SimplifiedPath = simplifiedPath;
    }

    public static ChartData Create(IReadOnlyList<Note> notes) {
        var exactPath = WheelPath.Create(notes);
        var simplifiedPath = exactPath.Simplify();
        
        return new ChartData(new List<Note>(notes), exactPath, simplifiedPath);
    }
}