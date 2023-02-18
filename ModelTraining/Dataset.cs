using System;
using System.Collections.Generic;
using System.IO;
using ChartHelper.Parsing;
using ChartMetrics;
using Newtonsoft.Json;

namespace ModelTraining;

public class Dataset {
    [JsonProperty(PropertyName = "directory")]
    public string Directory { get; }
    
    [JsonProperty(PropertyName = "elements")]
    public List<DataElement> Elements { get; }
    
    public Dataset(string directory, List<DataElement> elements) {
        Directory = directory;
        Elements = elements;
    }

    public static Dataset CreateFromDirectory(string directory, IReadOnlyList<Metric> metrics, double plotResolution, int plotSmooth, double highQuantile) {
        string orderPath = Path.Combine(directory, "order.txt");
        
        if (!File.Exists(orderPath))
            return null;

        var order = new List<(int, int)>();

        foreach (string line in File.ReadLines(orderPath)) {
            string[] split = line.Split(' ');

            if (split.Length >= 2 && int.TryParse(split[0], out int id) && int.TryParse(split[1], out int diff))
                order.Add((id, diff));
        }

        var elements = new List<DataElement>(order.Count);

        for (int i = 0; i < order.Count; i++) {
            (int id, int diff) = order[i];
            var srtb = SRTB.DeserializeFromFile(Path.Combine(directory, $"{id}.srtb"));

            if (srtb == null) {
                Console.WriteLine($"{id} is not found");

                continue;
            }

            var trackData = srtb.GetTrackData(SRTB.DifficultyType.XD);

            if (trackData == null) {
                Console.WriteLine($"{id} does not have an XD difficulty");

                continue;
            }

            var chartData = ChartData.Create(NoteConversion.ToCustomNotesList(trackData.Notes));
            var values = ChartRatingData.Create(chartData, metrics, plotResolution, plotSmooth, highQuantile).Values;
            double[] ratingData = new double[metrics.Count];

            for (int j = 0; j < metrics.Count; j++)
                ratingData[j] = values[metrics[j].Name];

            elements.Add(new DataElement(id, srtb.GetTrackInfo().Title, diff, ratingData));
            Console.WriteLine($"Created rating data for chart {i + 1} of {order.Count} in {directory}");
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }
        
        Console.WriteLine();

        return new Dataset(directory, elements);
    }
}