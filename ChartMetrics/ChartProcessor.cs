using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ChartHelper;
using ChartAutoRating;
using Util;

namespace ChartMetrics {
    public class ChartProcessor {
        private static readonly float SIMPLIFY_RATIO = 1.005f;
        private static readonly float MIN_PHRASE_LENGTH = 1f;
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
        private static readonly double[] BASE_COEFFICIENTS;
        private static readonly Network NETWORK;
        private static readonly Anchor[] ANCHORS = {
            new Anchor(0d, 0),
            new Anchor(0.12324755d, 30),
            new Anchor(0.15315513d, 35),
            new Anchor(0.21060236d, 44),
            new Anchor(0.23781559d, 48),
            new Anchor(0.24125996d, 54),
            new Anchor(0.32585019d, 59),
            new Anchor(0.34705346d, 62),
            new Anchor(0.37183286d, 69),
            new Anchor(0.40477940d, 73),
            new Anchor(0.43818623d, 75),
            new Anchor(0.5d, 80)
        };

        public static readonly float LOWER_QUANTILE = 0.1f;
        public static readonly float UPPER_QUANTILE = 0.85f;
        public static ReadOnlyCollection<Metric> Metrics { get; } = new ReadOnlyCollection<Metric>(METRICS);

        static ChartProcessor() {
            BASE_COEFFICIENTS = new double[METRICS.Length];
            NETWORK = new Network(METRICS.Length);

            using (var reader = new BinaryReader(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat"), FileMode.Open))) {
                for (int i = 0; i < METRICS.Length; i++) {
                    BASE_COEFFICIENTS[i] = reader.ReadDouble();

                    double x1 = reader.ReadDouble();
                    double x2 = reader.ReadDouble();
                    double x3 = reader.ReadDouble();
                    
                    NETWORK.SetCoefficients(i, new Coefficients(x1, x2, x3));
                }
            }
        }

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

        public class Result {
            public string MetricName { get; }
            
            public ReadOnlyCollection<Sample> Samples { get; }
            
            public ReadOnlyCollection<Sample> Sorted { get; }

            private float[] sortedEndTimes;

            internal Result(string metricName, IList<Sample> samples) {
                MetricName = metricName;
                Samples = new ReadOnlyCollection<Sample>(samples);
                
                var sorted = new Sample[samples.Count];

                for (int i = 0; i < samples.Count; i++)
                    sorted[i] = samples[i];

                Array.Sort(sorted);
                Sorted = new ReadOnlyCollection<Sample>(sorted);
            }

            public float GetMean() {
                if (Samples.Count == 0)
                    return 0f;

                if (Samples.Count == 1)
                    return Samples[0].Value;
                
                GetSortedEndTimes();

                float sum = 0f;

                foreach (var sample in Samples)
                    sum += sample.Value;

                return sum / sortedEndTimes[sortedEndTimes.Length - 1];
            }
            
            public float GetMedian() => GetQuantile(0.5f);

            public float GetQuantile(float quantile) {
                if (Samples.Count == 0)
                    return 0f;

                if (Samples.Count == 1)
                    return Samples[0].Value;
                
                GetSortedEndTimes();

                float totalLength = sortedEndTimes[sortedEndTimes.Length - 1];
                float targetTotal = quantile * totalLength;
                var first = Sorted[0];

                if (targetTotal < 0.5f * first.Length)
                    return first.Value;

                var last = Sorted[Sorted.Count - 1];

                if (targetTotal > totalLength - 0.5f * last.Length)
                    return last.Value;
                
                for (int i = 0; i < Sorted.Count - 1; i++) {
                    float end = sortedEndTimes[i] + 0.5f * Sorted[i + 1].Length;
                    
                    if (end < targetTotal)
                        continue;

                    float start = sortedEndTimes[i] - 0.5f * Sorted[i].Length;
                
                    return MathU.Remap(targetTotal, start, end, Sorted[i].Value, Sorted[i + 1].Value);
                }

                return Sorted[Sorted.Count - 1].Value;
            }

            public float GetClippedMean(float min, float max) {
                if (Samples.Count == 0)
                    return 0f;

                if (Samples.Count == 1)
                    return Samples[0].Value;
                
                GetSortedEndTimes();
                
                float totalLength = sortedEndTimes[sortedEndTimes.Length - 1];
                int firstIndex = -1;

                for (int i = 0; i < Sorted.Count; i++) {
                    if (Sorted[i].Value < min)
                        continue;

                    firstIndex = i;
                    
                    break;
                }

                if (firstIndex < 0)
                    return min;

                float sum;

                if (firstIndex > 0)
                    sum = min * sortedEndTimes[firstIndex - 1];
                else
                    sum = 0f;

                for (int i = firstIndex; i < Sorted.Count; i++) {
                    var sample = Sorted[i];
                    
                    if (sample.Value > max) {
                        if (i == 0)
                            return max;
                        
                        sum += max * (totalLength - sortedEndTimes[i - 1]);

                        break;
                    }
                    
                    sum += sample.Value * sample.Length;
                }

                return sum / totalLength;
            }
            
            private void GetSortedEndTimes() {
                if (sortedEndTimes != null)
                    return;

                sortedEndTimes = new float[Samples.Count];
                
                float totalLength = 0f;

                for (int i = 0; i < Sorted.Count; i++) {
                    totalLength += Sorted[i].Length;
                    sortedEndTimes[i] = totalLength;
                }
            }
        }
        
        private readonly struct Anchor {
            public double From { get; }
            
            public int To { get; }

            public Anchor(double from, int to) {
                From = from;
                To = to;
            }
        }
        
        public string ChartTitle { get; }
        
        public ReadOnlyCollection<Note> Notes { get; }

        private Dictionary<string, Result> results;
        private ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>> pathsExact;
        private ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>> pathsSimplified;

        public ChartProcessor(string chartTitle, ReadOnlyCollection<Note> notes) {
            ChartTitle = chartTitle;
            Notes = notes;
            results = new Dictionary<string, Result>();
        }

        public static bool TryLoadChart(string path, out ChartProcessor processor, Difficulty difficulty = Difficulty.XD) {
            if (!ChartData.TryCreateFromFile(path, out var chartData, Difficulty.XD) || !chartData.TrackData.TryGetValue(difficulty, out var trackData)) {
                processor = null;
                
                return false;
            }

            processor = new ChartProcessor(chartData.Title, trackData.Notes);

            return true;
        }

        public bool TryGetMetric(string name, out Result result) {
            name = name.ToLowerInvariant();
            
            if (!METRICS_DICT.TryGetValue(name, out var metric)) {
                result = null;
                
                return false;
            }

            if (results.TryGetValue(name, out result))
                return true;

            result = CalculateMetric(metric);
            results.Add(name, result);

            return true;
        }

        public int GetDifficultyRating() {
            double value = NETWORK.GetValue(CreateData());

            if (value < 0d)
                return 0;

            for (int i = 0; i < ANCHORS.Length - 1; i++) {
                var anchor = ANCHORS[i];
                var next = ANCHORS[i + 1];

                if (value < next.From)
                    return (int) MathU.Remap(value, anchor.From, next.From, anchor.To, next.To);
            }

            return 80;
        }

        public Data CreateData() {
            var data = Data.Create(METRICS.Length, i => {
                TryGetMetric(METRICS[i].Name, out var result);

                return result.Samples.Select(sample => ((double) sample.Value, (double) sample.Length));
            });
            
            data.Normalize(BASE_COEFFICIENTS);

            return data;
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
            indices.Add(candidates.Count - 1);

            for (int i = 1; i < indices.Count - 1; i++) {
                int firstIndex = indices[i - 1];
                int midIndex = indices[i];
                int lastIndex = indices[i + 1];
                float first = (cumulativeValues[midIndex] - cumulativeValues[firstIndex]) / (candidates[midIndex].Time - candidates[firstIndex].Time);
                float second = (cumulativeValues[lastIndex] - cumulativeValues[midIndex]) / (candidates[lastIndex].Time - candidates[midIndex].Time);
                float ratio;

                if (first > second)
                    ratio = first / second;
                else
                    ratio = second / first;

                if (ratio >= SIMPLIFY_RATIO)
                    continue;
                
                indices.RemoveAt(i);
                i--;
            }

            var samples = new Sample[indices.Count - 1];

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
                    if (MathU.AlmostEquals(candidates[i].Value, candidates[i - 1].Value))
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

                    bool almostZero = MathU.AlmostEquals(candidates[i].Value, 0f);
                    
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
                
                if (bestIndex < 0)
                    return;
                
                Subdivide(start, bestIndex);
                indices.Add(bestIndex);
                Subdivide(bestIndex, end);
            }
        }
    }
}