using UnityEngine;
using UnityEngine.UI;

public class FretboardUI : MonoBehaviour
{
    // Référence au PitchDetector
    private PitchDetector pitchDetector;
    
    // Frettes et cordes
    private int numFrets = 12;
    private string[] strings = { "E4", "B3", "G3", "D3", "A2", "E2" }; // Cordes standard
    
    // Buttons pour chaque note
    private Button[,] noteButtons; // [corde, frette]
    private Color originalColor = Color.white;
    private Color detectedColor = Color.yellow;
    private float colorFadeDuration = 0.1f;
    private Color[,] buttonColors;
    
    // Container pour les buttons
    private GridLayoutGroup gridLayout;

    void Start()
    {
        // Trouver le PitchDetector dans la scène
        pitchDetector = FindObjectOfType<PitchDetector>();
        if (pitchDetector == null)
        {
            Debug.LogError("PitchDetector introuvable ! Assurez-vous qu'il existe dans la scène.");
            return;
        }

        CreateFretboard();
    }

    void Update()
    {
        // Colorer la note détectée
        UpdateNoteColors();
    }

    private void CreateFretboard()
    {
        // Créer un Canvas pour afficher le fretboard
        GameObject canvasObj = new GameObject("FretboardCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // Créer un GridLayout pour les notes
        GameObject gridObj = new GameObject("FretboardGrid");
        gridObj.transform.SetParent(canvasObj.transform);
        
        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 60);
        grid.spacing = new Vector2(5, 5);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = numFrets + 1;
        
        RectTransform gridRect = gridObj.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;
        gridRect.sizeDelta = new Vector2((numFrets + 1) * 85, strings.Length * 65);

        // Initialiser les arrays
        noteButtons = new Button[strings.Length, numFrets];
        buttonColors = new Color[strings.Length, numFrets];

        // Créer les buttons pour chaque note
        for (int stringIdx = 0; stringIdx < strings.Length; stringIdx++)
        {
            for (int fretIdx = 0; fretIdx < numFrets; fretIdx++)
            {
                // Créer le button
                GameObject buttonObj = new GameObject($"{strings[stringIdx]}_Fret{fretIdx}");
                buttonObj.transform.SetParent(gridObj.transform);
                
                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = originalColor;
                
                Button button = buttonObj.AddComponent<Button>();
                button.interactable = false; // Désactiver les click
                
                Text buttonText = CreateButtonText(buttonObj, $"F{fretIdx}");
                
                noteButtons[stringIdx, fretIdx] = button;
                buttonColors[stringIdx, fretIdx] = originalColor;
            }
        }
    }

    private Text CreateButtonText(GameObject buttonObj, string text)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 14;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.black;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return textComponent;
    }

    private void UpdateNoteColors()
    {
        // Réinitialiser toutes les couleurs
        for (int stringIdx = 0; stringIdx < strings.Length; stringIdx++)
        {
            for (int fretIdx = 0; fretIdx < numFrets; fretIdx++)
            {
                // Fade de la couleur détectée vers blanc
                if (buttonColors[stringIdx, fretIdx] != originalColor)
                {
                    buttonColors[stringIdx, fretIdx] = Color.Lerp(
                        buttonColors[stringIdx, fretIdx],
                        originalColor,
                        Time.deltaTime / colorFadeDuration
                    );
                    noteButtons[stringIdx, fretIdx].image.color = buttonColors[stringIdx, fretIdx];
                }
            }
        }

        // Colorer la note détectée
        if (!string.IsNullOrEmpty(pitchDetector.detectedNote) && pitchDetector.detectedNote != "Aucune")
        {
            for (int stringIdx = 0; stringIdx < strings.Length; stringIdx++)
            {
                for (int fretIdx = 0; fretIdx < numFrets; fretIdx++)
                {
                    string noteOnFret = GetNoteAtFret(stringIdx, fretIdx);
                    
                    if (noteOnFret == pitchDetector.detectedNote && pitchDetector.confidence > 0.5f)
                    {
                        buttonColors[stringIdx, fretIdx] = detectedColor;
                        noteButtons[stringIdx, fretIdx].image.color = detectedColor;
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
}
