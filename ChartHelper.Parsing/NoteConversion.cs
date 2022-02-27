using System;
using System.Collections.Generic;
using ChartHelper.Types;

namespace ChartHelper.Parsing;

public static class NoteConversion {
    public static Note[] ToCustomNotesArray(IList<SRTB.Note> notes) {
        var newNotes = new Note[notes.Count];

        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];

            newNotes[i] = CreateCustomNote(note);
        }

        Note.ApplyDetailedData(newNotes);

        return newNotes;
    }
    
    public static List<Note> ToCustomNotesList(IList<SRTB.Note> notes) {
        var newNotes = new List<Note>();

        foreach (var note in notes)
            newNotes.Add(CreateCustomNote(note));

        Note.ApplyDetailedData(newNotes);

        return newNotes;
    }

    public static SRTB.Note[] ToBaseNotesArray(IList<Note> notes) {
        var newNotes = new SRTB.Note[notes.Count];

        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];

            newNotes[i] = CreateBaseNote(note);
        }
        
        return newNotes;
    }
    
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