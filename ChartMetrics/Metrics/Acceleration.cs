using System;
using System.Collections.Generic;
using ChartHelper;

namespace ChartMetrics {
    internal class Acceleration : Metric {
        public override string Name => "Acceleration";

        public override string Description => "The total change in speed / direction over the course of a pattern";

        internal override IList<Point> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            var paths = processor.GetSimplifiedPaths();
            var points = new List<Point>();

            float lastPathEnd = 0f;
            float lastSpinTime = 0f;

            foreach (var path in paths) {
                if (path.Count <= 1)
                    continue;
                
                float sum = 0f;

                for (int i = 0; i < path.Count - 2; i++) {
                    var start = path[i];
                    var mid = path[i + 1];
                    var end = path[i + 2];
                    
                    sum += Math.Abs((end.NetPosition - mid.NetPosition) / (end.Time - mid.Time) - (mid.NetPosition - start.NetPosition) / (mid.Time - start.Time));
                }

                var last = path[path.Count - 1];
                var secondToLast = path[path.Count - 2];

                sum += Math.Abs((last.NetPosition - secondToLast.NetPosition) / (last.Time - secondToLast.Time));
                points.Add(new Point(path[0].Time, sum));
                lastPathEnd = path[path.Count - 1].Time;
            }
            
            if (points.Count == 0) {
                return new List<Point> {
                    new Point(notes[0].Time, 0f),
                    new Point(notes[notes.Count - 1].Time, 0f)
                };
            }

            int index = 0;

            foreach (var note in notes) {
                var type = note.Type;

                if (type != NoteType.SpinLeft && type != NoteType.SpinRight && type != NoteType.Scratch)
                    continue;

                float time = note.Time;

                while (index < points.Count && time > points[index].Time)
                    index++;

                if (index != 0 && points[index - 1].Value == 0f)
                    continue;
                
                points.Insert(index, new Point(time, 0f));
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