using System;
using System.Collections.Generic;
using System.IO;
using ChartHelper.Parsing;
using ChartMetrics;

namespace ModelTraining;

public class Dataset {
    public IReadOnlyList<DataElement> Elements => elements;

    private List<DataElement> elements;
    
    private Dataset(List<DataElement> elements) => this.elements = elements;

    public static Dataset CreateFromDirectory(string directory, List<Metric> metrics) {
        string orderPath = Path.Combine(directory, "order.txt");
        
        if (!File.Exists(orderPath))
            return null;

        var order = new List<int>();

        foreach (string line in File.ReadLines(orderPath)) {
            if (int.TryParse(line, out int id) && !order.Contains(id))
                order.Add(id);
        }

        var elements = new List<DataElement>(order.Count);

        for (int i = 0; i < order.Count; i++) {
            int id = order[i];
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
            var values = ChartRatingData.Create(chartData, metrics).Values;
            var ratingData = new List<double>(values.Count);

            foreach (var metric in metrics)
                ratingData.Add(values[metric.Name]);

            elements.Add(new DataElement(id, srtb.GetTrackInfo().Title, ratingData));
            Console.WriteLine($"Created rating data for chart {i} of {order.Count} in {directory}");
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        return new Dataset(elements);
    }
}