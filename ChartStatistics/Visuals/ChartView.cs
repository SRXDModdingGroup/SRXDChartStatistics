﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using ChartHelper;
using ChartMetrics;

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
            Command.AddListener("load", args => LoadChart(args[0]));
            Command.AddListener("show", args => DisplayMetric(args[0]));
            Command.AddListener("path", args => DisplayPath(args[0], int.TryParse(args[1], out int val) ? val : -1));
            Command.SetPossibleValues("show", 0, ChartProcessor.Metrics.Select(metric => $"{metric.Name.ToLower()}: {metric.Description}").ToArray());
            // LoadChart("spinshare_60c0753bac2db");
            // DisplayMetric("DriftWeighted");
            // DisplayPath("Simplified", -1);
        }

        private void LoadChart(string path) {
            if (!ChartProcessor.TryLoadChart(path, out chartProcessor)) {
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

            var normalized = new PointF[samples.Count];

            for (int i = 0; i < samples.Count; i++) {
                var sample = samples[i];

                normalized[i] = new PointF(sample.Time, sample.Value / max);
            }

            var metricGraph = new BarGraph(samples[0].Time, samples[samples.Count - 1].Time + samples[samples.Count - 1].Length, graphBottom, graphTop, normalized);
            
            graphicsPanel.AddDrawable(metricGraph);
            metricDrawables.Add(metricGraph);

            float median = result.GetQuantile(0.5f);
            var medianLabel = new ValueLabel(Util.Lerp(graphBottom, graphTop, median / max), $"Med ({median})");
            
            graphicsPanel.AddDrawable(medianLabel);
            metricDrawables.Add(medianLabel);

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
            
            ReadOnlyCollection<ReadOnlyCollection<WheelPath.Point>> paths;

            name = name.ToLowerInvariant();

            switch (name) {
                case "exact":
                    paths = chartProcessor.GetExactPaths();

                    break;
                case "simplified":
                    paths = chartProcessor.GetSimplifiedPaths(iterations);

                    break;
                case "none":
                    paths = null;

                    break;
                default:
                    Console.WriteLine("This is not a valid path type");
                    
                    return;
            }

            lastShownPath = name;
            
            foreach (var drawable in pathDrawables)
                graphicsPanel.RemoveDrawable(drawable);
            
            pathDrawables.Clear();
            
            if (paths == null)
                return;

            foreach (var path in paths) {
                var sameColorRun = new List<PointF>();
                var currentColor = path[0].CurrentColor;

                for (int j = 0; j < path.Count; j++) {
                    var point = path[j];
                    float time = point.Time;
                    float position = point.LanePosition;

                    if (point.CurrentColor != currentColor) {
                        var previous = path[j - 1];
                        float positionDifference = point.NetPosition - previous.NetPosition;
                        float endTime = time;
                        float endPosition = previous.LanePosition + positionDifference;
                        
                        TruncateLine(previous.Time, previous.LanePosition, ref endTime, ref endPosition);
                        sameColorRun.Add(new PointF(endTime, (endPosition + 4f) / 8f));

                        if (sameColorRun.Count > 1) {
                            var graph = new LineGraph(sameColorRun[0].X, sameColorRun[sameColorRun.Count - 1].X, chartBottom, chartTop, sameColorRun);
                            
                            graphicsPanel.AddDrawable(graph);
                            pathDrawables.Add(graph);
                        }

                        currentColor = point.CurrentColor;
                        sameColorRun = new List<PointF>();
                        endTime = previous.Time;
                        endPosition = point.LanePosition - positionDifference;
                        TruncateLine(time, position, ref endTime, ref endPosition);
                        sameColorRun.Add(new PointF(endTime, (endPosition + 4f) / 8f));

                        void TruncateLine(float startX, float startY, ref float endX, ref float endY) {
                            if (endY > 4f) {
                                endX = Util.Remap(4f, startY, endY, startX, endX);
                                endY = 4f;
                            }
                            else if (endY < -4f) {
                                endX = Util.Remap(-4f, startY, endY, startX, endX);
                                endY = -4f;
                            }
                        }
                    }

                    sameColorRun.Add(new PointF(time, (position + 4f) / 8f));
                }

                if (sameColorRun.Count > 1) {
                    var graph = new LineGraph(sameColorRun[0].X, sameColorRun[sameColorRun.Count - 1].X, chartBottom, chartTop, sameColorRun);
                    
                    graphicsPanel.AddDrawable(graph);
                    pathDrawables.Add(graph);
                }
            }
            
            graphicsPanel.Redraw();
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
                        graphicsPanel.AddDrawable(new Match(note.Time, ColumnToY(note.Column), note.Color == NoteColor.Red));

                        break;
                    case NoteType.Beat:
                        graphicsPanel.AddDrawable(new Beat(note.Time, chartTop, chartBottom));

                        if (note.EndIndex > -1)
                            graphicsPanel.AddDrawable(new Zone(note.Time, notes[note.EndIndex].Time, Drawable.DrawLayer.BeatHold, Zone.ZoneType.BeatHold, chartTop, chartBottom));

                        break;
                    case NoteType.SpinRight:
                        graphicsPanel.AddDrawable(new Zone(note.Time, notes[note.EndIndex].Time, Drawable.DrawLayer.Zone, Zone.ZoneType.RightSpin, chartTop, chartBottom));

                        break;
                    case NoteType.SpinLeft:
                        graphicsPanel.AddDrawable(new Zone(note.Time, notes[note.EndIndex].Time, Drawable.DrawLayer.Zone, Zone.ZoneType.LeftSpin, chartTop, chartBottom));

                        break;
                    case NoteType.HoldPoint:
                    case NoteType.HoldEnd:
                    case NoteType.Liftoff:
                        var startNote = notes[note.StartIndex];

                        graphicsPanel.AddDrawable(new HoldSegment(startNote.Time, note.Time, ColumnToY(startNote.Column), ColumnToY(note.Column), startNote.Color == NoteColor.Red, startNote.CurveType));

                        break;
                    case NoteType.Tap:
                    case NoteType.Hold:
                        graphicsPanel.AddDrawable(new Tap(note.Time, ColumnToY(note.Column), note.Color == NoteColor.Red));

                        break;
                    case NoteType.Scratch:
                        graphicsPanel.AddDrawable(new Zone(note.Time, notes[note.EndIndex].Time, Drawable.DrawLayer.Zone, Zone.ZoneType.Scratch, chartTop, chartBottom));

                        break;
                }
            }

            graphicsPanel.Redraw();
        }

        private float ColumnToY(float column) => chartCenter + chartHeight * column / -8f;
    }
}