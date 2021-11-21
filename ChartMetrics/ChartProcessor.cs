using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ChartHelper;

namespace ChartMetrics {
    public class ChartProcessor {
        private static readonly float MAX_MERGE_RATIO = 1.01f;
        private static readonly float MIN_PHRASE_LENGTH = 1f;
        private static readonly float MAX_PHRASE_LENGTH = 5f;
        private static readonly Metric[] METRICS = {
            new OverallNoteDensity(),
            new TapBeatDensity(),
            new RequiredMovement(),
            new RequiredMovementWeighted(),
            new Acceleration(),
            new Drift(),
            new DriftWeighted()
        };
        private static readonly Dictionary<string, Metric> METRICS_DICT = METRICS.ToDictionary(metric => metric.Name.ToLower(), metric => metric);

        public static ReadOnlyCollection<Metric> Metrics { get; } = new ReadOnlyCollection<Metric>(METRICS);

        public readonly struct Sample : IComparable<Sample> {
            public float Time { get; }
            
            public float Value { get; }
            
            public float Length { get; }

            internal Sample(float time, float value, float length) {
                Time = time;
                Value = value;
                Length = length;
            }

            public int CompareTo(Sample other) => Value.CompareTo(other.Value);
        }

        public readonly struct Result {
            public string MetricName { get; }
            
            public ReadOnlyCollection<Sample> Samples { get; }
            
            public ReadOnlyCollection<Sample> Sorted { get; }

            private readonly float[] cumulativeLengths;

            internal Result(string metricName, IList<Sample> samples) {
                MetricName = metricName;
                Samples = new ReadOnlyCollection<Sample>(samples);
                
                var sorted = new Sample[samples.Count];

                for (int i = 0; i < samples.Count; i++)
                    sorted[i] = samples[i];

                Array.Sort(sorted);

                Sorted = new ReadOnlyCollection<Sample>(sorted);
                cumulativeLengths = new float[Samples.Count];
                
                float totalLength = 0f;

                for (int i = 0; i < Sorted.Count; i++) {
                    var sample = Sorted[i];
                    
                    totalLength += sample.Length;
                    cumulativeLengths[i] = totalLength;
                }
            }

            public float GetQuantile(float quantile) {
                if (Samples.Count == 0)
                    return 0f;

                if (Samples.Count == 1)
                    return Samples[0].Value;

                float totalLength = cumulativeLengths[cumulativeLengths.Length - 1];
                float targetTotal = quantile * totalLength;
                var first = Sorted[0];

                if (targetTotal < 0.5f * first.Length)
                    return Sorted[0].Value;

                var last = Sorted[Sorted.Count - 1];

                if (targetTotal > totalLength - 0.5f * last.Length)
                    return last.Value;
                
                for (int i = 0; i < Sorted.Count - 1; i++) {
                    float end = cumulativeLengths[i] + 0.5f * Sorted[i + 1].Length;
                    
                    if (end < targetTotal)
                        continue;

                    float start = cumulativeLengths[i] - 0.5f * Sorted[i].Length;
                
                    return Util.Remap(targetTotal, start, end, Sorted[i].Value, Sorted[i + 1].Value);
                }

                return Sorted[Sorted.Count - 1].Value;
            }
        }
        
        public ReadOnlyCollection<Note> Notes { get; }

        private Dictionary<string, Result> results;
        private ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>> pathsExact;
        private ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>> pathsSimplified;

        public ChartProcessor(ReadOnlyCollection<Note> notes) {
            Notes = notes;
            results = new Dictionary<string, Result>();
        }

        public static bool TryLoadChart(string path, out ChartProcessor processor, Difficulty difficulty = Difficulty.XD) {
            if (!ChartData.TryCreateFromFile(path, out var chartData, Difficulty.XD)) {
                processor = null;
                
                return false;
            }

            processor = new ChartProcessor(chartData.TrackData[difficulty].Notes);

            return true;
        }

        public bool TryGetMetric(string name, out Result result) {
            name = name.ToLowerInvariant();
            
            if (!METRICS_DICT.TryGetValue(name, out var metric)) {
                result = new Result();
                
                return false;
            }

            if (results.TryGetValue(name, out result))
                return true;

            result = CalculateMetric(metric);
            results.Add(name, result);

            return true;
        }

        public ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>> GetExactPaths() {
            if (pathsExact != null)
                return pathsExact;

            int startIndex = -1;
            var paths = new List<ReadOnlyCollection<WheelPath.Point>>();

            for (int i = 0; i < Notes.Count; i++) {
                var note = Notes[i];
                var type = note.Type;
                
                if (startIndex >= 0 && (type == NoteType.SpinLeft || type == NoteType.SpinRight || type == NoteType.Scratch || i == Notes.Count  || i == Notes.Count - 1)) {
                    foreach (var path in WheelPath.GeneratePaths(Notes, startIndex, i))
                        paths.Add(new ReadOnlyCollection<WheelPath.Point>(path));

                    startIndex = -1;
                }
                else if (note.IsAutoSnap || paths.Count == 0 && startIndex < 0
                    && (type == NoteType.Tap || type == NoteType.Hold || type == NoteType.Match))
                    startIndex = i;
            }

            pathsExact = new ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>>(paths);

            return pathsExact;
        }

        public ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>> GetSimplifiedPaths(int iterations = -1) {
            if (iterations < 0 && pathsSimplified != null)
                return pathsSimplified;
            
            var paths = new List<ReadOnlyCollection<WheelPath.Point>>();

            foreach (var path in GetExactPaths())
                paths.Add(new ReadOnlyCollection<WheelPath.Point>(WheelPath.Simplify(path, iterations)));

            if (iterations < 0)
                pathsSimplified = new ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>>(paths);

            return pathsSimplified;
        }

        private Result CalculateMetric(Metric metric) {
            var candidates = metric.Calculate(this);
            float[] cumulativeValues = new float[candidates.Count];
            float sum = 0f;

            for (int i = 0; i < candidates.Count; i++) {
                cumulativeValues[i] = sum;
                sum += candidates[i].Value;
            }
            
            var indices = new List<int>() { 0 };
            
            Subdivide(0, candidates.Count - 1);

            var samples = new Sample[indices.Count];
            
            indices.Add(candidates.Count - 1);

            for (int i = 0; i < samples.Length; i++) {
                int startIndex = indices[i];
                int endIndex = indices[i + 1];
                float time = candidates[startIndex].Time;
                float length = candidates[endIndex].Time - time;

                samples[i] = new Sample(time, (cumulativeValues[endIndex] - cumulativeValues[startIndex]) / length, length);
            }

            return new Result(metric.Name, samples);

            void Subdivide(int start, int end) {
                if (end - start < 2)
                    return;
                
                float startTime = candidates[start].Time;
                float endTime = candidates[end].Time;
                
                int bestIndex = -1;
                float bestRatio = 0f;
                bool bestIsAZero = false;
                
                for (int i = start + 1; i < end; i++) {
                    if (Util.AlmostEquals(candidates[i].Value, candidates[i - 1].Value))
                        continue;
                    
                    float midTime = candidates[i].Time;

                    if (midTime - startTime < MIN_PHRASE_LENGTH || endTime - midTime < MIN_PHRASE_LENGTH)
                        continue;
                    
                    float first = (cumulativeValues[i] - cumulativeValues[start]) / (midTime - startTime);
                    float second = (cumulativeValues[end] - cumulativeValues[i]) / (endTime - midTime);
                    float ratio;

                    if (first > second)
                        ratio = first / second;
                    else
                        ratio = second / first;

                    bool almostZero = Util.AlmostEquals(candidates[i].Value, 0f);
                    
                    if (almostZero && (!bestIsAZero || ratio > bestRatio)) {
                        bestIndex = i;
                        bestRatio = ratio;
                        bestIsAZero = true;

                        continue;
                    }
                    
                    if (ratio <= bestRatio && !almostZero)
                        continue;

                    bestIndex = i;
                    bestRatio = ratio;
                }
                
                if (bestIndex < 0 || Util.AlmostEquals(bestRatio, 1f) || bestRatio < MAX_MERGE_RATIO && startTime - endTime < MAX_PHRASE_LENGTH)
                    return;
                
                Subdivide(start, bestIndex);
                indices.Add(bestIndex);
                Subdivide(bestIndex, end);
            }
        }
    }
}