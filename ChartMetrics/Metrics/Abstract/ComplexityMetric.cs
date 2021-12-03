using System.Collections.Generic;
using ChartHelper.Types;
using Util;

namespace ChartMetrics {
    public abstract class ComplexityMetric : Metric {
        private static readonly int SEQUENCE_LENGTH = 8;
        
        protected abstract byte Selector(NoteType type);
        
        internal override IList<Point> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            float lastTime = -1f;
            byte currentStack = 0;
            var stackTimes = new List<float>();
            var sequence = new List<char>();

            for (int i = 1; i < notes.Count; i++) {
                var note = notes[i];
                byte selector = Selector(note.Type);
                
                if (selector == 0)
                    continue;

                if (!MathU.AlmostEquals(note.Time, lastTime)) {
                    stackTimes.Add(note.Time);
                    sequence.Add((char)currentStack);
                    currentStack = 0;
                    lastTime = note.Time;
                }

                currentStack |= selector;
            }
            
            if (sequence.Count == 0) {
                return new List<Point> {
                    new Point(notes[0].Time, 0f),
                    new Point(notes[notes.Count - 1].Time, 0f)
                };
            }
            
            if (!MathU.AlmostEquals(lastTime, stackTimes[stackTimes.Count - 1])) {
                stackTimes.Add(lastTime);
                sequence.Add((char) currentStack);
            }
            
            var lut = new Dictionary<string, int>();
            float[] complexities = new float[sequence.Count];

            for (int i = 0; i < sequence.Count; i++)
                complexities[i] = float.PositiveInfinity;

            var points = new List<Point>();

            for (int i = 0; i < sequence.Count - SEQUENCE_LENGTH + 1; i++) {
                string current = string.Empty;
                int count = 0;
                
                for (int j = 0; j < 1 << 7; j++)
                    lut.Add(((char)j).ToString(), j);
                
                for (int j = i; j < i + SEQUENCE_LENGTH; j++) {
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

                float complexity = (float) count / SEQUENCE_LENGTH;

                for (int j = i; j < i + SEQUENCE_LENGTH; j++) {
                    if (complexity < complexities[j])
                        complexities[j] = complexity;
                }
                
                lut.Clear();
            }

            for (int i = 0; i < complexities.Length - 2; i++)
                points.Add(new Point(stackTimes[i], complexities[i]));
            
            points.Add(new Point(stackTimes[complexities.Length - 2], complexities[complexities.Length - 2] + complexities[complexities.Length - 1]));
            points.Add(new Point(stackTimes[stackTimes.Count - 1], 0f));

            return points;
        }
    }
}