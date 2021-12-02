using System.Collections.Generic;
using ChartHelper;

namespace ChartMetrics {
    public abstract class PathMetric : Metric {
        protected abstract float ValueForPath(IList<WheelPath.Point> exact, IList<WheelPath.Point> simplified);

        protected abstract float ValueForSpin(Note note);
        
        internal override IList<Point> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            var exactPaths = processor.GetExactPaths();
            var simplifiedPaths = processor.GetSimplifiedPaths();
            var points = new List<Point>();

            float lastPathEnd = 0f;

            for (int i = 0; i < simplifiedPaths.Count; i++) {
                var exact = exactPaths[i];
                
                if (exact.Count < 2)
                    continue;

                var simplified = simplifiedPaths[i];

                points.Add(new Point(exact[0].Time, ValueForPath(exact, simplified)));
                lastPathEnd = exact[exact.Count - 1].Time;
            }

            if (points.Count == 0) {
                return new List<Point> {
                    new Point(notes[0].Time, 0f),
                    new Point(notes[notes.Count - 1].Time, 0f)
                };
            }

            int index = 0;
            float lastSpinTime = 0f;

            foreach (var note in notes) {
                var type = note.Type;

                if (type != NoteType.SpinLeft && type != NoteType.SpinRight && type != NoteType.Scratch)
                    continue;

                float time = note.Time;

                while (index < points.Count && time > points[index].Time)
                    index++;

                if (index != 0 && points[index - 1].Value == 0f)
                    continue;
                
                points.Insert(index, new Point(time, ValueForSpin(note)));
                lastSpinTime = time;
            }

            if (lastPathEnd > lastSpinTime)
                points.Add(new Point(lastPathEnd, 0f));
            else {
                var lastPoint = points[points.Count - 1];
                var secondToLastPoint = points[points.Count - 2];

                points[points.Count - 2] = new Point(secondToLastPoint.Time, secondToLastPoint.Value + lastPoint.Value);
                points[points.Count - 1] = new Point(lastPoint.Time, 0f);
            }

            return points;
        }
    }
}