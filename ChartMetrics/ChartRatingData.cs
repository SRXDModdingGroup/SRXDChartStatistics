using System;
using System.Collections.Generic;

namespace ChartMetrics; 

public class ChartRatingData {
    public IReadOnlyDictionary<string, double> Values => values;

    private SortedDictionary<string, double> values;

    public ChartRatingData(SortedDictionary<string, double> values) => this.values = values;

    public double Rate(ChartRatingModel model, IEnumerable<Metric> metrics) {
        double sum = 0d;
        var parametersPerMetric = model.ParametersPerMetric;

        foreach (var metric in metrics) {
            if (parametersPerMetric.TryGetValue(metric.Name, out var parameters))
                sum += Math.Pow(parameters.Coefficient * values[metric.Name], parameters.Power);
        }
        
        return sum;
    }

    public static ChartRatingData Create(ChartData chartData, IEnumerable<Metric> metrics) {
        var result = new PointValue().Calculate(chartData);
        var notes = chartData.Notes;
        double startTime;
        double endTime;

        if (notes.Count == 0) {
            startTime = 0d;
            endTime = 0d;
        }
        else {
            startTime = notes[0].Time;
            endTime = notes[notes.Count - 1].Time;
        }
        
        var weights = result.GetPlot(startTime, endTime, 100d).Smooth(100).Points;
        double weightSum = 0d;

        foreach (double weight in weights)
            weightSum += weight;

        var ratings = new SortedDictionary<string, double>();

        foreach (var metric in metrics) {
            result = metric.Calculate(chartData);

            var plot = result.GetPlot(startTime, endTime, 100d).Smooth(100);
            var points = plot.Points;
            double high = plot.GetQuantile(0.9d);
            double sum = 0d;

            for (int i = 0; i < points.Count && i < weights.Count; i++)
                sum += weights[i] * Math.Min(points[i], high);
                
            ratings.Add(metric.Name, sum * 100d / weightSum);
        }

        return new ChartRatingData(ratings);
    }
}