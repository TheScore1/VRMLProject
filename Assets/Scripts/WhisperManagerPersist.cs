using UnityEngine;
using Whisper;
using Whisper.Utils;

public class WhisperManagerPersist : MonoBehaviour
{
    private static WhisperManagerPersist instance;

    [Header("Whisper Manager")]
    public WhisperManager whisperManager;

    void Awake()
    {
        // Реализуем синглтон
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Также сохраняем WhisperManager
            if (whisperManager != null)
            {
                DontDestroyOnLoad(whisperManager.gameObject);
            }
        }
        else
        {
            // Если уже существует экземпляр, уничтожаем этот
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
            // Ищем существующий экземпляр на сцене
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