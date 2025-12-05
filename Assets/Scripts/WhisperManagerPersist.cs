using UnityEngine;
using Whisper;
using Whisper.Utils;

// Класс нужен для инициализации whisper в меню, а не на сцене с выступлением
public class WhisperManagerPersist : MonoBehaviour
{
    private static WhisperManagerPersist instance;

    [Header("Whisper Manager")]
    public WhisperManager whisperManager;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (whisperManager != null)
            {
                DontDestroyOnLoad(whisperManager.gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
            if (whisperManager != null)
            {
                Destroy(whisperManager.gameObject);
            }
        }
    }

    public static WhisperManager GetWhisperManager()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<WhisperManagerPersist>();
        }
        return instance?.whisperManager;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}