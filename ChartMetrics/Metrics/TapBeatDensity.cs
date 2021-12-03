using ChartHelper.Types;

namespace ChartMetrics {
    internal class TapBeatDensity : DensityMetric {
        public override string Description => "The density of taps, beats, liftoffs, and hard beat releases across the chart";

        protected override bool CandidateFilter(Note note) => note.Type == NoteType.Tap
                                                              || note.Type == NoteType.Hold
                                                              || note.Type == NoteType.Beat;

        protected override bool CountFilter(Note note) => CandidateFilter(note)
                                                          || note.Type == NoteType.Liftoff
                                                          || note.Type == NoteType.BeatReleaseHard;
    }
}