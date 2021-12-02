using System;
using System.Collections.Generic;
using ChartHelper;
using Util;

namespace ChartMetrics {
    public class SequenceComplexity : Metric {
        public override string Description { get; }
        
        internal override IList<Point> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            float lastTime = -1f;
            byte currentStack = 0;
            var stackTimes = new List<float>();
            var sequence = new List<char>();

            for (int i = 1; i < notes.Count; i++) {
                var note = notes[i];
                var type = note.Type;
                
                if (type != NoteType.Tap
                    && type != NoteType.Hold
                    && type != NoteType.Beat
                    && type != NoteType.SpinLeft
                    && type != NoteType.SpinRight
                    && type != NoteType.Scratch
                    && type != NoteType.Liftoff
                    && type != NoteType.BeatReleaseHard)
                    continue;

                if (!MathU.AlmostEquals(note.Time, lastTime)) {
                    stackTimes.Add(note.Time);
                    sequence.Add((char)currentStack);
                    currentStack = 0;
                    lastTime = note.Time;
                }

                switch (type) {
                    case NoteType.Tap:
                    case NoteType.Hold:
                        currentStack |= 1;
                        break;
                    case NoteType.Beat:
                        currentStack |= 1 << 1;
                        break;
                    case NoteType.SpinLeft:
                        currentStack |= 1 << 2;
                        break;
                    case NoteType.SpinRight:
                        currentStack |= 1 << 3;
                        break;
                    case NoteType.Scratch:
                        currentStack |= 1 << 4;
                        break;
                    case NoteType.Liftoff:
                        currentStack |= 1 << 5;
                        break;
                    case NoteType.BeatReleaseHard:
                        currentStack |= 1 << 6;
                        break;
                }
            }
            
            if (sequence.Count == 0) {
                return new List<Point> {
                    new Point(notes[0].Time, 0f),
                    new Point(notes[notes.Count - 1].Time, 0f)
                };
            }

            stackTimes.Add(lastTime);
            sequence.Add((char)currentStack);
            
            var lut = new Dictionary<string, int>();
            var complexities = new int[sequence.Count];
            
            var points = new List<Point>();

            for (int i = 0; i < sequence.Count - 1; i++) {
                string current = string.Empty;
                int count = 0;
                
                for (int j = 0; j < 1 << 7; j++)
                    lut.Add(((char)j).ToString(), j);
                
                for (int j = i; j < i + 16 && j < sequence.Count; j++) {
                    char stack = sequence[j];
                    string next = current + stack;

                    if (lut.ContainsKey(next))
                        current = next;
                    else {
                        lut.Add(next, lut.Count);
                        current = stack.ToString();
                        count++;
                    }
                }
                
                lut.Clear();
                points.Add(new Point(stackTimes[i], count / (stackTimes[Math.Min(i + 16, stackTimes.Count - 1)] - stackTimes[i])));
            }

            return points;
        }
    }
}