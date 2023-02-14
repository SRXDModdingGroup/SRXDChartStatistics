using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChartMetrics;
using Newtonsoft.Json;

namespace ModelTraining; 

internal class Program {
    private const double PLOT_RESOLUTION = 10d;
    private const int METRIC_SMOOTH = 5;
    private const double HIGH_QUANTILE = 0.95d;
    
    private static readonly Metric[] METRICS = {
        new Acceleration(),
        new SpinDensity(),
        new TapBeatDensity()
    };
    
    public static void Main(string[] args) {
        string projectDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;
        string resourcesDirectory = Path.Combine(projectDirectory, "Resources");
        string datasetsDirectory = Path.Combine(resourcesDirectory, "Datasets");
        var datasets = new List<Dataset>();

        foreach (string directory in Directory.GetDirectories(datasetsDirectory)) {
            Dataset dataset;
            string cachePath = Path.Combine(directory, "cache.json");

            if (File.Exists(cachePath))
                dataset = JsonConvert.DeserializeObject<Dataset>(File.ReadAllText(cachePath));
            else
                dataset = Dataset.CreateFromDirectory(directory, METRICS, PLOT_RESOLUTION, METRIC_SMOOTH, HIGH_QUANTILE);

            if (dataset == null) {
                Console.WriteLine($"Failed to get dataset for directory {directory}");
                
                continue;
            }
            
            datasets.Add(dataset);
            // File.WriteAllText(cachePath, JsonConvert.SerializeObject(dataset));
            Console.WriteLine($"Successfully got dataset for directory {directory}");
        }

        double[] normalizationFactors = Dataset.Normalize(datasets, METRICS.Length);
        
        for (int i = 0; i < METRICS.Length; i++) {
            var metric = METRICS[i];

            Console.WriteLine($"Metric: {metric.Name}");
            Console.WriteLine();
            
            foreach (var element in datasets.SelectMany(dataset => dataset.Elements).OrderByDescending(element => element.RatingData[i]))
                Console.WriteLine($"{element.RatingData[i]:0.0000} - {element.Id} {element.Title}");
            
            Console.WriteLine();
        }
        
        var model = Training.Train(datasets, METRICS, 1000000, 1d);

        foreach ((var element, double rating) in
                 datasets.SelectMany(dataset => dataset.Elements)
                     .Select(element => (element, Training.Rate(element.RatingData, model)))
                     .OrderByDescending(pair => pair.Item2))
            Console.WriteLine($"{rating:0.0000} - {element.Id} {element.Title}");

        Console.WriteLine();
    }
}