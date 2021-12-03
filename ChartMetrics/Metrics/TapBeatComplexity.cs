using ChartHelper.Types;

namespace ChartMetrics {
    public class TapBeatComplexity : ComplexityMetric {
        public override string Description => "The complexity of the sequence of taps, beats, liftoffs, and hard beat releases";
        
        protected override byte Selector(NoteType type) {
            switch (type) {
                case NoteType.Tap:
                case NoteType.Hold:
                case NoteType.Liftoff:
                    return 1;
                case NoteType.Beat:
                case NoteType.BeatReleaseHard:
                    return 1 << 1;
            }

            return 0;
        }
    }
}