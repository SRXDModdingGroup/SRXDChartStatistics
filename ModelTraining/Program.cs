using System;
using System.Collections.Generic;
using System.IO;
using ChartMetrics;
using Newtonsoft.Json;

namespace ModelTraining; 

internal class Program {
    private const double PLOT_RESOLUTION = 10d;
    private const int PLOT_SMOOTH = 5;
    private const double HIGH_QUANTILE = 0.95d;
    
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

        foreach (string directory in Directory.GetDirectories(datasetsDirectory)) {
            Dataset dataset;
            string cachePath = Path.Combine(directory, "cache.json");

            if (File.Exists(cachePath))
                dataset = JsonConvert.DeserializeObject<Dataset>(File.ReadAllText(cachePath));
            else
                dataset = Dataset.CreateFromDirectory(directory, METRICS, PLOT_RESOLUTION, PLOT_SMOOTH, HIGH_QUANTILE);

            if (dataset == null) {
                Console.WriteLine($"Failed to get dataset for directory {directory}");
                
                continue;
            }
            
            datasets.Add(dataset);
            // File.WriteAllText(cachePath, JsonConvert.SerializeObject(dataset));
            Console.WriteLine($"Successfully got dataset for directory {directory}");
        }

        double[] normalizationFactors = Dataset.Normalize(datasets, METRICS.Length);
        var model = Training.Train(datasets, normalizationFactors, METRICS, 1000000, 0.5d);
        
        Console.WriteLine();
    }
}