using System.Collections.Generic;
using ChartHelper;
using Util;

namespace ChartMetrics {
    public abstract class DensityMetric : Metric {
        protected abstract bool CandidateFilter(Note note);
        
        protected abstract bool CountFilter(Note note);
        
        internal override IList<Point> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            var points = new List<Point>();
            int notesFound = -1;
            float lastTime = 0f;

            for (int i = 1; i < notes.Count; i++) {
                var note = notes[i];
                
                if (!CountFilter(note))
                    continue;

                if (CandidateFilter(note) && (notesFound < 0 || !MathU.AlmostEquals(note.Time, lastTime))) {
                    if (notesFound >= 0)
                        points.Add(new Point(lastTime, notesFound));

                    notesFound = 0;
                    lastTime = note.Time;
                }

                notesFound++;
            }

            if (points.Count == 0) {
                return new List<Point> {
                    new Point(notes[0].Time, 0f),
                    new Point(notes[notes.Count - 1].Time, 0f)
                };
            }

            var lastPoint = points[points.Count - 1];

            points[points.Count - 1] = new Point(lastPoint.Time, lastPoint.Value + notesFound);
            points.Add(new Point(lastTime, 0f));

            return points;
        }
    }
}