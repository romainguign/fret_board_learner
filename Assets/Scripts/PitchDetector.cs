using UnityEngine;

[ExecuteInEditMode]
public class PitchDetector : MonoBehaviour
{
    private AudioClip micClip;
    private string device;
    private AudioSource audioSource;
    
    // Paramètres de détection
    private int sampleRate = 44100;
    private int bufferSize = 4096;
    private float[] audioBuffer;
    
    // Résultats publics
    [HideInInspector] public float detectedFrequency = 0f;
    [HideInInspector] public string detectedNote = "Aucune";
    [HideInInspector] public float confidence = 0f;
    
    // Smoothing et stabilité
    private float[] frequencyHistory = new float[5];
    private string lastNote = "Aucune";
    private int framesSinceChange = 0;
    private float minNoteEnergy = 0.0001f; // Seuil très bas pour détecter même les notes faibles

    // objet TextMesh pour afficher la note à l'écran (peut être bougé en édition)
    private TextMesh displayText;

    void Start()
    {
        // préparer l'affichage dès l'édition
        EnsureDisplayExists();

        if (!Application.isPlaying)
            return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("Aucun micro détecté !");
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        device = Microphone.devices[0];
        Debug.Log("Micro utilisé : " + device);

        // Démarrer l'enregistrement en boucle
        micClip = Microphone.Start(device, true, 1, sampleRate);
        audioSource.clip = micClip;
        audioSource.Play();
        
        audioBuffer = new float[bufferSize];
    }

    void Update()
    {
        // en édition on ne fait que préparer l'affichage
        if (!Application.isPlaying)
        {
            EnsureDisplayExists();
            return;
        }

        if (micClip == null)
            return;

        int position = Microphone.GetPosition(device);
        if (position > 0)
        {
            int startPos = Mathf.Max(0, position - bufferSize);
            micClip.GetData(audioBuffer, startPos);
            
            // Détecter la fréquence avec YIN amélioré
            DetectPitchYIN(audioBuffer);
            UpdateDisplay();
        }
    }

    private void DetectPitchYIN(float[] samples)
    {
        // Calculer l'énergie pour ignorer les bruits trop faibles
        float energy = CalculateEnergy(samples);
        
        if (energy < minNoteEnergy)
        {
            detectedFrequency = 0f;
            detectedNote = "Aucune";
            confidence = 0f;
            lastNote = "Aucune";
            return;
        }
        
        // Copie pour ne pas modifier le buffer original
        float[] processedSamples = new float[samples.Length];
        System.Array.Copy(samples, processedSamples, samples.Length);
        
        // Appliquer une fenêtre Hann pour réduire les artefacts
        ApplyHannWindow(processedSamples);
        
        // Normaliser les samples pour améliorer la détection
        NormalizeSamples(processedSamples);
        
        // Calculer la CMNDF (vraie algo YIN)
        float[] yinBuffer = ComputeYINBuffer(processedSamples);
        
        // Plage de fréquences pour guitare
        float minFreq = 50f;   // Bien en dessous du Mi2 pour capturer toutes les notes
        float maxFreq = 1500f; // Limite haute
        
        int minLag = (int)(sampleRate / maxFreq);
        int maxLag = (int)(sampleRate / minFreq);
        
        float threshold = 0.15f; // Seuil YIN moins strict
        int bestLag = FindPeakInYIN(yinBuffer, minLag, maxLag, threshold);
        
        if (bestLag > 0 && yinBuffer[bestLag] < threshold)
        {
            // Affinage du lag avec interpolation parabolique
            float refinedLag = RefineWithParabolic(yinBuffer, bestLag);
            float rawFrequency = sampleRate / refinedLag;
            
            // Smoothing temporal : moyenne de 5 derniers frames
            ShiftFrequencyHistory(rawFrequency);
            float smoothedFrequency = GetAverageFrequency();
            
            detectedFrequency = smoothedFrequency;
            confidence = Mathf.Clamp01(1f - yinBuffer[bestLag]);
            
            // Vérifier les erreurs d'octave : le détecteur peut parfois renvoyer une fréquence
            // deux fois trop basse (une octave en dessous). On ne corrige **que** si la fréquence
            // est vraiment faible, afin de ne pas transformer un E2 en E3.
            detectedNote = FrequencyToNote(detectedFrequency);
            int octave = ExtractOctave(detectedNote);
            // Si la fréquence est inférieure à ~60 Hz, on suppose un bug d'octave et on la multiplie
            // par 2 jusqu'à ce qu'elle soit dans une plage raisonnable.
            while (detectedFrequency < 60f && detectedFrequency * 2f < 2000f)
            {
                detectedFrequency *= 2f;
                detectedNote = FrequencyToNote(detectedFrequency);
                octave = ExtractOctave(detectedNote);
                Debug.Log($"Octave correction appliquée, nouvelle fréquence = {detectedFrequency:F1} Hz ({detectedNote})");
            }
            
            string newNote = detectedNote;
            
            // Validation : attendre 1 frame pour confirmer (réactivité meilleure)
            if (newNote != lastNote)
            {
                framesSinceChange++;
                if (framesSinceChange >= 1)
                {
                    lastNote = newNote;
                    detectedNote = newNote;
                    framesSinceChange = 0;
                }
            }
            else
            {
                framesSinceChange = 0;
                detectedNote = lastNote;
            }
            
            if (Time.frameCount % 15 == 0)
            {
                Debug.Log($"Note: {detectedNote} | Fréq: {detectedFrequency:F1} Hz | Confiance: {confidence:F2}");
            }
        }
        else
        {
            detectedFrequency = 0f;
            detectedNote = "Aucune";
            confidence = 0f;
            lastNote = "Aucune";
        }
    }

    private float CalculateEnergy(float[] samples)
    {
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / samples.Length);
    }

    private void NormalizeSamples(float[] samples)
    {
        float maxAbs = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            if (abs > maxAbs)
                maxAbs = abs;
        }
        
        if (maxAbs > 0.001f)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] /= maxAbs;
            }
        }
    }

    private float[] ComputeYINBuffer(float[] samples)
    {
        int bufferSize = samples.Length / 2;
        float[] yin = new float[bufferSize];
        
        yin[0] = 1f; // Par convention YIN
        float cumulativeEnergy = 0f;
        
        for (int tau = 1; tau < bufferSize; tau++)
        {
            float sum = 0f;
            for (int i = 0; i < samples.Length - tau; i++)
            {
                float delta = samples[i] - samples[i + tau];
                sum += delta * delta;
            }
            
            cumulativeEnergy += sum;
            
            if (cumulativeEnergy > 0.0001f)
            {
                yin[tau] = sum * tau / cumulativeEnergy;
            }
            else
            {
                yin[tau] = 1f;
            }
        }
        
        return yin;
    }

    private int FindPeakInYIN(float[] yin, int minLag, int maxLag, float threshold)
    {
        // Cherche le premier lag qui descend sous le seuil (plus fiable pour les octaves)
        for (int i = minLag; i < maxLag && i < yin.Length; i++)
        {
            if (yin[i] < threshold)
            {
                // Vérifier que c'est bien un minimum local
                if (i > 0 && yin[i] < yin[i - 1] && (i + 1 >= yin.Length || yin[i] < yin[i + 1]))
                {
                    return i;
                }
            }
        }
        
        // Fallback : chercher le minimum global dans la range
        int bestLag = minLag;
        float bestValue = yin[minLag];
        
        for (int i = minLag; i < maxLag && i < yin.Length; i++)
        {
            if (yin[i] < bestValue)
            {
                bestValue = yin[i];
                bestLag = i;
            }
        }
        
        return bestValue < threshold ? bestLag : 0;
    }

    private float RefineWithParabolic(float[] yin, int lag)
    {
        // Affinage parabolique pour meilleure précision
        if (lag < 1 || lag + 1 >= yin.Length)
            return (float)lag;
        
        float y1 = yin[lag - 1];
        float y2 = yin[lag];
        float y3 = yin[lag + 1];
        
        float a = (y3 - 2 * y2 + y1) / 2f;
        if (Mathf.Abs(a) < 0.0001f) return (float)lag;
        
        float b = (y3 - y1) / 2f;
        float offset = -b / (2f * a);
        
        return Mathf.Clamp(lag + offset, lag - 0.5f, lag + 0.5f);
    }

    private void ApplyHannWindow(float[] samples)
    {
        for (int i = 0; i < samples.Length; i++)
        {
            float window = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (samples.Length - 1)));
            samples[i] *= window;
        }
    }

    private void ShiftFrequencyHistory(float newFrequency)
    {
        for (int i = frequencyHistory.Length - 1; i > 0; i--)
        {
            frequencyHistory[i] = frequencyHistory[i - 1];
        }
        frequencyHistory[0] = newFrequency;
    }

    private float GetAverageFrequency()
    {
        float sum = 0f;
        int count = 0;
        
        for (int i = 0; i < frequencyHistory.Length; i++)
        {
            if (frequencyHistory[i] > 0)
            {
                sum += frequencyHistory[i];
                count++;
            }
        }
        
        return count > 0 ? sum / count : 0f;
    }

    private string FrequencyToNote(float frequency)
    {
        string[] notes = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };
        
        float refFreq = 440f; // La4
        float semitones = 12 * Mathf.Log(frequency / refFreq, 2f);
        int midiNote = Mathf.RoundToInt(semitones + 69); // 69 = La4 en MIDI
        
        int octave = (midiNote / 12) - 1;
        int noteIndex = midiNote % 12;
        
        if (noteIndex < 0) noteIndex += 12;
        
        return notes[noteIndex] + octave;
    }

    private int ExtractOctave(string note)
    {
        if (string.IsNullOrEmpty(note))
            return 0;
        // dernier caractère de la chaîne est l'octave
        char c = note[note.Length - 1];
        if (char.IsDigit(c))
            return c - '0';
        return 0;
    }

    // -----------------------------------
    // Display helpers
    // -----------------------------------
    private void EnsureDisplayExists()
    {
        if (displayText != null)
            return;

        // chercher un enfant existant
        Transform existing = transform.Find("PitchDisplay");
        if (existing != null)
        {
            displayText = existing.GetComponent<TextMesh>();
        }
        else
        {
            GameObject go = new GameObject("PitchDisplay");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            displayText = go.AddComponent<TextMesh>();
            displayText.fontSize = 48;
            displayText.anchor = TextAnchor.MiddleCenter;
            displayText.alignment = TextAlignment.Center;
            displayText.color = Color.green;
            displayText.text = "Aucune\n0Hz"; // placeholder
        }
    }

    private void UpdateDisplay()
    {
        if (displayText == null)
            return;

        displayText.text = $"{detectedNote}\n{detectedFrequency:F1}Hz";
        displayText.color = confidence > 0.7f ? Color.green : Color.red;
    }


}
