using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Scale
{
    public string name;
    public List<int> intervals; // Intervalles en demi-tons : ex [0,2,4,5,7,9,11] pour majeur
    
    public Scale(string name, int[] intervals)
    {
        this.name = name;
        this.intervals = new List<int>(intervals);
    }
}

public class ScalesLibrary
{
    public static string[] GetAllNotes()
    {
        return new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    }
    
    public static Dictionary<string, Scale> GetAllScales()
    {
        Dictionary<string, Scale> scales = new Dictionary<string, Scale>
        {
            { "Major", new Scale("Major", new int[] { 0, 2, 4, 5, 7, 9, 11 }) },
            { "Minor", new Scale("Minor", new int[] { 0, 2, 3, 5, 7, 8, 10 }) },
            { "Pentatonic Minor", new Scale("Pentatonic Minor", new int[] { 0, 3, 5, 7, 10 }) },
            { "Pentatonic Major", new Scale("Pentatonic Major", new int[] { 0, 2, 4, 7, 9 }) },
            { "Blues", new Scale("Blues", new int[] { 0, 3, 5, 6, 7, 10 }) },
            // { "Dorian", new Scale("Dorian", new int[] { 0, 2, 3, 5, 7, 9, 10 }) },
            // { "Lydian", new Scale("Lydian", new int[] { 0, 2, 4, 6, 7, 9, 11 }) },
            // { "Mixolydian", new Scale("Mixolydian", new int[] { 0, 2, 4, 5, 7, 9, 10 }) },
            // { "Harmonic Minor", new Scale("Harmonic Minor", new int[] { 0, 2, 3, 5, 7, 8, 11 }) },
            // { "Melodic Minor", new Scale("Melodic Minor", new int[] { 0, 2, 3, 5, 7, 9, 11 }) },
            // { "Whole Tone", new Scale("Whole Tone", new int[] { 0, 2, 4, 6, 8, 10 }) },
            // { "Chromatic", new Scale("Chromatic", new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }) },
        };
        return scales;
    }
}
