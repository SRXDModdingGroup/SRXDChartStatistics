using ChartHelper;

namespace ChartMetrics {
    public class SpinDensity : DensityMetric {
        public override string Description { get; }

        protected override bool CandidateFilter(Note note) => note.Type == NoteType.SpinLeft
                                                              || note.Type == NoteType.SpinRight
                                                              || note.Type == NoteType.Scratch;

        protected override bool CountFilter(Note note) => CandidateFilter(note);
    }
}