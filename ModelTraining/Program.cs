using System;
using System.Collections.Generic;
using System.IO;
using ChartMetrics;

namespace ModelTraining; 

internal class Program {
    private static readonly Metric[] METRICS = {
        new Acceleration(),
        new MovementNoteDensity(),
        new OverallNoteDensity(),
        new RequiredMovement(),
        new SpinDensity(),
        new TapBeatDensity()
    };
    
    public static void Main(string[] args) {
        string projectDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;
        string resourcesDirectory = Path.Combine(projectDirectory, "Resources");
        string datasetsDirectory = Path.Combine(resourcesDirectory, "Datasets");
        var datasets = new List<Dataset>();
        var ratingMetrics = GetRatingMetrics();

        foreach (string directory in Directory.GetDirectories(datasetsDirectory)) {
            var dataset = Dataset.CreateFromDirectory(directory, ratingMetrics);
                
            if (dataset != null)
                datasets.Add(dataset);
                
            Console.WriteLine();
        }
            
        Console.WriteLine();
    }
        
    private static List<Metric> GetRatingMetrics() {
        var ratingMetrics = new List<Metric>();

        foreach (var metric in METRICS) {
            if (metric is not PointValue)
                ratingMetrics.Add(metric);
        }

        return ratingMetrics;
    }
}