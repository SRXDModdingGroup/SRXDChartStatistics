using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChartMetrics;
using Newtonsoft.Json;

namespace ModelTraining; 

internal static class Program {
    private const double PLOT_RESOLUTION = 10d;
    private const int PLOT_SMOOTH = 5;
    private const double HIGH_QUANTILE = 0.95d;
    
    private static readonly Metric[] METRICS = {
        new Acceleration(),
        new RequiredMovement(),
        new SpinDensity(),
        new TapBeatDensity()
    };
    
    public static async Task Main(string[] args) {
        string projectDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;
        string resourcesDirectory = Path.Combine(projectDirectory, "Resources");
        string datasetsDirectory = Path.Combine(resourcesDirectory, "Datasets");
        var superset = Superset.CreateFromDirectory(datasetsDirectory, METRICS, PLOT_RESOLUTION, PLOT_SMOOTH, HIGH_QUANTILE);
        double[] normalizationFactors = superset.Normalize();
        var datasets = superset.Datasets;
        
        for (int i = 0; i < METRICS.Length; i++) {
            var metric = METRICS[i];

            Console.WriteLine($"Metric: {metric.Name}");
            Console.WriteLine();
            
            foreach (var element in datasets.SelectMany(dataset => dataset.Elements).OrderByDescending(element => element.RatingData[i]))
                Console.WriteLine($"{element.RatingData[i]:0.0000} - {element.Id} {element.Title}");
            
            Console.WriteLine();
        }

        var cts = new CancellationTokenSource();
        var task = Task.Run(() => Train(superset, cts.Token), cts.Token);

        while (!task.IsCompleted && (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter)) { }
        
        cts.Cancel();

        double[] best = await task;

        foreach ((var element, double rating) in
                 datasets.SelectMany(dataset => dataset.Elements)
                     .Select(element => (element, Training.Rate(best, element)))
                     .OrderByDescending(pair => pair.Item2))
            Console.WriteLine($"{rating:0.0000} - {element.Id} {element.Title}");

        Console.WriteLine();
        Console.WriteLine($"Cost: {Training.CalculateCost(best, superset):0.00000}");

        for (int j = 0; j < METRICS.Length; j++)
            Console.WriteLine($"{METRICS[j].Name}: Coeff = {best[j]:0.00000}");
    }

    private static double[] Train(Superset superset, CancellationToken ct) => Training.TrainGradient(superset, 10000, 0.01d, 0.0001d, ct);
    // private static double[] Train(Superset superset, CancellationToken ct) => Training.TrainGenetic(superset, 10000, 0.01d, ct);
}