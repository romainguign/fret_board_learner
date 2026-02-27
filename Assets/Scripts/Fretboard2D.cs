using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Fretboard2D : MonoBehaviour
{
    // Configurations
    private int numFrets = 18;
    private string[] strings = { "E4", "B3", "G3", "D3", "A2", "E2" };
    
    private float fretSpacing = 1.2f;
    private float stringSpacing = 0.8f;
    private Vector2 startPosition = Vector2.zero;
    
    // Couleurs
    private Color defaultColor = Color.white;
    private Color detectedColor = Color.yellow;
    private Color rootNoteColor = new Color(1f, 0.5f, 0f); // Orange pour les notes racines
    private float colorFadeDuration = 0.15f;
    
    // Données
    private GameObject[,] noteObjects;
    private SpriteRenderer[,] noteRenderers;
    private Color[,] noteColors;
    private float[,] noteFrequencies;
    private string[,] noteNames;
    
    // Mode d'affichage
    [SerializeField]
    public bool ignoreOctave = false;
    
    // Gamme
    private Scale currentScale = null;
    private string currentRootNote = "C";
    private bool useScaleFilter = false;
    private Dictionary<string, Scale> allScales;
    
    // État
    private bool hasBeenCreated = false;
    private PitchDetector pitchDetector;

    void Start()
    {
        // En édition: créer une seule fois
        if (!Application.isPlaying && !hasBeenCreated && transform.Find("Notes") == null)
        {
            CreateFretboard();
            hasBeenCreated = true;
            return;
        }

        if (!Application.isPlaying)
            return;

        // Chercher ou créer le fretboard
        Transform container = transform.Find("Notes");
        
        if (container == null)
        {
            CreateFretboard();
        }
        
        InitializeFromChildren();
        pitchDetector = FindFirstObjectByType<PitchDetector>();
        allScales = ScalesLibrary.GetAllScales();
        
        if (noteNames == null || noteNames.Length == 0)
        {
            Debug.LogError("noteNames est NULL ou vide!");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Save Fretboard")]
    public void SaveFretboard()
    {
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(gameObject.scene);
    }
#endif

    private void CreateFretboard()
    {
        if (hasBeenCreated)
            return;

        hasBeenCreated = true;
        
        // Initialiser les arrays
        noteObjects = new GameObject[strings.Length, numFrets];
        noteRenderers = new SpriteRenderer[strings.Length, numFrets];
        noteColors = new Color[strings.Length, numFrets];
        noteFrequencies = new float[strings.Length, numFrets];
        noteNames = new string[strings.Length, numFrets];

        // Notes ouvertes en Hz
        float[] openFreq = { 329.63f, 246.94f, 196.00f, 146.83f, 110.00f, 82.41f };

        // Créer conteneur
        GameObject container = new GameObject("Notes");
        container.transform.SetParent(transform);
        container.transform.localPosition = Vector3.zero;

        // Calculer position de départ pour centrer
        float width = (numFrets - 1) * fretSpacing;
        float height = (strings.Length - 1) * stringSpacing;
        startPosition = new Vector2(-width / 2f, height / 2f);

        // Créer toutes les notes
        for (int s = 0; s < strings.Length; s++)
        {
            for (int f = 0; f < numFrets; f++)
            {
                // Position
                float x = startPosition.x + f * fretSpacing;
                float y = startPosition.y - s * stringSpacing;

                // GameObject
                GameObject noteGO = new GameObject($"{strings[s]}_F{f}");
                noteGO.transform.SetParent(container.transform);
                noteGO.transform.localPosition = new Vector3(x, y, 0);

                // Sprite
                SpriteRenderer sr = noteGO.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite();
                sr.color = defaultColor;

                // Texte avec le nom de la note (enfant séparé pour éviter conflit MeshRenderer/SpriteRenderer)
                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(noteGO.transform);
                textGO.transform.localPosition = Vector3.zero;
                
                TextMesh tm = textGO.AddComponent<TextMesh>();
                tm.text = GetNoteNameEnglish(s, f);
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontSize = 20;
                tm.color = Color.black;
                tm.characterSize = 0.1f;

                // Données
                noteObjects[s, f] = noteGO;
                noteRenderers[s, f] = sr;
                noteColors[s, f] = defaultColor;
                noteFrequencies[s, f] = openFreq[s] * Mathf.Pow(2f, f / 12f);
                noteNames[s, f] = GetNoteNameEnglish(s, f);
            }
        }

    }

    private void InitializeFromChildren()
    {
        Transform container = transform.Find("Notes");
        if (container == null)
        {
            Debug.LogError("Conteneur Notes non trouvé");
            return;
        }

        int expected = strings.Length * numFrets;
        if (container.childCount != expected)
        {
            Debug.LogError($"Enfants trouvés: {container.childCount}, attendu: {expected}");
            return;
        }

        noteObjects = new GameObject[strings.Length, numFrets];
        noteRenderers = new SpriteRenderer[strings.Length, numFrets];
        noteColors = new Color[strings.Length, numFrets];
        noteFrequencies = new float[strings.Length, numFrets];
        noteNames = new string[strings.Length, numFrets];

        float[] openFreq = { 329.63f, 246.94f, 196.00f, 146.83f, 110.00f, 82.41f };

        int idx = 0;
        for (int s = 0; s < strings.Length; s++)
        {
            for (int f = 0; f < numFrets; f++)
            {
                Transform child = container.GetChild(idx);
                noteObjects[s, f] = child.gameObject;

                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    noteRenderers[s, f] = sr;
                    noteColors[s, f] = sr.color;

                    // Régénérer sprite si nécessaire
                    if (sr.sprite == null)
                        sr.sprite = CreateCircleSprite();
                }

                noteFrequencies[s, f] = openFreq[s] * Mathf.Pow(2f, f / 12f);
                noteNames[s, f] = GetNoteNameEnglish(s, f);
                idx++;
            }
        }

    }

    void Update()
    {
        if (!Application.isPlaying || pitchDetector == null || noteRenderers == null)
            return;

        UpdateNoteColors();
    }

    private void UpdateNoteColors()
    {
        // Fade toutes les notes vers blanc
        for (int s = 0; s < strings.Length; s++)
        {
            for (int f = 0; f < numFrets; f++)
            {
                if (noteRenderers[s, f] == null)
                    continue;

                if (noteColors[s, f] != defaultColor)
                {
                    noteColors[s, f] = Color.Lerp(
                        noteColors[s, f],
                        defaultColor,
                        Time.deltaTime / colorFadeDuration
                    );
                    noteRenderers[s, f].color = noteColors[s, f];
                }
            }
        }

        // Colorer les notes détectées
        if (pitchDetector.detectedFrequency > 0 && pitchDetector.confidence > 0.4f)
        {
            int matchCount = 0;
            string detectedBase = StripOctave(pitchDetector.detectedNote);

            for (int s = 0; s < strings.Length; s++)
            {
                for (int f = 0; f < numFrets; f++)
                {
                    if (noteRenderers[s, f] == null)
                        continue;

                    bool isMatch = false;

                    if (ignoreOctave)
                    {
                        string strippedNoteName = StripOctave(noteNames[s, f]);
                        isMatch = strippedNoteName == detectedBase;
                    }
                    else
                    {
                        // Comparer la fréquence exacte
                        float diff = Mathf.Abs(Mathf.Log(pitchDetector.detectedFrequency / noteFrequencies[s, f], 2f));
                        isMatch = diff < 1f / 24f;
                    }

                    if (isMatch)
                    {
                        noteColors[s, f] = detectedColor;
                        noteRenderers[s, f].color = detectedColor;
                        matchCount++;
                    }
                }
            }

        }
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2.2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color col = Color.white;
                if (Vector2.Distance(new Vector2(x, y), center) > radius)
                    col.a = 0;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    private string GetNoteName(int stringIdx, int fretIdx)
    {
        string[] notes = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };
        int[] openNotes = { 4, 11, 7, 2, 9, 4 };
        int[] openOctaves = { 4, 3, 3, 3, 2, 2 };

        int noteIdx = (openNotes[stringIdx] + fretIdx) % 12;
        int octave = openOctaves[stringIdx] + (openNotes[stringIdx] + fretIdx) / 12;

        return notes[noteIdx] + octave;
    }

    private string GetNoteNameEnglish(int stringIdx, int fretIdx)
    {
        string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int[] openNotes = { 4, 11, 7, 2, 9, 4 };
        int[] openOctaves = { 4, 3, 3, 3, 2, 2 };

        int noteIdx = (openNotes[stringIdx] + fretIdx) % 12;
        int octave = openOctaves[stringIdx] + (openNotes[stringIdx] + fretIdx) / 12;

        return notes[noteIdx] + octave;
    }

    private string StripOctave(string note)
    {
        if (string.IsNullOrEmpty(note))
            return note;
        int i = note.Length - 1;
        while (i >= 0 && char.IsDigit(note[i]))
            i--;
        return note.Substring(0, i + 1);
    }

    public void SetIgnoreOctave(bool value)
    {
        ignoreOctave = value;
    }

    public void SetScale(string scaleName)
    {
        if (allScales != null && allScales.ContainsKey(scaleName))
        {
            currentScale = allScales[scaleName];
            UpdateScaleDisplay();
        }
        else
        {
            Debug.LogWarning($"Gamme '{scaleName}' non trouvée");
        }
    }

    public void SetUseScaleFilter(bool use)
    {
        useScaleFilter = use;
        UpdateScaleDisplay();
    }

    private bool IsNoteInScale(string noteName)
    {
        if (currentScale == null || !useScaleFilter)
            return true;

        // Enlever l'octave de la note
        string baseNote = StripOctave(noteName);
        
        // Mappage des notes anglaises aux intervalles
        Dictionary<string, int> noteToInterval = new Dictionary<string, int>
        {
            { "C", 0 }, { "C#", 1 }, { "Db", 1 },
            { "D", 2 }, { "D#", 3 }, { "Eb", 3 },
            { "E", 4 },
            { "F", 5 }, { "F#", 6 }, { "Gb", 6 },
            { "G", 7 }, { "G#", 8 }, { "Ab", 8 },
            { "A", 9 }, { "A#", 10 }, { "Bb", 10 },
            { "B", 11 }
        };

        if (!noteToInterval.ContainsKey(baseNote))
            return true;

        int interval = noteToInterval[baseNote];
        int rootInterval = noteToInterval[currentRootNote];
        
        // Calculer l'intervalle relatif à la note de base de la gamme
        int relativeInterval = (interval - rootInterval + 12) % 12;
        
        return currentScale.intervals.Contains(relativeInterval);
    }

    private void UpdateScaleDisplay()
    {
        if (noteRenderers == null)
            return;

        for (int s = 0; s < noteRenderers.GetLength(0); s++)
        {
            for (int f = 0; f < noteRenderers.GetLength(1); f++)
            {
                if (noteRenderers[s, f] == null || noteObjects[s, f] == null)
                    continue;

                bool shouldShow = IsNoteInScale(noteNames[s, f]);
                noteObjects[s, f].SetActive(shouldShow);
                
                if (shouldShow)
                {
                    // Vérifier si c'est une note racine
                    string baseNote = StripOctave(noteNames[s, f]);
                    if (baseNote == currentRootNote)
                    {
                        noteRenderers[s, f].color = rootNoteColor;
                    }
                    else
                    {
                        noteRenderers[s, f].color = defaultColor;
                    }
                }
            }
        }
    }

    public string[] GetAvailableScales()
    {
        if (allScales == null)
        {
            allScales = ScalesLibrary.GetAllScales();
        }
        
        string[] scaleNames = new string[allScales.Count];
        allScales.Keys.CopyTo(scaleNames, 0);
        return scaleNames;
    }

    public string[] GetAvailableRootNotes()
    {
        return ScalesLibrary.GetAllNotes();
    }

    public void SetRootNote(string note)
    {
        currentRootNote = note;
        UpdateScaleDisplay();
    }
}
