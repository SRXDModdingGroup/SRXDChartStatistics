using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartMetrics {
    public abstract class PathMetric : Metric {
        protected abstract void AddPointsForPath(List<MetricPoint> points, ref double sum, IReadOnlyList<WheelPathPoint> path, int start, int end);

        protected abstract double GetValueForSpin(Note note);
        
        public override MetricResult Calculate(ChartData chartData) {
            var notes = chartData.Notes;
            var path = chartData.SimplifiedPath.Points;
            var points = new List<MetricPoint>();
            
            if (notes.Count == 0 || path.Count == 0)
                return MetricResult.Empty;
            
            int pathStartIndex = 0;
            int nextSpinIndex = 0;
            double sum = 0d;

            for (int i = 0; i < path.Count; i++) {
                if (i < path.Count - 1 && !path[i + 1].FirstInPath)
                    continue;
                
                AdvanceSpins(path[pathStartIndex].Time);
                AddPointsForPath(points, ref sum, path, pathStartIndex, i);
                pathStartIndex = i;
            }
            
            AdvanceSpins(double.MaxValue);

            return new MetricResult(points);

            void AdvanceSpins(double time) {
                for (; nextSpinIndex < notes.Count; nextSpinIndex++) {
                    var note = notes[nextSpinIndex];
                    var type = note.Type;

                    if (type != NoteType.SpinLeft && type != NoteType.SpinRight && type != NoteType.Scratch)
                        continue;

                    double noteTime = note.Time;

                    if (noteTime <= time) {
                        sum += GetValueForSpin(note);
                        points.Add(new MetricPoint(noteTime, sum));
                    }
                    else
                        break;
                }
            }
        }
    }
}