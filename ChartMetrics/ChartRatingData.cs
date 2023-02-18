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
                sum += parameters.Coefficient * Math.Min(parameters.NormalizationFactor * values[metric.Name], 1d);
        }
        
        return sum;
    }

    public static ChartRatingData Create(ChartData chartData, IEnumerable<Metric> metrics, double plotResolution, int plotSmooth, double highQuantile) {
        var pointValue = new PointValue().Calculate(chartData);
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
        
        var weights = pointValue.GetPlot(startTime, endTime, plotResolution).Smooth(plotSmooth).Points;
        double weightSum = 0d;

        foreach (double weight in weights)
            weightSum += weight;

        var ratings = new SortedDictionary<string, double>();

        foreach (var metric in metrics) {
            pointValue = metric.Calculate(chartData);

            var plot = pointValue.GetPlot(startTime, endTime, plotResolution).Smooth(plotSmooth);
            var points = plot.Points;
            double high = plot.GetQuantile(highQuantile);
            double sum = 0d;

            for (int i = 0; i < points.Count && i < weights.Count; i++)
                sum += weights[i] * Math.Min(points[i], high);
                
            ratings.Add(metric.Name, sum / weightSum);
        }

        return new ChartRatingData(ratings);
    }
}