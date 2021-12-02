using ChartHelper;

namespace ChartMetrics {
    public class MovementNoteDensity : DensityMetric {
        public override string Description { get; }

        protected override bool CandidateFilter(Note note) => note.Type == NoteType.Tap
                                                              || note.Type == NoteType.Hold
                                                              || note.Type == NoteType.Match
                                                              || note.Type == NoteType.SpinLeft
                                                              || note.Type == NoteType.SpinRight
                                                              || note.Type == NoteType.Scratch;

        protected override bool CountFilter(Note note) => CandidateFilter(note)
                                                          || note.Type == NoteType.HoldPoint
                                                          || note.Type == NoteType.HoldEnd
                                                          || note.Type == NoteType.Liftoff;
    }
}