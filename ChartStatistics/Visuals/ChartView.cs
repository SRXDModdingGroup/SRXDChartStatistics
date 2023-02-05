using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using ChartHelper;
using ChartHelper.Parsing;
using ChartHelper.Types;
using ChartMetrics;
using Util;

namespace ChartStatistics {
    public class ChartView {
        private float chartTop;
        private float chartBottom;
        private float chartCenter;
        private float chartHeight;
        private float graphTop;
        private float graphBottom;
        private string lastShownMetric;
        private string lastShownPath;
        private ChartProcessor chartProcessor;
        private GraphicsPanel graphicsPanel;
        private List<Drawable> metricDrawables;
        private List<Drawable> pathDrawables;

        public ChartView(float chartTop, float chartBottom, float graphTop, float graphBottom, GraphicsPanel graphicsPanel) {
            this.graphicsPanel = graphicsPanel;
            this.graphTop = graphTop;
            this.graphBottom = graphBottom;
            this.chartTop = chartTop;
            this.chartBottom = chartBottom;
            chartCenter = 0.5f * (chartBottom + chartTop);
            chartHeight = chartBottom - chartTop;
            graphicsPanel.AddDrawable(new Grid(chartTop, chartBottom, 7, 5));
            metricDrawables = new List<Drawable>();
            pathDrawables = new List<Drawable>();
            Command.AddListener("load", args => {
                if (TryParseDifficulty(args[1], out var diff))
                    LoadChart(args[0], diff);
            });
            Command.AddListener("show", args => DisplayMetric(args[0]));
            Command.AddListener("path", args => DisplayPath(args[0], int.TryParse(args[1], out int val) ? val : -1));
            Command.AddListener("rate", args => {
                if (TryParseDifficulty(args[1], out var diff))
                    RateChart(args[0], string.IsNullOrWhiteSpace(args[0]), diff);
            });
            Command.AddListener("rateall", args => {
                if (TryParseDifficulty(args[0], out var diff))
                    RateAllCharts(diff);
            });
            Command.SetPossibleValues("show", 0, ChartProcessor.Metrics.Select(metric => $"{metric.Name.ToLower()}: {metric.Description}").ToArray());
            LoadChart("spinshare_61b9f36788196");
            // DisplayMetric("sequencecomplexity");
            // DisplayPath("Simplified", -1);
        }

        private void LoadChart(string path, SRTB.DifficultyType difficulty = SRTB.DifficultyType.XD) {
            if (!TryLoadChart(path, difficulty, ref chartProcessor)) {
                Console.WriteLine("Could not find this file");
                
                return;
            }
            
            DrawChart();
            Console.WriteLine("Loaded chart successfully");
            
            if (!string.IsNullOrWhiteSpace(lastShownMetric))
                DisplayMetric(lastShownMetric);

            if (!string.IsNullOrWhiteSpace(lastShownPath))
                DisplayPath(lastShownPath, -1);
        }

        private void DisplayMetric(string name) {
            if (chartProcessor == null) {
                Console.WriteLine("A chart has not been loaded");
                
                return;
            }
            
            if (!chartProcessor.TryGetMetric(name, out var result)) {
                Console.WriteLine("This metric does not exist");
                
                return;
            }

            lastShownMetric = result.MetricName;

            foreach (var drawable in metricDrawables)
                graphicsPanel.RemoveDrawable(drawable);
            
            metricDrawables.Clear();

            var samples = result.Samples;
            float max = 0f;

            for (int i = 0; i < samples.Count; i++) {
                var sample = samples[i];
                var marker = new PhraseMarker(sample.Time, chartBottom, i, sample.Value);
                
                graphicsPanel.AddDrawable(marker);
                metricDrawables.Add(marker);

                if (sample.Value > max)
                    max = sample.Value;
            }

            var normalized = new PointD[samples.Count];

            for (int i = 0; i < samples.Count; i++) {
                var sample = samples[i];

                normalized[i] = new PointD(sample.Time, sample.Value / max);
            }

            var metricGraph = new BarGraph(samples[0].Time, samples[samples.Count - 1].Time + samples[samples.Count - 1].Length, graphBottom, graphTop, normalized);
            
            graphicsPanel.AddDrawable(metricGraph);
            metricDrawables.Add(metricGraph);

            float lowerQuantile = result.GetQuantile(ChartProcessor.LOWER_QUANTILE);
            var valueLabel = new ValueLabel(MathU.Lerp(graphBottom, graphTop, lowerQuantile / max), $"Low ({lowerQuantile:0.00})");
            
            graphicsPanel.AddDrawable(valueLabel);
            metricDrawables.Add(valueLabel);
            
            float upperQuantile = result.GetQuantile(ChartProcessor.UPPER_QUANTILE);
            
            valueLabel = new ValueLabel(MathU.Lerp(graphBottom, graphTop, upperQuantile / max), $"High ({upperQuantile:0.00})");
            graphicsPanel.AddDrawable(valueLabel);
            metricDrawables.Add(valueLabel);
            
            float mean = result.GetClippedMean(lowerQuantile, upperQuantile);
            
            valueLabel = new ValueLabel(MathU.Lerp(graphBottom, graphTop, mean / max), $"Mean ({mean:0.00})");
            graphicsPanel.AddDrawable(valueLabel);
            metricDrawables.Add(valueLabel);

            var metricLabel = new Label(0f, graphTop, result.MetricName);
            
            graphicsPanel.AddDrawable(metricLabel);
            metricDrawables.Add(metricLabel);
            graphicsPanel.Redraw();
        }

        private void DisplayPath(string name, int iterations) {
            if (chartProcessor == null) {
                Console.WriteLine("A chart has not been loaded");
                
                return;
            }
            
            IReadOnlyList<WheelPathPoint> pathPoints;

            name = name.ToLowerInvariant();

            switch (name) {
                case "exact":
                    pathPoints = chartProcessor.ExactPath.Points;

                    break;
                case "simplified":
                    pathPoints = chartProcessor.SimplifiedPath.Points;

                    break;
                case "none":
                    pathPoints = null;

                    break;
                default:
                    Console.WriteLine("This is not a valid path type");
                    
                    return;
            }

            lastShownPath = name;
            
            foreach (var drawable in pathDrawables)
                graphicsPanel.RemoveDrawable(drawable);
            
            pathDrawables.Clear();
            
            if (pathPoints == null)
                return;

            var sameColorRun = new List<PointD>();
            var currentColor = pathPoints[0].CurrentColor;

            for (int j = 0; j < pathPoints.Count; j++) {
                var point = pathPoints[j];
                double time = point.Time;
                float position = point.LanePosition;

                if (point.CurrentColor != currentColor) {
                    var previous = pathPoints[j - 1];
                    float positionDifference = point.NetPosition - previous.NetPosition;
                    double endTime = time;
                    float endPosition = previous.LanePosition + positionDifference;
                    
                    TruncateLine(previous.Time, previous.LanePosition, ref endTime, ref endPosition);
                    sameColorRun.Add(new PointD(endTime, (endPosition + 4f) / 8f));

                    if (sameColorRun.Count > 1) {
                        var graph = new LineGraph(sameColorRun[0].X, sameColorRun[sameColorRun.Count - 1].X, chartBottom, chartTop, sameColorRun);
                        
                        graphicsPanel.AddDrawable(graph);
                        pathDrawables.Add(graph);
                    }

                    currentColor = point.CurrentColor;
                    sameColorRun = new List<PointD>();
                    endTime = previous.Time;
                    endPosition = point.LanePosition - positionDifference;
                    TruncateLine(time, position, ref endTime, ref endPosition);
                    sameColorRun.Add(new PointD(endTime, (endPosition + 4f) / 8f));

                    void TruncateLine(double startX, float startY, ref double endX, ref float endY) {
                        if (endY > 4f) {
                            endX = MathU.Remap(4d, startY, endY, startX, endX);
                            endY = 4f;
                        }
                        else if (endY < -4f) {
                            endX = MathU.Remap(-4d, startY, endY, startX, endX);
                            endY = -4f;
                        }
                    }
                }

                sameColorRun.Add(new PointD(time, (position + 4f) / 8f));
            }

            if (sameColorRun.Count > 1) {
                var graph = new LineGraph(sameColorRun[0].X, sameColorRun[sameColorRun.Count - 1].X, chartBottom, chartTop, sameColorRun);
                
                graphicsPanel.AddDrawable(graph);
                pathDrawables.Add(graph);
            }
            
            graphicsPanel.Redraw();
        }

        private void RateChart(string path, bool rateThis, SRTB.DifficultyType difficulty) {
            ChartProcessor processor = null;

            if (rateThis) {
                if (chartProcessor == null) {
                    Console.WriteLine("A chart has not been loaded");
                    
                    return;
                }
                
                processor = chartProcessor;
            }
            else if (!TryLoadChart(path, difficulty, ref processor)) {
                Console.WriteLine("Could not load this file");

                return;
            }

            Console.WriteLine($"Difficulty: {0}");
        }

        private void DrawChart() {
            graphicsPanel.Clear();
            graphicsPanel.AddDrawable(new Grid(chartTop, chartBottom, 9, 10));
            graphicsPanel.AddDrawable(new Grid(graphTop, graphBottom, 10, 10));
            metricDrawables.Clear();
            pathDrawables.Clear();
            
            var notes = chartProcessor.Notes;
            
            foreach (var note in notes) {
                switch (note.Type) {
                    case NoteType.Match:
                        graphicsPanel.AddDrawable(new MatchNote(note.Time, ColumnToY(note.Column), note.Color == NoteColor.Red));

                        break;
                    case NoteType.Beat:
                        graphicsPanel.AddDrawable(new Beat(note.Time, chartTop, chartBottom));

                        if (note.EndIndex >= 0)
                            graphicsPanel.AddDrawable(new Zone(note.Time, notes[note.EndIndex].Time, Drawable.DrawLayer.BeatHold, Zone.ZoneType.BeatHold, chartTop, chartBottom));

                        break;
                    case NoteType.SpinRight:
                        graphicsPanel.AddDrawable(new Zone(note.Time, note.EndIndex >= 0 ? notes[note.EndIndex].Time : note.Time + 1f, Drawable.DrawLayer.Zone, Zone.ZoneType.RightSpin, chartTop, chartBottom));

                        break;
                    case NoteType.SpinLeft:
                        graphicsPanel.AddDrawable(new Zone(note.Time, note.EndIndex >= 0 ? notes[note.EndIndex].Time : note.Time + 1f, Drawable.DrawLayer.Zone, Zone.ZoneType.LeftSpin, chartTop, chartBottom));

                        break;
                    case NoteType.HoldPoint:
                    case NoteType.HoldEnd:
                    case NoteType.Liftoff:
                        if (note.StartIndex < 0)
                            break;
                        
                        var startNote = notes[note.StartIndex];
                        
                        graphicsPanel.AddDrawable(new HoldSegment(startNote.Time, note.Time, ColumnToY(startNote.Column), ColumnToY(note.Column), startNote.Color == NoteColor.Red, startNote.CurveType));

                        break;
                    case NoteType.Tap:
                    case NoteType.Hold:
                        graphicsPanel.AddDrawable(new Tap(note.Time, ColumnToY(note.Column), note.Color == NoteColor.Red));

                        break;
                    case NoteType.Scratch:
                        graphicsPanel.AddDrawable(new Zone(note.Time, note.EndIndex >= 0 ? notes[note.EndIndex].Time : note.Time + 1f, Drawable.DrawLayer.Zone, Zone.ZoneType.Scratch, chartTop, chartBottom));

                        break;
                }
            }

            graphicsPanel.Redraw();
        }

        private float ColumnToY(float column) => chartCenter + chartHeight * column / -8f;

        private static void RateAllCharts(SRTB.DifficultyType difficulty) {
            var data = new List<(string, int)>();
            var processor = new ChartProcessor();
            string[] allPaths = FileHelper.GetAllSrtbs().ToArray();

            for (int i = 0; i < allPaths.Length; i++) {
                string path = allPaths[i];
                
                if (!TryLoadChart(path, difficulty, ref processor))
                    continue;

                int diff = 0;

                try {
                    diff = 0;
                }
                catch (Exception e) {
                    Console.WriteLine($"Error scanning chart {processor.Title}:");
                    Console.WriteLine(e.Message);
                }
                
                data.Add((processor.Title, diff));
                Console.WriteLine($"Scanned chart {i} of {allPaths.Length}");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }

            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
            data.Sort((a, b) => a.Item2.CompareTo(b.Item2));

            foreach ((string title, int diff) in data)
                Console.WriteLine($"{diff} - {title}");
            
            Console.WriteLine();
        }
        
        private static bool TryLoadChart(string path, SRTB.DifficultyType difficulty, ref ChartProcessor processor) {
            var srtb = SRTB.DeserializeFromFile(path);
            
            if (srtb == null)
                return false;

            var trackData = srtb.GetTrackData(difficulty);

            if (trackData == null)
                return false;

            if (processor == null)
                processor = new ChartProcessor();
            
            processor.SetData(srtb.GetTrackInfo().Title, NoteConversion.ToCustomNotesList(trackData.Notes));

            return true;
        }
        
        private static bool TryParseDifficulty(string arg, out SRTB.DifficultyType difficulty) {
            difficulty = SRTB.DifficultyType.XD;

            if (string.IsNullOrWhiteSpace(arg) || Enum.TryParse(arg, true, out difficulty))
                return true;
            
            Console.WriteLine("Invalid difficulty string");

            return false;

        }
    }
}