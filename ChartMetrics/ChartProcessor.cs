using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ChartHelper.Types;
using Util;

namespace ChartMetrics {
    public class ChartProcessor {
        private static readonly float SIMPLIFY_RATIO = 1.05f;
        private static readonly float MIN_PHRASE_LENGTH = 0.25f;
        private static readonly Metric[] METRICS = {
            new OverallNoteDensity(),
            new TapBeatDensity(),
            new RequiredMovement(),
            new Acceleration(),
            new Drift(),
            new MovementComplexity(),
            new SequenceComplexity(),
            new TapBeatComplexity()
        };
        private static readonly Metric[] DIFFICULTY_METRICS = {
            new OverallNoteDensity(),
            new TapBeatDensity(),
            new MovementNoteDensity(),
            new RequiredMovement(),
            new Acceleration(),
            new MovementComplexity(),
            new SequenceComplexity(),
            new TapBeatComplexity()
        };
        private static readonly Dictionary<string, Metric> METRICS_DICT;

        public static readonly float LOWER_QUANTILE = 0.1f;
        public static readonly float UPPER_QUANTILE = 0.975f;
        public static ReadOnlyCollection<Metric> Metrics { get; } = new ReadOnlyCollection<Metric>(METRICS);
        public static ReadOnlyCollection<Metric> DifficultyMetrics { get; } = new ReadOnlyCollection<Metric>(DIFFICULTY_METRICS);

        private static Matrix matrix;
        private static double[] baseCoefficients;
        private static double bias;
        private static double scale;
        private static bool parametersLoaded;

        static ChartProcessor() {
            METRICS_DICT = new Dictionary<string, Metric>();

            foreach (var metric in METRICS)
                METRICS_DICT.Add(metric.Name.ToLowerInvariant(), metric);

            foreach (var metric in DIFFICULTY_METRICS) {
                if (!METRICS_DICT.ContainsKey(metric.Name.ToLowerInvariant()))
                    METRICS_DICT.Add(metric.Name.ToLowerInvariant(), metric);
            }
        }

        private static void LoadParameters() {
            if (parametersLoaded)
                return;
            
            baseCoefficients = new double[DIFFICULTY_METRICS.Length];

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "parameters.dat");
            
            if (!File.Exists(path))
                return;

            using (var reader = new BinaryReader(File.Open(path, FileMode.Open))) {
                for (int i = 0; i < DIFFICULTY_METRICS.Length; i++)
                    baseCoefficients[i] = reader.ReadDouble();
                
                matrix = Matrix.Deserialize(reader);
                bias = reader.ReadDouble();
                scale = reader.ReadDouble();
            }

            parametersLoaded = true;
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
        
        public string Title { get; private set; }
        
        public int DifficultyRating { get; private set; }
        
        public ReadOnlyCollection<Note> Notes { get; private set; }
        
        private Dictionary<string, Result> results;
        private List<ReadOnlyCollection<WheelPath.Point>> exactPaths;
        private List<ReadOnlyCollection<WheelPath.Point>> simplifiedPaths;

        public ChartProcessor() {
            results = new Dictionary<string, Result>();
            exactPaths = new List<ReadOnlyCollection<WheelPath.Point>>();
            simplifiedPaths = new List<ReadOnlyCollection<WheelPath.Point>>();
        }

        public void SetData(string title, int difficultyRating, IList<Note> notes) {
            Title = title;
            DifficultyRating = difficultyRating;
            Notes = new ReadOnlyCollection<Note>(notes);
            exactPaths.Clear();
            simplifiedPaths.Clear();
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
            LoadParameters();

            double value = CreateNormalizedData().GetResult(matrix);

            if (value < 0d)
                return 0;

            if (value > 1d)
                return 100;

            return (int) Math.Round(100d * (value - bias) / scale);
        }

        public Data CreateData(IReadOnlyList<Metric> metrics) {
            var data = Data.Create(Title, DifficultyRating, metrics.Count, i => {
                TryGetMetric(metrics[i].Name, out var result);

                var samples = result.Samples;
                var last = samples[samples.Count - 1];

                return samples.Select(sample => ((double) sample.Value, (double) sample.Time)).Append((0d, last.Time + last.Length));
            });
            
            return data;
        }

        public ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>> GetExactPaths() {
            if (exactPaths.Count > 0)
                return new ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>>(exactPaths);

            int startIndex = -1;

            for (int i = 0; i < Notes.Count; i++) {
                var note = Notes[i];
                var type = note.Type;
                
                if (startIndex >= 0 && (type == NoteType.SpinLeft || type == NoteType.SpinRight || type == NoteType.Scratch || i == Notes.Count  || i == Notes.Count - 1)) {
                    foreach (var path in WheelPath.GeneratePaths(Notes, startIndex, i))
                        exactPaths.Add(new ReadOnlyCollection<WheelPath.Point>(path));

                    startIndex = -1;
                }
                else if (note.IsAutoSnap || exactPaths.Count == 0 && startIndex < 0
                    && (type == NoteType.Tap || type == NoteType.Hold || type == NoteType.Match))
                    startIndex = i;
            }

            return new ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>>(exactPaths);
        }

        public ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>> GetSimplifiedPaths(int iterations = -1) {
            if (iterations < 0 && simplifiedPaths.Count > 0)
                return new ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>>(simplifiedPaths);
            
            foreach (var path in GetExactPaths())
                simplifiedPaths.Add(new ReadOnlyCollection<WheelPath.Point>(WheelPath.Simplify(path, iterations)));
            
            return new ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>>(simplifiedPaths);
        }

        private Result CalculateMetric(Metric metric) {
            IList<Metric.Point> candidates;

            candidates = metric.Calculate(this);
            
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

        private Data CreateNormalizedData() {
            LoadParameters();
            
            var data = CreateData(DIFFICULTY_METRICS);
            
            for (int i = 0; i < DIFFICULTY_METRICS.Length; i++)
                data.Clamp(i, data.GetQuantile(i, 0.975d));
            
            data.Normalize(baseCoefficients);

            return data;
        }
    }
}