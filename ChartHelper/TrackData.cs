using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChartHelper {
    /// <summary>
    /// A container for a single track of a chart
    /// </summary>
    public class TrackData {
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
            int index = 0;
            int sustainedNoteStartIndex = -1;
            int lastHoldPointIndex = -1;
            int lastBeatIndex = -1;
            bool holding = false;
            bool spinning = false;
            bool beatHolding = false;
            bool readyToSnap = false;
			
            foreach (Match match in noteMatches) {
                var note = new Note(
                    float.Parse(match.Groups[1].Value, NumberFormatInfo.InvariantInfo),
                    int.Parse(match.Groups[2].Value), 
                    int.Parse(match.Groups[3].Value),
                    int.Parse(match.Groups[4].Value),
                    int.Parse(match.Groups[5].Value));
                bool skip = false;

                switch (note.TypeRaw) {
                    case NoteTypeRaw.SpinRight:
                    case NoteTypeRaw.SpinLeft:
                    case NoteTypeRaw.Scratch:
                        EndHold();
                        EndSpin(index, note);
                        sustainedNoteStartIndex = index;
                        spinning = true;
                        readyToSnap = true;

                        break;
                    case NoteTypeRaw.Beat:
                        lastBeatIndex = index;
                        beatHolding = true;

                        break;
                    case NoteTypeRaw.Tap:
                    case NoteTypeRaw.Match:
                        EndSpin(index, note);
                        
                        if (readyToSnap) {
                            note.IsAutoSnap = true;
                            readyToSnap = false;
                        }
                        
                        break;
                    case NoteTypeRaw.BeatRelease:
                        if (!beatHolding) {
                            skip = true;

                            break;
                        }
                        
                        beatHolding = false;
                        note.StartIndex = lastBeatIndex;

                        if (lastBeatIndex >= 0)
                            notes[lastBeatIndex].EndIndex = index;
                        
                        break;
                    case NoteTypeRaw.Hold:
                        EndHold();
                        EndSpin(index, note);
                        
                        if (readyToSnap) {
                            note.IsAutoSnap = true;
                            readyToSnap = false;
                        }
                        
                        holding = true;
                        sustainedNoteStartIndex = index;
                        lastHoldPointIndex = -1;

                        break;
                    case NoteTypeRaw.HoldPoint:
                        if (spinning) {
                            spinning = false;
                            note.Type = NoteType.SpinEnd;
                        }
                        else if (holding) {
                            lastHoldPointIndex = index;
                            note.Color = notes[sustainedNoteStartIndex].Color;
                        }
                        else {
                            skip = true;
                            
                            break;
                        }

                        note.StartIndex = sustainedNoteStartIndex;
                        
                        if (sustainedNoteStartIndex > -1)
                            notes[sustainedNoteStartIndex].EndIndex = index;
                        
                        sustainedNoteStartIndex = index;

                        break;
                }
                
                if (skip)
                    continue;
                
                notes.Add(note);
                index++;
            }

            EndHold();

            Notes = new ReadOnlyCollection<Note>(notes);

            void EndHold() {
                if (!holding || lastHoldPointIndex < 0)
                    return;
                
                var lastHoldPoint = notes[lastHoldPointIndex];
                    
                if (lastHoldPoint.CurveType == CurveType.CurveOut)
                    lastHoldPoint.Type = NoteType.Liftoff;
                else
                    lastHoldPoint.Type = NoteType.HoldEnd;

                holding = false;
            }

            void EndSpin(int i, Note note) {
                if (!spinning)
                    return;
                
                spinning = false;
                note.IsSpinEnd = true;
                
                if (sustainedNoteStartIndex > -1)
                    notes[sustainedNoteStartIndex].EndIndex = i;
            }
        }
    }
}