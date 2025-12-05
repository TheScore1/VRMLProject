using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;

public class VRTrainerManager : MonoBehaviour
{
    private GemmaTrainerClient gemmaClient;
    private SpeakerBubble speaker;
    private string currentTopic = "";
    private string transcribedSpeech = "";

    [Header("Settings")]
    public PresentationSettings settings;

    void Start()
    {
        speaker = FindAnyObjectByType<SpeakerBubble>();
        if (speaker == null)
        {
            Debug.LogError("Questions UI not found");
            return;
        }    

        var rootDir = Directory.GetParent(Application.dataPath).FullName;
        var presentationsPath = Path.Combine(rootDir, "Presentations");
        var thisPresPath = Path.Combine(presentationsPath, settings.selectedPptxName);

        gemmaClient = GetComponent<GemmaTrainerClient>();

        StartCoroutine(gemmaClient.StartSession(thisPresPath.Replace('\\', '/'), OnSessionStarted));
    }

    void OnSessionStarted(string topic)
    {
        currentTopic = topic;
        Debug.Log($"Training session started. Topic: {currentTopic}");

        SimulateTrainingFlow();
    }

    void SimulateTrainingFlow()
    {
        // Симуляция: спикер отвечает на вопрос
        string speakerAnswer = "В презентации я говорил о том, что машинное обучение..." +
            "Это очень важная технология... ну... вот...";

        // Анализируем стиль речи
        StartCoroutine(gemmaClient.AnalyzeSpeechStyle(speakerAnswer, OnSpeechAnalyzed));

        // Генерируем следующий вопрос от NPC
        StartCoroutine(gemmaClient.GenerateNPCQuestion(speakerAnswer, OnQuestionGenerated));
    }

    void OnSpeechAnalyzed(System.Collections.Generic.Dictionary<string, object> analysis)
    {
        Debug.Log("Speech analysis complete");
        // Отображаем рекомендации в UI VR-тренажёра
        Debug.Log(analysis.Keys);
    }

    void OnQuestionGenerated(string question)
    {
        Debug.Log($"NPC: {question}");
        // NPC произносит вопрос голосом в VR
        // Ждём ответа спикера
        // Затем вызываем AnalyzeAnswer
        speaker.SetText(question);
    }
}
