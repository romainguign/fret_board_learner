using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ToggleColorHandler : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text labelText;
    
    [Header("Couleurs du Fond")]
    [SerializeField] private Color bgNormalColor = Color.white;
    [SerializeField] private Color bgHighlightedColor = Color.cyan;
    [SerializeField] private Color bgPressedColor = Color.blue;
    [SerializeField] private Color bgSelectedColor = Color.green;
    
    [Header("Couleurs du Texte")]
    [SerializeField] private Color textNormalColor = Color.black;
    [SerializeField] private Color textHighlightedColor = Color.black;
    [SerializeField] private Color textPressedColor = Color.white;
    [SerializeField] private Color textSelectedColor = Color.white;
    
    private ColorBlock colors;
    private bool isHovered = false;

    void Start()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (labelText == null) labelText = GetComponentInChildren<Text>();
        
        // Setup du toggle standard
        toggle.targetGraphic = backgroundImage;
        colors = new ColorBlock
        {
            normalColor = bgNormalColor,
            highlightedColor = bgHighlightedColor,
            pressedColor = bgPressedColor,
            selectedColor = bgSelectedColor,
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        toggle.colors = colors;
        
        // Listeners
        toggle.onValueChanged.AddListener(OnToggleChanged);
        
        // Pour détecter le hover
        EventTrigger trigger = gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => OnPointerEnter());
        trigger.triggers.Add(pointerEnter);
        
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => OnPointerExit());
        trigger.triggers.Add(pointerExit);
        
        // Couleur initiale du texte
        UpdateTextColor();
    }
    
    void Update()
    {
        // Maintenir les couleurs du ColorBlock selon l'état du toggle
        if (backgroundImage != null && toggle != null)
        {
            ColorBlock cb = toggle.colors;
            
            if (toggle.isOn)
            {
                // Quand on, force les couleurs en mode "selected"
                cb.normalColor = bgSelectedColor;
                cb.highlightedColor = bgHighlightedColor;
                cb.pressedColor = bgPressedColor;
            }
            else
            {
                // Quand off, revenir aux couleurs normales
                cb.normalColor = bgNormalColor;
                cb.highlightedColor = bgHighlightedColor;
                cb.pressedColor = bgPressedColor;
            }
            
            toggle.colors = cb;
        }
    }
    
    private void OnPointerEnter()
    {
        isHovered = true;
        if (labelText != null && !toggle.isOn)
        {
            labelText.color = textHighlightedColor;
        }
    }
    
    private void OnPointerExit()
    {
        isHovered = false;
        UpdateTextColor();
    }
    
    private void OnToggleChanged(bool isOn)
    {
        UpdateTextColor();
    }
    
    private void UpdateTextColor()
    {
        if (labelText == null) return;
        
        if (toggle.isOn)
        {
            labelText.color = textSelectedColor;
        }
        else if (isHovered)
        {
            labelText.color = textHighlightedColor;
        }
        else
        {
            labelText.color = textNormalColor;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        if (labelText == null) labelText = GetComponentInChildren<Text>();
        
        // Mettre à jour prévisuellement la couleur du texte dans l'éditeur
        if (labelText != null && toggle != null)
        {
            if (toggle.isOn)
            {
                labelText.color = textSelectedColor;
            }
            else
            {
                labelText.color = textNormalColor;
            }
        }
    }
#endif
}

