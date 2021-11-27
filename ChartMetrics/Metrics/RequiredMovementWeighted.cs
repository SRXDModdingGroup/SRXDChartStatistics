using System;
using System.Collections.Generic;
using ChartHelper;

namespace ChartMetrics {
    internal class RequiredMovementWeighted : Metric {
        public override string Name => "RequiredMovementWeighted";

        public override string Description => "The minimum amount of movement required to hit every positional note in a pattern, with extra weight given to faster movements";

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

                for (int i = 0; i < path.Count - 1; i++) {
                    var first = path[i];
                    var second = path[i + 1];
                    float distance = Math.Abs(path[i + 1].NetPosition - path[i].NetPosition);
                    
                    sum += distance * distance / (second.Time - first.Time);
                }

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
                
                points.Insert(index, new Point(time, 80f));
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