using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private Fretboard2D fretboard;
    private Toggle octaveToggle;

    void Start()
    {
        // Chercher le Fretboard2D
        fretboard = FindFirstObjectByType<Fretboard2D>();
        if (fretboard == null)
        {
            Debug.LogError("Fretboard2D introuvable !");
            return;
        }

        // Chercher le Toggle dans le Canvas
        octaveToggle = FindFirstObjectByType<Toggle>();
        if (octaveToggle == null)
        {
            Debug.LogError("Toggle introuvable !");
            return;
        }

        // Ajouter un listener pour quand le toggle change
        octaveToggle.onValueChanged.AddListener(OnOctaveToggleChanged);
    }

    private void OnOctaveToggleChanged(bool isChecked)
    {
        fretboard.SetIgnoreOctave(isChecked);
        Debug.Log($"Mode ignoreOctave: {(isChecked ? "ON (toutes octaves)" : "OFF (octave exact)")}");
    }
}
