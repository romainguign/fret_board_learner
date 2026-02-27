using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoBehaviour
{
    private Fretboard2D fretboard;
    
    [SerializeField] private Toggle octaveToggle;
    [SerializeField] private Transform rootNoteContainer;
    [SerializeField] private Transform scaleContainer;
    [SerializeField] private GameObject togglePrefab;
    
    private ToggleGroup scaleToggleGroup;
    private List<Toggle> rootNoteToggles = new List<Toggle>();
    private List<Toggle> scaleToggles = new List<Toggle>();
    private Toggle selectedRootNoteToggle = null;
    private Toggle selectedScaleToggle = null;
    private bool isUpdatingRootNoteToggle = false;
    private bool isUpdatingScaleToggle = false;

    void Start()
    {
        // Chercher le Fretboard2D
        fretboard = FindFirstObjectByType<Fretboard2D>();
        if (fretboard == null)
        {
            Debug.LogError("Fretboard2D introuvable !");
            return;
        }

        // Setup octave toggle
        if (octaveToggle != null)
        {
            octaveToggle.onValueChanged.AddListener(OnOctaveToggleChanged);
        }
        else
        {
            Debug.LogWarning("Toggle Octave non assigné dans l'Inspector");
        }

        // Créer le ToggleGroup pour les gammes
        if (scaleContainer != null)
        {
            scaleToggleGroup = scaleContainer.GetComponent<ToggleGroup>();
            if (scaleToggleGroup == null)
            {
                scaleToggleGroup = scaleContainer.gameObject.AddComponent<ToggleGroup>();
            }
        }

        // Créer les toggles visuels
        if (rootNoteContainer != null)
        {
            CreateRootNoteToggles();
        }
        else
        {
            Debug.LogWarning("Root Note Container non assigné dans l'Inspector");
        }

        if (scaleContainer != null)
        {
            CreateScaleToggles();
        }
        else
        {
            Debug.LogWarning("Scale Container non assigné dans l'Inspector");
        }
    }

    private void OnOctaveToggleChanged(bool isChecked)
    {
        fretboard.SetIgnoreOctave(isChecked);
    }

    private void CreateRootNoteToggles()
    {
        rootNoteToggles.Clear();
        
        // Récupérer tous les toggles enfants existants
        Toggle[] existingToggles = rootNoteContainer.GetComponentsInChildren<Toggle>();
        Toggle referenceToggle = null;
        
        foreach (Toggle toggle in existingToggles)
        {
            rootNoteToggles.Add(toggle);
            toggle.group = null;  // Pas de ToggleGroup
            string noteName = toggle.gameObject.name;
            toggle.onValueChanged.AddListener((isOn) => OnRootNoteToggleChanged(isOn, noteName, toggle));
            if (referenceToggle == null)
                referenceToggle = toggle;
        }

        string[] notes = fretboard.GetAvailableRootNotes();
        
        // Créer les toggles manquants
        foreach (string note in notes)
        {
            // Vérifier si ce toggle existe déjà
            bool exists = false;
            foreach (Toggle toggle in rootNoteToggles)
            {
                if (toggle.gameObject.name == note)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                GameObject toggleGO = new GameObject(note);
                toggleGO.transform.SetParent(rootNoteContainer, false);
                
                // Si on a un toggle de référence, copier son style
                if (referenceToggle != null)
                {
                    CopyToggleStyle(referenceToggle, toggleGO);
                }
                else
                {
                    // Sinon, créer avec style par défaut
                    CreateDefaultToggleStyle(toggleGO);
                }
                
                Toggle toggle = toggleGO.GetComponent<Toggle>();
                toggle.group = null;  // Pas de ToggleGroup
                toggle.isOn = false;
                
                string noteValue = note;
                toggle.onValueChanged.AddListener((isOn) => OnRootNoteToggleChanged(isOn, noteValue, toggle));
                
                rootNoteToggles.Add(toggle);
            }
        }

        // Pas de sélection par défaut - tout le clavier est visible
        // (la logique est : clique = sélectionne, reclique = tout affiche)
    }

    private void CreateScaleToggles()
    {
        scaleToggles.Clear();
        
        // Récupérer tous les toggles enfants existants
        Toggle[] existingToggles = scaleContainer.GetComponentsInChildren<Toggle>();
        Toggle referenceToggle = null;
        
        foreach (Toggle toggle in existingToggles)
        {
            scaleToggles.Add(toggle);
            toggle.group = null;  // Pas de ToggleGroup
            string scaleName = toggle.gameObject.name;
            toggle.onValueChanged.AddListener((isOn) => OnScaleToggleChanged(isOn, scaleName, toggle));
            if (referenceToggle == null)
                referenceToggle = toggle;
        }

        string[] scales = fretboard.GetAvailableScales();
        
        // Créer les toggles manquants
        foreach (string scale in scales)
        {
            // Vérifier si ce toggle existe déjà
            bool exists = false;
            foreach (Toggle toggle in scaleToggles)
            {
                if (toggle.gameObject.name == scale)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                GameObject toggleGO = new GameObject(scale);
                toggleGO.transform.SetParent(scaleContainer, false);
                
                // Si on a un toggle de référence, copier son style
                if (referenceToggle != null)
                {
                    CopyToggleStyle(referenceToggle, toggleGO);
                }
                else
                {
                    // Sinon, créer avec style par défaut
                    CreateDefaultToggleStyle(toggleGO);
                }
                
                Toggle toggle = toggleGO.GetComponent<Toggle>();
                toggle.group = null;  // Pas de ToggleGroup
                toggle.isOn = false;
                
                // Mettre à jour le texte avec le nom de la gamme
                Text textComponent = toggleGO.GetComponentInChildren<Text>();
                if (textComponent != null)
                    textComponent.text = scale;
                
                string scaleValue = scale;
                toggle.onValueChanged.AddListener((isOn) => OnScaleToggleChanged(isOn, scaleValue, toggle));
                
                scaleToggles.Add(toggle);
            }
        }

        // Pas de sélection par défaut
    }

    private void CopyToggleStyle(Toggle source, GameObject target)
    {
        // Copier Image
        Image sourceImage = source.GetComponent<Image>();
        if (sourceImage != null)
        {
            Image targetImage = target.AddComponent<Image>();
            targetImage.sprite = sourceImage.sprite;
            targetImage.color = sourceImage.color;
            targetImage.type = sourceImage.type;
        }

        // Copier LayoutElement
        LayoutElement sourceLayout = source.GetComponent<LayoutElement>();
        if (sourceLayout != null)
        {
            LayoutElement targetLayout = target.AddComponent<LayoutElement>();
            targetLayout.preferredWidth = sourceLayout.preferredWidth;
            targetLayout.preferredHeight = sourceLayout.preferredHeight;
        }

        // Copier Toggle (sauf isOn et group qui seront set après)
        Toggle sourceToggle = source.GetComponent<Toggle>();
        Toggle targetToggle = target.AddComponent<Toggle>();
        targetToggle.targetGraphic = target.GetComponent<Image>();
        targetToggle.transition = sourceToggle.transition;
        targetToggle.colors = sourceToggle.colors;

        // Copier RectTransform si elle existe
        RectTransform sourceRect = source.GetComponent<RectTransform>();
        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (sourceRect != null && targetRect != null)
        {
            targetRect.sizeDelta = sourceRect.sizeDelta;
        }

        // Copier le texte enfant
        Text sourceText = source.GetComponentInChildren<Text>();
        if (sourceText != null)
        {
            // Chercher ou créer un enfant Text
            Transform existingText = target.transform.Find("Text");
            GameObject textGO;
            if (existingText != null)
            {
                textGO = existingText.gameObject;
            }
            else
            {
                textGO = new GameObject("Text");
                textGO.transform.SetParent(target.transform, false);
            }

            Text targetText = textGO.GetComponent<Text>();
            if (targetText == null)
                targetText = textGO.AddComponent<Text>();

            targetText.font = sourceText.font;
            targetText.fontSize = sourceText.fontSize;
            targetText.alignment = sourceText.alignment;
            targetText.color = sourceText.color;
            targetText.text = "";

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
        }
    }

    private void CreateDefaultToggleStyle(GameObject toggleGO)
    {
        // Créer avec style par défaut
        LayoutElement layout = toggleGO.AddComponent<LayoutElement>();
        layout.preferredWidth = 100;
        layout.preferredHeight = 40;
        
        Image image = toggleGO.AddComponent<Image>();
        image.color = new Color(0.7f, 0.7f, 0.7f);
        
        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.targetGraphic = image;
        
        // Ajouter du texte
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(toggleGO.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void OnRootNoteToggleChanged(bool isOn, string note, Toggle toggle)
    {
        if (isUpdatingRootNoteToggle)
            return;

        isUpdatingRootNoteToggle = true;

        if (isOn)
        {
            // Si un autre toggle était sélectionné, le désélectionner
            if (selectedRootNoteToggle != null && selectedRootNoteToggle != toggle)
            {
                selectedRootNoteToggle.isOn = false;
            }
            
            // Sélectionner ce toggle
            selectedRootNoteToggle = toggle;
            fretboard.SetRootNote(note);
            
            // Activer le filtrage par scale si une scale est sélectionnée
            if (selectedScaleToggle != null)
            {
                fretboard.SetUseScaleFilter(true);
            }
            else
            {
                // Pas de scale sélectionnée = afficher toutes les notes
                fretboard.SetUseScaleFilter(false);
            }
        }
        else
        {
            // Si on a cliqué sur le toggle sélectionné (reclique intentionnel pour déselectionner)
            if (selectedRootNoteToggle == toggle)
            {
                selectedRootNoteToggle = null;
                toggle.isOn = false;  // Force la déselection
                // Afficher toutes les notes
                fretboard.SetUseScaleFilter(false);
            }
            else
            {
                // Clic accidentel dans le vide - réactiver le toggle sélectionné
                if (selectedRootNoteToggle != null)
                {
                    selectedRootNoteToggle.isOn = true;
                }
            }
        }

        isUpdatingRootNoteToggle = false;
    }

    private void OnScaleToggleChanged(bool isOn, string scale, Toggle toggle)
    {
        if (isUpdatingScaleToggle)
            return;

        isUpdatingScaleToggle = true;

        if (isOn)
        {
            // Si un autre toggle était sélectionné, le désélectionner
            if (selectedScaleToggle != null && selectedScaleToggle != toggle)
            {
                selectedScaleToggle.isOn = false;
            }
            
            // Sélectionner cette scale
            selectedScaleToggle = toggle;
            fretboard.SetScale(scale);
            
            // Ne filtrer que si une root note est aussi sélectionnée
            if (selectedRootNoteToggle != null)
            {
                fretboard.SetUseScaleFilter(true);
            }
            else
            {
                // Pas de root note = afficher toutes les notes
                fretboard.SetUseScaleFilter(false);
            }
        }
        else
        {
            // Si on a cliqué sur le toggle sélectionné (reclique intentionnel pour déselectionner)
            if (selectedScaleToggle == toggle)
            {
                selectedScaleToggle = null;
                toggle.isOn = false;  // Force la déselection
                // Afficher toutes les notes
                fretboard.SetUseScaleFilter(false);
            }
            else
            {
                // Clic accidentel dans le vide - réactiver le toggle sélectionné
                if (selectedScaleToggle != null)
                {
                    selectedScaleToggle.isOn = true;
                }
            }
        }

        isUpdatingScaleToggle = false;
    }
}
