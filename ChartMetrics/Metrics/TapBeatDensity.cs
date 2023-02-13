using ChartHelper.Types;

namespace ChartMetrics; 

public class TapBeatDensity : DensityMetric {
    public override string Description => "The density of taps, beats, liftoffs, and hard beat releases across the chart";
        
    protected override bool CountFilter(Note note) => note.Type == NoteType.Tap
                                                      || note.Type == NoteType.Hold
                                                      || note.Type == NoteType.Beat;
}