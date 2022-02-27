using System;
using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartHelper.Parsing;

/// <summary>
/// Utility class for converting between the srtb note type and the ChartHelper note type
/// </summary>
public static class NoteConversion {
    /// <summary>
    /// Converts a set of srtb notes to an array of ChartHelper notes
    /// </summary>
    /// <param name="notes">The set of srtb notes</param>
    /// <returns>An array of ChartHelper notes</returns>
    public static Note[] ToCustomNotesArray(IList<SRTB.Note> notes) {
        var newNotes = new Note[notes.Count];

        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];

            newNotes[i] = CreateCustomNote(note);
        }

        Note.ApplyDetailedData(newNotes);

        return newNotes;
    }
    
    /// <summary>
    /// Converts a set of srtb notes to a list of ChartHelper notes
    /// </summary>
    /// <param name="notes">The set of srtb notes</param>
    /// <returns>A list of ChartHelper notes</returns>
    public static List<Note> ToCustomNotesList(IList<SRTB.Note> notes) {
        var newNotes = new List<Note>();

        foreach (var note in notes)
            newNotes.Add(CreateCustomNote(note));

        Note.ApplyDetailedData(newNotes);

        return newNotes;
    }

    /// <summary>
    /// Converts a set of ChartHelper notes to an array of srtb notes
    /// </summary>
    /// <param name="notes">The set of ChartHelper notes</param>
    /// <returns>An array of srtb notes</returns>
    public static SRTB.Note[] ToBaseNotesArray(IList<Note> notes) {
        var newNotes = new SRTB.Note[notes.Count];

        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];

            newNotes[i] = CreateBaseNote(note);
        }
        
        return newNotes;
    }
    
    /// <summary>
    /// Converts a set of ChartHelper notes to a list of srtb notes
    /// </summary>
    /// <param name="notes">The set of ChartHelper notes</param>
    /// <returns>A list of srtb notes</returns>
    public static List<SRTB.Note> ToBaseNotesList(IList<Note> notes) {
        var newNotes = new List<SRTB.Note>();

        foreach (var note in notes)
            newNotes.Add(CreateBaseNote(note));

        return newNotes;
    }
    
    private static Note CreateCustomNote(SRTB.Note note) => new(
        note.Time,
        Enum.IsDefined(typeof(NoteTypeRaw), note.Type) ? (NoteTypeRaw) note.Type : NoteTypeRaw.Experimental,
        (NoteColor) note.ColorIndex,
        note.Column,
        note.Size == 0 ? CurveType.Cosine : (CurveType) note.Size);

    private static SRTB.Note CreateBaseNote(Note note) => new() {
        Time = note.Time,
        Type = (int) note.TypeRaw,
        ColorIndex = (int) note.Color,
        Column = note.Column,
        Size = (int) note.CurveType
    };
}