using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;

namespace ChartHelper {
    /// <summary>
    /// A helper class for finding and interacting with .srtb files
    /// </summary>
    public static class FileHelper {
        private static readonly Regex MATCH_SETTINGS = new Regex(@"(.+?) *= *(.+)");
        private static readonly Regex MATCH_SRTB = new Regex(@"\w+\.srtb");
        
        private static string customPath;
        /// <summary>
        /// The current custom path, either the default custom path or one specified in Settings.txt
        /// </summary>
        public static string CustomPath {
            get {
                if ((!string.IsNullOrWhiteSpace(customPath) || Settings.TryGetValue("CustomPath", out customPath)) && Directory.Exists(customPath))
                    return customPath;

                customPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Super Spin Digital\Spin Rhythm XD\Custom");
                    
                if (!Directory.Exists(customPath))
                    customPath = null;

                return customPath;
            }
        }

        private static ReadOnlyDictionary<string, string> settings;
        /// <summary>
        /// A dictionary of values provided by Settings.txt, if it exists
        /// </summary>
        public static ReadOnlyDictionary<string, string> Settings {
            get {
                if (settings != null)
                    return settings;
                
                string thisDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string settingsPath = Path.Combine(thisDirectory, "Settings.txt");
                
                var dict = new Dictionary<string, string>();
                
                if (File.Exists(settingsPath)) {
                    var contents = File.ReadAllText(settingsPath).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in contents) {
                        if (line[0] == '#')
                            continue;

                        var match = MATCH_SETTINGS.Match(line);
                        
                        if (!match.Success)
                            continue;
                        
                        dict.Add(match.Groups[1].Value, match.Groups[2].Value);
                    }
                }

                settings = new ReadOnlyDictionary<string, string>(dict);

                return settings;
            }
        }

        /// <summary>
        /// Returns true if the specified path or filename represents an srtb file
        /// </summary>
        /// <param name="path">The path or filename to test</param>
        /// <returns>True if it is a .srtb file</returns>
        public static bool IsSrtb(string path) => MATCH_SRTB.IsMatch(Path.GetFileName(path));

        /// <summary>
        /// Attempts to find the full path of an srtb file with the specified name
        /// </summary>
        /// <param name="name">The name of the srtb file to find, with or without the .srtb extension</param>
        /// <param name="path">The resulting file path</param>
        /// <returns>True if a valid path was found</returns>
        public static bool TryGetSrtbWithFileName(string name, out string path) {
            if (!IsSrtb(name))
                name += ".srtb";

            path = Path.Combine(CustomPath, name);

            return File.Exists(path);
        }

        /// <summary>
        /// Gets all of the .srtb paths in the current custom folder
        /// </summary>
        /// <returns>An enumeration of all .srtb files</returns>
        public static IEnumerable<string> GetAllSrtbs(string path = "") {
            if (string.IsNullOrWhiteSpace(path))
                path = CustomPath;
            
            foreach (string file in Directory.GetFiles(path)) {
                if (IsSrtb(file))
                    yield return file;
            }
        }
    }
}