using ChartHelper.Types;

namespace ChartMetrics {
    public class SequenceComplexity : ComplexityMetric {
        public override string Description => "The complexity of the sequence of all notes except matches and hold points";
        
        protected override byte Selector(NoteType type) {
            switch (type) {
                case NoteType.Tap:
                case NoteType.Hold:
                    return 1;
                case NoteType.Beat:
                    return 1 << 1;
                case NoteType.SpinLeft:
                    return 1 << 2;
                case NoteType.SpinRight:
                    return 1 << 3;
                case NoteType.Scratch:
                    return 1 << 4;
                case NoteType.Liftoff:
                    return 1 << 5;
                case NoteType.BeatReleaseHard:
                    return 1 << 6;
            }

            return 0;
        }
    }
}