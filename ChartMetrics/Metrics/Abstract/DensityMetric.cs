using System.Collections.Generic;
using ChartHelper.Types;
using Util;

namespace ChartMetrics {
    public abstract class DensityMetric : Metric {
        protected abstract bool CandidateFilter(Note note);
        
        protected abstract bool CountFilter(Note note);
        
        internal override IList<Point> Calculate(ChartProcessor processor) {
            var notes = processor.Notes;
            var points = new List<Point>();
            int notesInCurrentStack = 0;
            int notesToPop = 0;
            float stackTime = -1f;
            float stackTimeToPop = -1f;
            bool stackIsCandidate = false;

            for (int i = 0; i < notes.Count; i++) {
                var note = notes[i];
                
                if (!CountFilter(note))
                    continue;

                if (!MathU.AlmostEquals(note.Time, stackTime)) {
                    if (stackIsCandidate) {
                        if (notesToPop > 0) {
                            points.Add(new Point(stackTimeToPop, notesToPop));
                            notesToPop = 0;
                            stackTimeToPop = stackTime;
                        }
                        
                        stackIsCandidate = false;
                    }
                    
                    notesToPop += notesInCurrentStack;
                    notesInCurrentStack = 0;
                    stackTime = note.Time;
                    
                    if (stackTimeToPop < 0f)
                        stackTimeToPop = stackTime;
                }
                
                notesInCurrentStack++;
                
                if (CandidateFilter(note))
                    stackIsCandidate = true;
            }

            if (points.Count == 0) {
                return new List<Point> {
                    new Point(notes[0].Time, 0f),
                    new Point(notes[notes.Count - 1].Time, 0f)
                };
            }

            var lastPoint = points[points.Count - 1];

            points[points.Count - 1] = new Point(lastPoint.Time, lastPoint.Value + notesToPop + notesInCurrentStack);
            points.Add(new Point(stackTime, 0f));

            return points;
        }
    }
}