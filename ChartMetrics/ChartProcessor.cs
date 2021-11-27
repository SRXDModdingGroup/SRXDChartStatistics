using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ChartHelper;
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
        private static readonly double[][] COEFFICIENTS;
        private static readonly Anchor[] ANCHORS = {
            new Anchor(0d, 0),
            new Anchor(0.482425903207917d, 30),
            new Anchor(0.610231954336391d, 36),
            new Anchor(0.669821538594481d, 41),
            new Anchor(0.707048429051248d, 46),
            new Anchor(0.741980824951096d, 51),
            new Anchor(0.888418897429309d, 57),
            new Anchor(0.956910366638872d, 67),
            new Anchor(0.990732215668648d, 71),
            new Anchor(0.994679518934933d, 73),
            new Anchor(0.996297721144238d, 75),
            new Anchor(1d, 80)
        };

        public static readonly float LOWER_QUANTILE = 0.1f;
        public static readonly float UPPER_QUANTILE = 0.85f;
        public static ReadOnlyCollection<Metric> Metrics { get; } = new ReadOnlyCollection<Metric>(METRICS);

        static ChartProcessor() {
            using (var reader = new BinaryReader(File.Open(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat"), FileMode.Open))) {
                COEFFICIENTS = new double[Metrics.Count][];
                
                for (int i = 0; i < Metrics.Count; i++) {
                    double baseCoeff = reader.ReadDouble();
                    double x0 = reader.ReadDouble();
                    double x1 = reader.ReadDouble();
                    double x2 = reader.ReadDouble();
                    
                    COEFFICIENTS[i] = new [] { baseCoeff, x0, x1, x2, };
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

        public readonly struct DetailedRatingInfo {
            public int DifficultyRating { get; }
            
            public double[] MeasuredValues { get; }
            
            public double[] ContributedValues { get; }

            internal DetailedRatingInfo(string chartTitle, int difficultyRating, double[] measuredValues, double[] contributedValues) {
                DifficultyRating = difficultyRating;
                MeasuredValues = measuredValues;
                ContributedValues = contributedValues;
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
            if (!ChartData.TryCreateFromFile(path, out var chartData, Difficulty.XD)) {
                processor = null;
                
                return false;
            }
            
            processor = new ChartProcessor(chartData.Title, chartData.TrackData[difficulty].Notes);

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
            double sum = 0d;

            for (int i = 0; i < METRICS.Length; i++) {
                var metric = METRICS[i];
                double[] coefficients = COEFFICIENTS[i];

                TryGetMetric(metric.Name, out var result);

                double value = coefficients[0] * result.GetClippedMean(result.GetQuantile(LOWER_QUANTILE), result.GetQuantile(UPPER_QUANTILE));
                
                sum += value * (coefficients[1] + value * (coefficients[2] + value * coefficients[3]));
            }

            if (sum < 0d)
                return 0;

            for (int i = 0; i < ANCHORS.Length - 1; i++) {
                var anchor = ANCHORS[i];
                var next = ANCHORS[i + 1];

                if (sum < next.From)
                    return (int) MathU.Remap(sum, anchor.From, next.From, anchor.To, next.To);
            }

            return 80;
        }
        
        public DetailedRatingInfo GetDifficultyRatingDetailed() {
            double sum = 0d;
            double[] measuredValues = new double[METRICS.Length];
            double[] contributedValues = new double[METRICS.Length];
            
            for (int i = 0; i < METRICS.Length; i++) {
                var metric = METRICS[i];
                double[] coefficients = COEFFICIENTS[i];

                TryGetMetric(metric.Name, out var result);

                double value = coefficients[0] * result.GetClippedMean(result.GetQuantile(LOWER_QUANTILE), result.GetQuantile(UPPER_QUANTILE));
                double contributedValue = value * (coefficients[1] + value * (coefficients[2] + value * coefficients[3]));
                
                measuredValues[i] = value;
                contributedValues[i] = contributedValue;
                sum += contributedValue;
            }

            if (sum < 0d)
                return new DetailedRatingInfo(ChartTitle, 0, measuredValues, contributedValues);

            for (int i = 0; i < ANCHORS.Length - 1; i++) {
                var anchor = ANCHORS[i];
                var next = ANCHORS[i + 1];

                if (sum < next.From)
                    return new DetailedRatingInfo(ChartTitle, (int) MathU.Remap(sum, anchor.From, next.From, anchor.To, next.To), measuredValues, contributedValues);
            }

            return new DetailedRatingInfo(ChartTitle, 80, measuredValues, contributedValues);
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