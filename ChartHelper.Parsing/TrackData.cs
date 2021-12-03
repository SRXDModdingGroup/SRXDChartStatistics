using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using ChartHelper.Types;

namespace ChartHelper {
    /// <summary>
    /// A container for a single track of a chart
    /// </summary>
    public class TrackData {
        private static readonly Dictionary<int, NoteTypeRaw> NOTE_TYPE_VALUES = new Dictionary<int, NoteTypeRaw> {
            { 0, NoteTypeRaw.Match },
            { 1, NoteTypeRaw.Beat },
            { 2, NoteTypeRaw.SpinRight },
            { 3, NoteTypeRaw.SpinLeft },
            { 4, NoteTypeRaw.Hold },
            { 5, NoteTypeRaw.HoldPoint },
            { 8, NoteTypeRaw.Tap },
            { 11, NoteTypeRaw.BeatRelease },
            { 12, NoteTypeRaw.Scratch }
        };

        private static readonly Regex MATCH_METADATA = new Regex(@"\\""difficultyRating\\"":(\d+),.*?,\\""difficultyType\\"":(\d+)");
        private static readonly Regex MATCH_NOTE_DATA
            = new Regex(@"\{\\""time\\"":(\d+\.?\d*),\\""type\\"":(\d+),\\""colorIndex\\"":(\d+),\\""column\\"":(-?\d+),\\""m_size\\"":(\d+)\}");

        /// <summary>
        /// The difficulty type of the track
        /// </summary>
        public Difficulty DifficultyType { get; private set; }
        /// <summary>
        /// The difficulty rating of the track
        /// </summary>
        public int DifficultyRating { get; private set; }
        /// <summary>
        /// All of the notes contained within the track
        /// </summary>
        public ReadOnlyCollection<Note> Notes { get; private set; }

        internal static bool TryCreate(string srtbData, out TrackData data) {
            data = new TrackData();
            
            var diffMatch = MATCH_METADATA.Match(srtbData);

            if (!int.TryParse(diffMatch.Groups[1].Value, out int difficultyRating)
                || !int.TryParse(diffMatch.Groups[2].Value, out int difficultyType)
                || !Enum.IsDefined(typeof(Difficulty), difficultyType - 2))
                return false;

            data.DifficultyRating = difficultyRating;
            data.DifficultyType = (Difficulty) (difficultyType - 2);

            return true;
        }

        internal void GenerateNoteData(string srtbData) {
            var noteMatches = MATCH_NOTE_DATA.Matches(srtbData);
            var notes = new List<Note>();
			
            foreach (Match match in noteMatches) {
                notes.Add(CreateNote(
                    float.Parse(match.Groups[1].Value, NumberFormatInfo.InvariantInfo),
                    int.Parse(match.Groups[2].Value), 
                    int.Parse(match.Groups[3].Value),
                    int.Parse(match.Groups[4].Value),
                    int.Parse(match.Groups[5].Value)));
            }

            Note.ApplyDetailedData(notes);
            Notes = new ReadOnlyCollection<Note>(notes);
        }

        private static Note CreateNote(float time, int type, int color = 0, int column = 0, int curveType = 0) =>
            new Note(time, NOTE_TYPE_VALUES.TryGetValue(type, out var newType) ? newType : NoteTypeRaw.Experimental, (NoteColor) color, column, curveType == 0 ? CurveType.Cosine : (CurveType) curveType);
    }
}