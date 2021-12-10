using System.Collections.Generic;
using ChartHelper.Types;
using Util;

namespace ChartMetrics {
    public class TemporalComplexity : Metric {
        public override string Description => string.Empty;
        internal override IList<Point> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            float lastTime = -1f;
            var stackTimes = new List<float>();

            for (int i = 1; i < notes.Count; i++) {
                var note = notes[i];
                
                if (note.Type != NoteType.Tap
                    && note.Type != NoteType.Hold
                    && note.Type != NoteType.Beat
                    && note.Type != NoteType.Liftoff
                    && note.Type != NoteType.BeatReleaseHard
                    && note.Type != NoteType.BeatReleaseSoft
                    || MathU.AlmostEquals(note.Time, lastTime))
                    continue;

                stackTimes.Add(note.Time);
                lastTime = note.Time;
            }

            for (int i = 0; i < stackTimes.Count - 16; i++) {
                for (int j = 0; j < i + 15; j++) {
                    
                }
            }

            return null;
        }
    }
}