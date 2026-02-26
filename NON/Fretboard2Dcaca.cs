using UnityEngine;

[ExecuteInEditMode]
public class Fretboard2D : MonoBehaviour
{
#if UNITY_EDITOR
    // helper for prefab creation
    private static void EnsureFolderExists(string path)
    {
        if (!UnityEditor.AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path);
            string newFolder = System.IO.Path.GetFileName(path);
            UnityEditor.AssetDatabase.CreateFolder(parent, newFolder);
        }
    }
#endif
    // Référence au PitchDetector
    private PitchDetector pitchDetector;
    
    // Frettes et cordes
    private int numFrets = 12;
    private string[] strings = { "E4", "B3", "G3", "D3", "A2", "E2" }; // Cordes standard
    
    // GameObjects pour chaque note
    private GameObject[,] noteObjects; // [corde, frette]
    private SpriteRenderer[,] noteRenderers;
    private Color[,] noteColors;
    private float[,] noteFrequencies; // store frequency for each cell
    private string[,] noteBaseNames;   // note sans octave (Do, Ré, ...)

    // mode de coloration
    [SerializeField]
    private bool ignoreOctave = false; // true = all 'Mi' light up regardless of octave
    
    // Espacement et position
    private float fretSpacing = 1.2f;
    private float stringSpacing = 0.8f;
    private Vector2 startPosition = Vector2.zero;
    
    // Couleurs
    private Color defaultColor = Color.white;
    private Color detectedColor = Color.yellow;
    private float colorFadeDuration = 0.15f;

    private bool hasBeenCreated = false;

    void Start()
    {
        // En édition: créer la grille une seule fois si elle n'existe pas
        if (!Application.isPlaying && !hasBeenCreated && transform.childCount == 0)
        {
            CreateFretboard2D();
            hasBeenCreated = true;
            return;
        }

        // Au runtime: vérifier que le fretboard est complet, sinon le recréer
        if (Application.isPlaying)
        {
            Transform container = transform.Find("Notes");
            
            // Si le conteneur n'existe pas ou n'a pas le bon nombre d'enfants, recréer
            if (container == null || container.childCount != strings.Length * numFrets)
            {
                Debug.LogWarning($"Fretboard incomplet (attendu {strings.Length * numFrets}, trouvé {(container != null ? container.childCount : 0)}), recréation...");
                
                // Nettoyer l'ancien
                if (container != null)
                    DestroyImmediate(container.gameObject);
                    
                // Recréer
                CreateFretboard2D();
            }

            InitializeFromExistingChildren();
            pitchDetector = FindObjectOfType<PitchDetector>();
            if (pitchDetector == null)
            {
                Debug.LogError("PitchDetector introuvable !");
            }
        }
    }

    private void InitializeFromExistingChildren()
    {
        // find notes container if exists
        Transform container = transform.Find("Notes");
        if (container == null)
        {
            Debug.LogError("Conteneur Notes non trouvé dans FretboardManager2D");
            return;
        }

        if (container.childCount != strings.Length * numFrets)
        {
            Debug.LogError($"Nombre d'enfants dans Notes ({container.childCount}) ne correspond pas au nombre de notes attendu ({strings.Length * numFrets})");
            return;
        }

        noteObjects = new GameObject[strings.Length, numFrets];
        noteRenderers = new SpriteRenderer[strings.Length, numFrets];
        noteColors = new Color[strings.Length, numFrets];
        noteFrequencies = new float[strings.Length, numFrets];
        noteBaseNames = new string[strings.Length, numFrets];

        // notes ouvertes en Hz
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
                    
                    // Régénérer le sprite s'il est None (cas du préfab)
                    if (sr.sprite == null)
                    {
                        sr.sprite = CreateCircleSprite();
                        Debug.Log($"Sprite régénéré pour {child.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"SpriteRenderer manquant sur {child.name}");
                }

                noteFrequencies[s, f] = openFreq[s] * Mathf.Pow(2f, f / 12f);
                noteBaseNames[s, f] = StripOctave(GetNoteAtFret(s, f));
                idx++;
            }
        }
        Debug.Log($"Fretboard initialisé: {strings.Length * numFrets} notes trouvées");
    }



    private void EnsureFretboardExists()
    {
        // Never touch at runtime
        if (Application.isPlaying)
            return;

        // If already created or there are children, do nothing
        if (hasBeenCreated || transform.childCount > 0)
            return;

        CreateFretboard2D();
        hasBeenCreated = true;
    }

#if UNITY_EDITOR
    // appelée à chaque modification dans l'inspecteur, permet de reconstruire si besoin
    private void OnValidate()
    {
        EnsureFretboardExists();
    }
#endif

    void Update()
    {
        // en édition on n'applique que la coloration si on est en play
        if (!Application.isPlaying)
            return;

        if (pitchDetector == null)
            return;

        UpdateNoteColors();
    }

    private void CreateFretboard2D()
    {
        // Initialiser les arrays
        noteObjects = new GameObject[strings.Length, numFrets];
        noteRenderers = new SpriteRenderer[strings.Length, numFrets];
        noteColors = new Color[strings.Length, numFrets];
        noteFrequencies = new float[strings.Length, numFrets];
        noteBaseNames = new string[strings.Length, numFrets];

        // notes de base en Hz pour chaque corde
        float[] openFreq = { 329.63f, 246.94f, 196.00f, 146.83f, 110.00f, 82.41f }; // E4, B3, G3, D3, A2, E2

        // créer conteneur
        GameObject notesContainer = new GameObject("Notes");
        notesContainer.transform.SetParent(transform);
        notesContainer.transform.localPosition = Vector3.zero;

        // Créer les cercles pour chaque note
        for (int stringIdx = 0; stringIdx < strings.Length; stringIdx++)
        {
            for (int fretIdx = 0; fretIdx < numFrets; fretIdx++)
            {
                try
                {
                    // Position dans le monde
                    float x = startPosition.x + fretIdx * fretSpacing;
                    float y = startPosition.y - stringIdx * stringSpacing; // Inverser Y pour que E2 soit en bas
                    
                    // Créer un GameObject avec SpriteRenderer
                    GameObject noteObj = new GameObject($"{strings[stringIdx]}_F{fretIdx}");
                    noteObj.transform.SetParent(notesContainer.transform);
                    noteObj.transform.localPosition = new Vector3(x, y, 0);
                    
                    SpriteRenderer spriteRenderer = noteObj.AddComponent<SpriteRenderer>();
                    
                    // Créer un sprite blanc en code
                    Sprite circleSprite = CreateCircleSprite();
                    spriteRenderer.sprite = circleSprite;
                    spriteRenderer.color = defaultColor;
                    
                    // Ajouter du texte pour afficher le nom de la note
                    TextMesh textMesh = noteObj.AddComponent<TextMesh>();
                    textMesh.text = GetNoteAtFret(stringIdx, fretIdx);
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.fontSize = 20;
                    textMesh.color = Color.black;
                    textMesh.characterSize = 0.1f;
                    
                    noteObjects[stringIdx, fretIdx] = noteObj;
                    noteRenderers[stringIdx, fretIdx] = spriteRenderer;
                    noteColors[stringIdx, fretIdx] = defaultColor;

                    // calculer fréquence de la note
                    noteFrequencies[stringIdx, fretIdx] = openFreq[stringIdx] * Mathf.Pow(2f, fretIdx / 12f);
                    noteBaseNames[stringIdx, fretIdx] = StripOctave(GetNoteAtFret(stringIdx, fretIdx));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Erreur création note [{stringIdx},{fretIdx}]: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        Debug.Log("Fretboard créé avec " + (strings.Length * numFrets) + " notes");

#if UNITY_EDITOR
        // sauvegarder un prefab dans un dossier 'Assets/Fretboards' pour réutilisation
        string folder = "Assets/Fretboards";
        EnsureFolderExists(folder);
        string prefabPath = $"{folder}/Fretboard_{name}.prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, prefabPath, UnityEditor.InteractionMode.UserAction);
        Debug.Log($"Prefab du fretboard enregistré : {prefabPath}");
#endif
    }

    private Sprite CreateCircleSprite()
    {
        // Créer une texture blanche
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2.2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color col = Color.white;
                
                // Distance du centre
                float dist = Vector2.Distance(new Vector2(x, y), center);
                
                if (dist > radius)
                {
                    col.a = 0; // Transparent en dehors du cercle
                }
                
                texture.SetPixel(x, y, col);
            }
        }
        
        texture.Apply();
        
        // Créer le sprite
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        return sprite;
    }

    private void UpdateNoteColors()
    {
        if (pitchDetector == null || noteRenderers == null)
            return;

        // Fade tous les cercles vers le blanc
        for (int stringIdx = 0; stringIdx < strings.Length; stringIdx++)
        {
            for (int fretIdx = 0; fretIdx < numFrets; fretIdx++)
            {
                SpriteRenderer renderer = noteRenderers[stringIdx, fretIdx];
                if (renderer == null)
                    continue; // pas de renderer, on saute

                if (noteColors[stringIdx, fretIdx] != defaultColor)
                {
                    noteColors[stringIdx, fretIdx] = Color.Lerp(
                        noteColors[stringIdx, fretIdx],
                        defaultColor,
                        Time.deltaTime / colorFadeDuration
                    );
                    renderer.color = noteColors[stringIdx, fretIdx];
                }
            }
        }

        // Colorer les cercles selon le mode choisi
        if (pitchDetector.detectedFrequency > 0 && pitchDetector.confidence > 0.4f)
        {
            string detectedBase = StripOctave(pitchDetector.detectedNote);
            int matchCount = 0;
            for (int stringIdx = 0; stringIdx < strings.Length; stringIdx++)
            {
                for (int fretIdx = 0; fretIdx < numFrets; fretIdx++)
                {
                    SpriteRenderer renderer = noteRenderers[stringIdx, fretIdx];
                    if (renderer == null)
                        continue;

                    bool hit = false;
                    if (ignoreOctave)
                    {
                        hit = noteBaseNames[stringIdx, fretIdx] == detectedBase;
                    }
                    else
                    {
                        float targetFreq = noteFrequencies[stringIdx, fretIdx];
                        float diff = Mathf.Abs(Mathf.Log(pitchDetector.detectedFrequency / targetFreq, 2f));
                        hit = diff < 1f/24f;
                    }

                    if (hit)
                    {
                        noteColors[stringIdx, fretIdx] = detectedColor;
                        renderer.color = detectedColor;
                        matchCount++;
                    }
                }
            }
            
            if (Time.frameCount % 30 == 0 && matchCount > 0)
            {
                Debug.Log($"Freq détectée: {pitchDetector.detectedFrequency:F1} Hz => {matchCount} cercle(s) allumé(s) | {pitchDetector.detectedNote} (mode {(ignoreOctave?"toutes octaves":"octave uniquement")})");
                for (int si = 0; si < strings.Length; si++)
                {
                    for (int fi = 0; fi < numFrets; fi++)
                    {
                        float targetFreq = noteFrequencies[si, fi];
                        float diff = Mathf.Abs(Mathf.Log(pitchDetector.detectedFrequency / targetFreq, 2f));
                        if (diff < 1f/24f)
                        {
                            string noteOnFret = GetNoteAtFret(si, fi);
                            Debug.Log($"  -> {noteOnFret} ({targetFreq:F1}Hz) at {noteObjects[si, fi].transform.position}");
                        }
                    }
                }
            }
        }
    }

    private string GetNoteAtFret(int stringIdx, int fretIdx)
    {
        // Notes en ordre chromatique
        string[] chromatic = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };
        
        // Notes ouvertes de chaque corde
        int[] openNoteIndices = { 4, 11, 7, 2, 9, 4 }; // E, B, G, D, A, E (en index chromatic)
        int[] openOctaves = { 4, 3, 3, 3, 2, 2 };
        
        int noteIdx = (openNoteIndices[stringIdx] + fretIdx) % 12;
        int octave = openOctaves[stringIdx] + (openNoteIndices[stringIdx] + fretIdx) / 12;
        
        return chromatic[noteIdx] + octave;
    }

    // retire l'octave (chiffre final) d'une note, p.ex. "Mi4" -> "Mi"
    private string StripOctave(string note)
    {
        if (string.IsNullOrEmpty(note))
            return note;
        int i = note.Length - 1;
        while (i >= 0 && char.IsDigit(note[i]))
            i--;
        return note.Substring(0, i + 1);
    }

    // Méthode publique pour changer le mode ignoreOctave depuis l'UI
    public void SetIgnoreOctave(bool value)
    {
        ignoreOctave = value;
    }
}
