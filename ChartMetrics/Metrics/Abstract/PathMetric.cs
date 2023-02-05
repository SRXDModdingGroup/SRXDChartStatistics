using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartMetrics {
    public abstract class PathMetric : Metric {
        protected abstract float ValueForPath(IList<WheelPathPoint> exact, IList<WheelPathPoint> simplified);

        protected abstract float ValueForSpin(Note note);
        
        internal override IList<MetricPoint> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            var exactPath = processor.ExactPath.Points;
            var simplifiedPath = processor.SimplifiedPath.Points;
            var points = new List<MetricPoint>();

            float lastPathEnd = 0f;

            for (int i = 0; i < simplifiedPath.Count; i++) {
                var exact = exactPath[i];
                
                if (exact.Count < 2)
                    continue;

                var simplified = simplifiedPath[i];

                points.Add(new MetricPoint(exact[0].Time, ValueForPath(exact, simplified)));
                lastPathEnd = exact[exact.Count - 1].Time;
            }

            if (points.Count == 0) {
                return new List<MetricPoint> {
                    new MetricPoint(notes[0].Time, 0f),
                    new MetricPoint(notes[notes.Count - 1].Time, 0f)
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
                
                points.Insert(index, new MetricPoint(time, ValueForSpin(note)));
                lastSpinTime = time;
            }

            if (lastPathEnd > lastSpinTime)
                points.Add(new MetricPoint(lastPathEnd, 0f));
            else {
                var lastPoint = points[points.Count - 1];
                var secondToLastPoint = points[points.Count - 2];

                points[points.Count - 2] = new MetricPoint(secondToLastPoint.Time, secondToLastPoint.Value + lastPoint.Value);
                points[points.Count - 1] = new MetricPoint(lastPoint.Time, 0f);
            }

            return points;
        }
    }
}