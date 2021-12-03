using System;
using System.IO;

namespace ChartData {
    public static class Program {
        public static void Main() {
            bool exit = false;
            
            do {
                Console.Write("Enter file name: ");

                string name = Console.ReadLine();

                if (!FileHelper.TryGetSrtbWithFileName(name, out string path)) {
                    Console.WriteLine($"Could not find file {path}");
                    
                    continue;
                }
                
                if (!ChartData.TryCreateFromFile(path, out var data)) {
                    Console.WriteLine($"Could not load file {path}");
                    
                    continue;
                }
                
                Console.WriteLine(data.Title);
                Console.WriteLine(data.Subtitle);
                Console.WriteLine(data.Artist);
                Console.WriteLine(data.Featuring);
                Console.WriteLine(data.Charter);
                Console.WriteLine();

                foreach (var pair in data.TrackData) {
                    Console.WriteLine(pair.Key);
                    Console.WriteLine(pair.Value.DifficultyRating);
                    Console.WriteLine();

                    foreach (var note in pair.Value.Notes) {
                        Console.WriteLine($"{note.Time}, {note.Type}, {note.Color}, {note.Column}, {note.CurveType}");
                    }
                    
                    Console.WriteLine();
                }
                
                Console.WriteLine("Press any key to continue, or 'e' to exit");
                Console.WriteLine();

                exit = Console.ReadKey().KeyChar == 'e';
            } while (!exit);
        }
    }
}