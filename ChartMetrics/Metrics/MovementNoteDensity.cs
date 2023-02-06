using ChartHelper.Types;

namespace ChartMetrics {
    public class MovementNoteDensity : DensityMetric {
        public override string Description => string.Empty;
        
        protected override bool CountFilter(Note note) => note.Type == NoteType.Tap
                                                          || note.Type == NoteType.Hold
                                                          || note.Type == NoteType.Match
                                                          || note.Type == NoteType.SpinLeft
                                                          || note.Type == NoteType.SpinRight
                                                          || note.Type == NoteType.Scratch
                                                          || note.Type == NoteType.HoldPoint
                                                          || note.Type == NoteType.HoldEnd
                                                          || note.Type == NoteType.Liftoff;
    }
}