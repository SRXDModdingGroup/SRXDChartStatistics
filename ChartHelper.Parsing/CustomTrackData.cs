using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using ChartData;
using ChartHelper.Types;

namespace ChartHelper {
    /// <summary>
    /// A container for a single track of a chart
    /// </summary>
    public class CustomTrackData {
        /// <summary>
        /// The difficulty type of the track
        /// </summary>
        public Difficulty DifficultyType { get; set; }
        
        /// <summary>
        /// The difficulty rating of the track
        /// </summary>
        public int DifficultyRating { get; set; }
        
        /// <summary>
        /// All of the notes contained within the track
        /// </summary>
        public List<Note> Notes { get; }

        /// <summary>
        /// Constructor. Generates custom track data from an srtb's TrackData
        /// </summary>
        /// <param name="trackData">TrackData from an srtb</param>
        public CustomTrackData(SRTB.TrackData trackData) {
            DifficultyType = (Difficulty) (trackData.DifficultyType - 2);
            DifficultyRating = trackData.DifficultyRating;
            Notes = new List<Note>();

            foreach (var note in trackData.Notes)
                Notes.Add(CreateNote(note.Time, note.Type, note.ColorIndex, note.Column, note.Size));

            Note.ApplyDetailedData(Notes);
        }

        /// <summary>
        /// Alters an srtb's TrackData using the values in this custom track data
        /// </summary>
        /// <param name="target">The SRTB TrackData to alter</param>
        public void ApplyToTrackData(SRTB.TrackData target) {
            target.DifficultyType = (int) DifficultyType + 2;
            target.DifficultyRating = DifficultyRating;

            var newNotes = new SRTB.Note[Notes.Count];

            for (int i = 0; i < Notes.Count; i++) {
                var note = Notes[i];

                newNotes[i] = new SRTB.Note {
                    Time = note.Time,
                    Type = (int) note.TypeRaw,
                    ColorIndex = (int) note.Color,
                    Column = note.Column,
                    Size = (int) note.CurveType
                };
            }

            target.Notes = newNotes;
        }
        
        private static Note CreateNote(float time, int type, int color = 0, int column = 0, int curveType = 0) =>
            new(time, Enum.IsDefined(typeof(NoteTypeRaw), type) ? (NoteTypeRaw) type : NoteTypeRaw.Experimental, (NoteColor) color, column, curveType == 0 ? CurveType.Cosine : (CurveType) curveType);
    }
}