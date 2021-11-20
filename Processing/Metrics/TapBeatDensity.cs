using System.Collections.Generic;
using ChartHelper;

namespace ChartStatistics {
    public class TapBeatDensity : Metric {
        public override string Name => "TapBeatDensity";

        public override string Description => "The density of taps, beats, liftoffs, and hard beat releases across the chart";

        public override IList<Point> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            var points = new List<Point>();
            int tapBeatsFound = -1;
            float lastTime = 0f;

            for (int i = 1; i < notes.Count; i++) {
                var note = notes[i];
                var type = note.Type;
                
                if (type != NoteType.Tap && type != NoteType.Hold && type != NoteType.Beat
                    && type != NoteType.Liftoff && type != NoteType.BeatReleaseHard)
                    continue;

                if (tapBeatsFound < 0 || !Util.AlmostEquals(note.Time, lastTime)) {
                    if (tapBeatsFound >= 0)
                        points.Add(new Point(lastTime, tapBeatsFound));

                    tapBeatsFound = 0;
                    lastTime = note.Time;
                }

                tapBeatsFound++;
            }

            var lastPoint = points[points.Count - 1];

            points[points.Count - 1] = new Point(lastPoint.Time, lastPoint.Value + tapBeatsFound);
            points.Add(new Point(lastTime, 0f));

            return points;
        }
    }
}