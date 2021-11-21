﻿using System.Collections.Generic;
using ChartHelper;

namespace ChartMetrics {
    internal class OverallNoteDensity : Metric {
        public override string Name => "OverallNoteDensity";

        public override string Description => "The density of notes of any type across the chart";

        internal override IList<Point> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            var points = new List<Point>();
            int lastIndex = 0;
            float lastTime = notes[0].Time;

            for (int i = 1; i < notes.Count; i++) {
                var note = notes[i];
                
                if (Util.AlmostEquals(note.Time, lastTime) || note.TypeRaw == NoteTypeRaw.HoldPoint || note.TypeRaw == NoteTypeRaw.BeatRelease)
                    continue;

                points.Add(new Point(lastTime, i - lastIndex));
                lastIndex = i;
                lastTime = note.Time;
            }

            points[points.Count - 1] = new Point(lastTime, notes.Count - lastIndex);
            points.Add(new Point(notes[notes.Count - 1].Time, 0f));

            return points;
        }
    }
}