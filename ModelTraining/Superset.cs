using System;
using System.Collections.Generic;
using System.IO;
using ChartMetrics;
using Newtonsoft.Json;

namespace ModelTraining; 

public class Superset {
    [JsonProperty(PropertyName = "datasets")]
    public List<Dataset> Datasets { get; }
    
    [JsonProperty(PropertyName = "metricCount")]
    public int MetricCount { get; }

    public Superset(List<Dataset> datasets, int metricCount) {
        Datasets = datasets;
        MetricCount = metricCount;
    }
    
    public double[] Normalize() {
        double maxDifficulty = 0d;
        double[] normalizationFactors = new double[MetricCount];

        foreach (var dataset in Datasets) {
            foreach (var element in dataset.Elements) {
                double[] ratingData = element.RatingData;

                if (element.Difficulty > maxDifficulty)
                    maxDifficulty = element.Difficulty;
                
                for (int i = 0; i < MetricCount; i++) {
                    double value = ratingData[i];

                    if (value > normalizationFactors[i])
                        normalizationFactors[i] = value;
                }
            }
        }
        
        foreach (var dataset in Datasets) {
            foreach (var element in dataset.Elements) {
                element.Difficulty /= maxDifficulty;
                
                double[] ratingData = element.RatingData;
                
                for (int i = 0; i < MetricCount; i++)
                    ratingData[i] /= normalizationFactors[i];
            }
        }
        
        for (int i = 0; i < MetricCount; i++)
            normalizationFactors[i] = 1d / normalizationFactors[i];

        return normalizationFactors;
    }

    public static Superset CreateFromDirectory(string directory, IReadOnlyList<Metric> metrics, double plotResolution, int plotSmooth, double highQuantile) {
        var datasets = new List<Dataset>();
        
        foreach (string subDirectory in Directory.GetDirectories(directory)) {
            var dataset = Dataset.CreateFromDirectory(subDirectory, metrics, plotResolution, plotSmooth, highQuantile);

            if (dataset == null) {
                Console.WriteLine($"Failed to get dataset for directory {subDirectory}");
                
                continue;
            }
            
            datasets.Add(dataset);
            Console.WriteLine($"Successfully got dataset for directory {subDirectory}");
        }

        return new Superset(datasets, metrics.Count);
    }
}