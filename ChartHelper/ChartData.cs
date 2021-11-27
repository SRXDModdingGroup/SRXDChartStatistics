using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;

namespace ChartHelper {
    /// <summary>
    /// A container for all of the useful data in a chart
    /// </summary>
    public class ChartData {
        private static readonly Difficulty[] ALL_DIFFICULTIES = {
            Difficulty.Easy,
            Difficulty.Normal,
            Difficulty.Hard,
            Difficulty.Expert,
            Difficulty.XD
        };
        private static readonly Regex MATCH_STRING_VALUES = new Regex(@"\{""key"":""(SO_.+?)"",""val"":""(.+?)"",""loadedGenerationId"":1\}");
        private static readonly Regex MATCH_TRACK_INFO
            = new Regex(@"\\""artistName\\"":\\""(.*?)\\"",\\""featArtists\\"":\\""(.*?)\\"",\\""title\\"":\\""(.*?)\\"",\\""subtitle\\"":\\""(.*?)\\"",.+?\\""charter\\"":\\""(.*?)\\"",.+?\\""difficulties\\"":\[(\{.*?\},?)*\]");
        private static readonly Regex MATCH_DIFFICULTY_ACTIVE = new Regex(@"\\""_active\\"":(true|false)");
        
        /// <summary>
        /// Attempts to generate a chart data container from the text within an srtb file
        /// </summary>
        /// <param name="text">The text to parse</param>
        /// <param name="data">The resulting data</param>
        /// <param name="difficulties">The difficulties to include in the data. Leave empty to include all difficulties</param>
        /// <returns>True if the data was successfully created</returns>
        public static bool TryCreateFromText(string text, out ChartData data, params Difficulty[] difficulties) {
            if (difficulties.Length == 0)
                difficulties = ALL_DIFFICULTIES;
            
            if (!TryGetSrtbData(text, out var srtbData) || !srtbData.TryGetValue("SO_TrackInfo_TrackInfo", out string toMatch)) {
                data = null;

                return false;
            }

            var match = MATCH_TRACK_INFO.Match(toMatch);

            if (!match.Success) {
                data = null;

                return false;
            }

            var groups = match.Groups;
            string artist = groups[1].Value;
            string featuring = groups[2].Value;
            string title = groups[3].Value;
            string subtitle = groups[4].Value;
            string charter = groups[5].Value;
            var difficultyCaptures = groups[6].Captures;
            var trackData = new Dictionary<Difficulty, TrackData>();
            var difficultiesHash = new HashSet<Difficulty>(difficulties);
            
            for (int i = 0; i < 5 && srtbData.TryGetValue($"SO_TrackData_TrackData_{i}", out string value); i++) {
                var matchDifficulty = MATCH_DIFFICULTY_ACTIVE.Match(difficultyCaptures[i].Value);

                if (!matchDifficulty.Success || matchDifficulty.Groups[1].Value != "true"
                    || !ChartHelper.TrackData.TryCreate(value, out var newTrackData))
                    continue;

                if (!difficultiesHash.Contains(newTrackData.DifficultyType))
                    continue;

                newTrackData.GenerateNoteData(value);
                trackData.Add(newTrackData.DifficultyType, newTrackData);
            }

            if (trackData.Count == 0) {
                data = null;
                
                return false;
            }

            data = new ChartData(title, subtitle, artist, featuring, charter, trackData);

            return true;
        }
        /// <summary>
        /// Attempts to generate a chart data container from the text within an srtb file
        /// </summary>
        /// <param name="text">The text to parse</param>
        /// <param name="data">The resulting data</param>
        /// <returns>True if the data was successfully created</returns>
        public static bool TryCreateFromText(string text, out ChartData data) => TryCreateFromText(text, out data, ALL_DIFFICULTIES);
        
        /// <summary>
        /// Attempts to generate a chart data container from an srtb file
        /// </summary>
        /// <param name="path">The path of the file to parse</param>
        /// <param name="data">The resulting data</param>
        /// <param name="difficulties">The difficulties to include in the data. Leave empty to include all difficulties</param>
        /// <returns>True if the data was successfully created</returns>
		public static bool TryCreateFromFile(string path, out ChartData data, params Difficulty[] difficulties) {
            if (File.Exists(path) || FileHelper.TryGetSrtbWithFileName(path, out path))
                return TryCreateFromText(File.ReadAllText(path), out data, difficulties);

            data = null;
				
            return false;
        }
        /// <summary>
        /// Attempts to generate a chart data container from an srtb file
        /// </summary>
        /// <param name="path">The path of the file to parse</param>
        /// <param name="data">The resulting data</param>
        /// <returns>True if the data was successfully created</returns>
        public static bool TryCreateFromFile(string path, out ChartData data) => TryCreateFromFile(path, out data, ALL_DIFFICULTIES);

        private static bool TryGetSrtbData(string srtbContent, out Dictionary<string, string> data) {
            var matches = MATCH_STRING_VALUES.Matches(srtbContent);
            
            data = new Dictionary<string, string>(matches.Count);

            for (int i = 0; i < matches.Count; i++)
                data[matches[i].Groups[1].Value] = matches[i].Groups[2].Value;

            return true;
        }

        /// <summary>
        /// The title of the chart
        /// </summary>
        public string Title { get; }
        /// <summary>
        /// The subtitle of the chart
        /// </summary>
        public string Subtitle { get; }
        /// <summary>
        /// The artist of the chart
        /// </summary>
        public string Artist { get; }
        /// <summary>
        /// The featured artist of the chart
        /// </summary>
        public string Featuring { get; }
        /// <summary>
        /// The charter of the chart
        /// </summary>
        public string Charter { get; }
        /// <summary>
        /// Track data for each difficulty
        /// </summary>
        public ReadOnlyDictionary<Difficulty, TrackData> TrackData { get; }

        private ChartData(string title, string subtitle, string artist, string featuring, string charter, Dictionary<Difficulty, TrackData> trackData) {
            Title = title;
            Subtitle = subtitle;
            Artist = artist;
            Featuring = featuring;
            Charter = charter;
            TrackData = new ReadOnlyDictionary<Difficulty, TrackData>(trackData);
        }
    }
}