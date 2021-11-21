﻿using System;
using System.Collections.Generic;
using ChartHelper;

namespace ChartMetrics {
    internal class Drift : Metric {
        public override string Name => "Drift";

        public override string Description => "The distance that a pattern strays from a centered position";

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
                    var start = path[i];
                    var end = path[i + 1];
                    
                    sum += Math.Abs(0.5f * (end.NetPosition + start.NetPosition)) * (end.Time - start.Time);
                }
                
                points.Add(new Point(path[0].Time, sum));
                lastPathEnd = path[path.Count - 1].Time;
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