using ChartHelper;

namespace ChartMetrics {
    internal class OverallNoteDensity : DensityMetric {
        public override string Description => "The density of notes of any type across the chart";

        protected override bool CandidateFilter(Note note) => note.TypeRaw != NoteTypeRaw.HoldPoint
                                                              && note.TypeRaw != NoteTypeRaw.BeatRelease;

        protected override bool CountFilter(Note note) => true;
    }
}