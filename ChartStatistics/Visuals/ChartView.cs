using System;
using System.Collections.Generic;
using System.Linq;
using ChartHelper.Parsing;
using ChartHelper.Types;
using ChartMetrics;
using Util;

namespace ChartStatistics; 

public class ChartView {
    private static readonly Metric[] METRICS = {
        new Acceleration(),
        new MovementNoteDensity(),
        new OverallNoteDensity(),
        new PointValue(),
        new RequiredMovement(),
        new SpinDensity(),
        new TapBeatDensity()
    };
    
    private static readonly Metric[] RATING_METRICS = {
        new Acceleration(),
        new MovementNoteDensity(),
        new OverallNoteDensity(),
        new RequiredMovement(),
        new SpinDensity(),
        new TapBeatDensity()
    };

    private float chartTop;
    private float chartBottom;
    private float chartCenter;
    private float chartHeight;
    private float graphTop;
    private float graphBottom;
    private string lastShownMetric;
    private string lastShownPath;
    private ChartData chartData;
    private GraphicsPanel graphicsPanel;
    private List<Drawable> metricDrawables;
    private List<Drawable> pathDrawables;

    public ChartView(float chartTop, float chartBottom, float graphTop, float graphBottom, GraphicsPanel graphicsPanel) {
        this.chartTop = chartTop;
        this.chartBottom = chartBottom;
        chartCenter = 0.5f * (chartBottom + chartTop);
        chartHeight = chartBottom - chartTop;
        this.graphicsPanel = graphicsPanel;
        this.graphTop = graphTop;
        this.graphBottom = graphBottom;
        lastShownMetric = string.Empty;
        lastShownPath = string.Empty;
        chartData = ChartData.Empty;
        graphicsPanel.AddDrawable(new Grid(chartTop, chartBottom, 7, 5));
        metricDrawables = new List<Drawable>();
        pathDrawables = new List<Drawable>();
        Command.AddListener("load", args => {
            if (TryParseDifficulty(args[1], out var diff))
                LoadChart(args[0], diff);
        });
        Command.AddListener("show", args => DisplayMetric(args[0]));
        Command.AddListener("path", args => DisplayPath(args[0]));
        Command.AddListener("rate", args => {
            if (TryParseDifficulty(args[1], out var diff))
                RateChart(args[0], string.IsNullOrWhiteSpace(args[0]), diff);
        });
        Command.AddListener("rateall", args => {
            if (TryParseDifficulty(args[0], out var diff))
                RateAllCharts(diff);
        });
        Command.SetPossibleValues("show", 0, METRICS.Select(metric => $"{metric.Name.ToLower()}: {metric.Description}").ToArray());
        LoadChart("Overthinker");
        DisplayMetric("requiredmovement");
        DisplayPath("simplified");
    }

    private void LoadChart(string path, SRTB.DifficultyType difficulty = SRTB.DifficultyType.XD) {
        if (!TryLoadSrtb(path, out var srtb)) {
            Console.WriteLine("Could not find this file");
                
            return;
        }

        if (!TryGetTrackData(srtb, difficulty, out var trackData)) {
            Console.WriteLine("Could not get difficulty");
                
            return;
        }
            
        chartData = ChartData.Create(NoteConversion.ToCustomNotesList(trackData.Notes));
        DrawChart();
        Console.WriteLine("Loaded chart successfully");
            
        if (!string.IsNullOrWhiteSpace(lastShownMetric))
            DisplayMetric(lastShownMetric);

        if (!string.IsNullOrWhiteSpace(lastShownPath))
            DisplayPath(lastShownPath);
    }

    private void DisplayMetric(string name) {
        if (!TryGetMetric(name, out var metric)) {
            Console.WriteLine("This metric does not exist");
                
            return;
        }

        lastShownMetric = metric.Name;

        foreach (var drawable in metricDrawables)
            graphicsPanel.RemoveDrawable(drawable);
            
        metricDrawables.Clear();

        var result = metric.Calculate(chartData);
        var notes = chartData.Notes;
        var plot = result.GetPlot(notes[0].Time, notes[notes.Count - 1].Time, 100d).Smooth(100);
        var points = plot.Points;
        double max = 0d;

        foreach (double value in points) {
            if (value > max)
                max = value;
        }
            
        var normalized = new PointD[points.Count];

        for (int i = 0; i < points.Count; i++) {
            double value = points[i];

            normalized[i] = new PointD(plot.StartTime + plot.EndTime * ((double) i / (points.Count - 1)), value / max);
        }

        var metricGraph = new BarGraph(plot.StartTime, plot.EndTime, graphBottom, graphTop, normalized);
            
        graphicsPanel.AddDrawable(metricGraph);
        metricDrawables.Add(metricGraph);
            
        double lowerQuantile = plot.GetQuantile(0.1d);
        var valueLabel = new ValueLabel(MathU.Lerp(graphBottom, graphTop, (float) (lowerQuantile / max)), $"Low ({lowerQuantile:0.00})");
            
        graphicsPanel.AddDrawable(valueLabel);
        metricDrawables.Add(valueLabel);
            
        double upperQuantile = plot.GetQuantile(0.9d);
            
        valueLabel = new ValueLabel(MathU.Lerp(graphBottom, graphTop, (float) (upperQuantile / max)), $"High ({upperQuantile:0.00})");
        graphicsPanel.AddDrawable(valueLabel);
            
        var metricLabel = new Label(0f, graphTop, metric.Name);
            
        graphicsPanel.AddDrawable(metricLabel);
        metricDrawables.Add(metricLabel);
        graphicsPanel.Redraw();
    }

    private void DisplayPath(string name) {
        WheelPath path;

        name = name.ToLowerInvariant();

        switch (name) {
            case "exact":
                path = chartData.ExactPath;

                break;
            case "simplified":
                path = chartData.SimplifiedPath;

                break;
            case "none":
                path = WheelPath.Empty;

                break;
            default:
                Console.WriteLine("This is not a valid path type");
                    
                return;
        }
            
        var points = path.Points;

        lastShownPath = name;
            
        foreach (var drawable in pathDrawables)
            graphicsPanel.RemoveDrawable(drawable);
            
        pathDrawables.Clear();
            
        if (points == null)
            return;

        var currentPath = new List<PointD>();

        for (int i = 0; i < points.Count; i++) {
            var point = points[i];
            double time = point.Time;
            float position = point.LanePosition;
                
            currentPath.Add(new PointD(time, (position + 4f) / 8f));

            if (i == points.Count - 1 || points[i + 1].FirstInPath) {
                var graph = new LineGraph(currentPath[0].X, currentPath[currentPath.Count - 1].X, chartBottom, chartTop, new List<PointD>(currentPath));
                
                graphicsPanel.AddDrawable(graph);
                pathDrawables.Add(graph);
                currentPath.Clear();
            }
            else if (point.CurrentColor != points[i + 1].CurrentColor) {
                var next = points[i + 1];
                float positionDifference = next.NetPosition - point.NetPosition;
                float endPosition = next.LanePosition + positionDifference;
                double endTime = next.Time;
                    
                TruncateLine(time, position, ref endTime, ref endPosition);
                currentPath.Add(new PointD(endTime, (endPosition + 4f) / 8f));
                    
                var graph = new LineGraph(currentPath[0].X, currentPath[currentPath.Count - 1].X, chartBottom, chartTop, new List<PointD>(currentPath));
                
                graphicsPanel.AddDrawable(graph);
                pathDrawables.Add(graph);
                currentPath.Clear();
                endTime = point.Time;
                endPosition = next.LanePosition - positionDifference;
                TruncateLine(next.Time, next.LanePosition, ref endTime, ref endPosition);
                currentPath.Add(new PointD(endTime, (endPosition + 4f) / 8f));
            }
        }
            
        graphicsPanel.Redraw();
            
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

    private void RateChart(string path, bool rateThis, SRTB.DifficultyType difficulty) {
        ChartData chartDataToRate;
            
        if (rateThis)
            chartDataToRate = chartData;
        else if (!TryLoadSrtb(path, out var srtb)) {
            Console.WriteLine("Could not find this file");
                
            return;
        }
        else if (!TryGetTrackData(srtb, difficulty, out var trackData)) {
            Console.WriteLine("Could not get difficulty");

            return;
        }
        else
            chartDataToRate = ChartData.Create(NoteConversion.ToCustomNotesList(trackData.Notes));

        var ratingData = ChartRatingData.Create(chartDataToRate, RATING_METRICS);

        foreach (var metric in RATING_METRICS)
            Console.WriteLine($"{metric.Name}: {ratingData.Values[metric.Name]:F}");

        Console.WriteLine();
        Console.WriteLine($"Difficulty: {ratingData.Rate(ChartRatingModel.Empty, RATING_METRICS):F}");
    }

    private void DrawChart() {
        graphicsPanel.Clear();
        graphicsPanel.AddDrawable(new Grid(chartTop, chartBottom, 9, 10));
        graphicsPanel.AddDrawable(new Grid(graphTop, graphBottom, 10, 10));
        metricDrawables.Clear();
        pathDrawables.Clear();
            
        var notes = chartData.Notes;
            
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
        var data = new List<(string, double)>();
        string[] allPaths = FileHelper.GetAllSrtbs().ToArray();

        for (int i = 0; i < allPaths.Length; i++) {
            string path = allPaths[i];
                
            if (!TryLoadSrtb(path, out var srtb) || !TryGetTrackData(srtb, difficulty, out var trackData))
                continue;

            double diff = 0;
            string title = srtb.GetTrackInfo().Title;

            try {
                var chartData = ChartData.Create(NoteConversion.ToCustomNotesList(trackData.Notes));
                var ratingData = ChartRatingData.Create(chartData, RATING_METRICS);
                    
                diff = ratingData.Rate(ChartRatingModel.Empty, RATING_METRICS);
            }
            catch (Exception e) {
                Console.WriteLine($"Error scanning chart {title}:");
                Console.WriteLine(e.Message);
            }
                
            data.Add((title, diff));
            Console.WriteLine($"Scanned chart {i} of {allPaths.Length}");
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        Console.Write(new string(' ', Console.BufferWidth));
        Console.SetCursorPosition(0, Console.CursorTop);
        data.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        foreach ((string title, double diff) in data)
            Console.WriteLine($"{diff:F} - {title}");
            
        Console.WriteLine();
    }
        
    private static bool TryLoadSrtb(string name, out SRTB srtb) {
        if (!FileHelper.TryGetSrtbWithFileName(name, out string path)) {
            srtb = null;

            return false;
        }
            
        srtb = SRTB.DeserializeFromFile(path);

        return srtb != null;
    }

    private static bool TryGetTrackData(SRTB srtb, SRTB.DifficultyType difficulty, out SRTB.TrackData trackData) {
        trackData = srtb.GetTrackData(difficulty);

        return trackData != null;
    }
        
    private static bool TryParseDifficulty(string arg, out SRTB.DifficultyType difficulty) {
        difficulty = SRTB.DifficultyType.XD;

        if (string.IsNullOrWhiteSpace(arg) || Enum.TryParse(arg, true, out difficulty))
            return true;
            
        Console.WriteLine("Invalid difficulty string");

        return false;
    }

    private static bool TryGetMetric(string name, out Metric metric) {
        name = name.ToLowerInvariant();
            
        for (int i = 0; i < METRICS.Length; i++) {
            metric = METRICS[i];

            if (metric.Name.ToLowerInvariant() == name)
                return true;
        }

        metric = null;

        return false;
    }
}