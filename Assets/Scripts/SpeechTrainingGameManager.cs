using UnityEngine;
using System.Collections;
using Whisper.Samples;
using System;
using System.IO;
using System.Linq;

public class SpeechTrainingGameManager : MonoBehaviour
{
    [SerializeField] private SpeechTrainingClient api;
    [SerializeField] private MicrophoneRecorder mic;
    [SerializeField] private WhisperAudio whisper;
    public PresentationSettings settings;
    [SerializeField] private SpeakerBubble bubble;

    [Header("Game Settings")]
    private int questionsCount = 1;
    [Tooltip("Если выключено, используется значение MIN")]
    [SerializeField] private bool randomizeQuestionsCount = true;
    [SerializeField] private int minQuestions = 1;
    [SerializeField] private int maxQuestions = 3;

    private int currentQuestionIndex;
    private CanvasPagingController cpg;

    private void Start()
    {
        bubble = FindAnyObjectByType<SpeakerBubble>();
        cpg = FindAnyObjectByType<CanvasPagingController>();

        if (bubble == null)
        {
            Debug.Log("Нет SpeakerBubble. Return");
            return;
        }
        if (cpg == null)
        {
            Debug.Log("Нет CanvasPagingController. Return");
            return;
        }

        StartCoroutine(GameFlow());
    }

    private IEnumerator GameFlow()
    {
        System.Random rand = new System.Random();
        questionsCount = randomizeQuestionsCount ? rand.Next(minQuestions, maxQuestions) : minQuestions;

        yield return api.StartSession();

        yield return api.LoadPresentation(
            pptxPath: settings.selectedPptxName,
            onSuccess: r =>
            {
                Debug.Log($"Тема презентации: {r.topic}");
            }
        );

        Debug.Log("🎙 Начните выступление по презентации");
        MicrophoneStateManager.MuteAnyway = false;
        mic.ResetSpeechState();
        yield return WaitForPresentationSpeech();

        currentQuestionIndex = 0;

        while (currentQuestionIndex < questionsCount)
        {
            string question = null;

            yield return api.RequestNextQuestion(q => question = q);

            var question_normalized = question.Replace("*", "");

            if (question_normalized.Contains(":"))
                question_normalized = question_normalized.Split(":")[1];

            question_normalized = question_normalized.Trim();

            Debug.Log($"ВОПРОС {currentQuestionIndex + 1}: {question_normalized}");

            bubble.SetRandomizedPosition();
            bubble.SetText(question_normalized);
            
            yield return WaitForAnswerSpeech();

            string answerText = GetLastTranscription();
            float answerDuration = GetLastDuration();

            bubble.Hide();

            yield return api.SendAnswer(
                answerText,
                answerDuration,
                kind: "answer"
            );

            currentQuestionIndex++;
        }

        MicrophoneStateManager.MuteAnyway = true;

        yield return api.FinishSession(OnGameFinished);
    }

    private IEnumerator WaitForPresentationSpeech()
    {
        while (!mic.SpeechEnded)
            yield return null;

        bool done = false;
        whisper.OnTranscriptionComplete += (_) => done = true;

        whisper.TranscribeLastRecording();

        while (!done)
            yield return null;

        whisper.OnTranscriptionComplete -= (_) => done = true;

        string speechText = GetLastTranscription();
        float duration = GetLastDuration();

        Debug.Log("🎤 Выступление завершено");

        yield return api.SendAnswer(
            speechText,
            duration,
            kind: "presentation"
        );
    }

    private IEnumerator WaitForAnswerSpeech()
    {
        mic.ResetSpeechState();

        while (!mic.SpeechEnded)
            yield return null;

        bool done = false;
        whisper.OnTranscriptionComplete += (_) => done = true;

        whisper.TranscribeLastRecording();

        while (!done)
            yield return null;

        whisper.OnTranscriptionComplete -= (_) => done = true;
    }

    private string GetLastTranscription()
    {
        return whisper._lastTranscribedClip;
    }

    private float GetLastDuration()
    {
        return whisper._lastRecordedClip.length;
    }

    private void OnGameFinished(FinishSessionResponse result)
    {
        var reportLines = result.report
            .Trim()
            .Replace("*", "")
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (reportLines.Contains("ИТОГОВЫЙ ОТЧЕТ"))
            reportLines.Remove("ИТОГОВЫЙ ОТЧЕТ");

        if (reportLines.Contains("ИТОГОВЫЙ ОТЧЁТ"))
            reportLines.Remove("ИТОГОВЫЙ ОТЧЁТ");

        if (reportLines.Contains("ИТОГОВЫЙ ОТЧЕТ:"))
            reportLines.Remove("ИТОГОВЫЙ ОТЧЕТ:");

        if (reportLines.Contains("ИТОГОВЫЙ ОТЧЁТ:"))
            reportLines.Remove("ИТОГОВЫЙ ОТЧЁТ:");

        reportLines.Insert(0, $"Время выступления: {result.stats.presentation_duration} минут");
        reportLines.Insert(1, $"Слова паразиты: {result.stats.parasites}");

        for (int i = 0; i < reportLines.Count; i += 3)
        {
            var str = "";
            for (int j = 0; j < 3 && (i + j) < reportLines.Count; j++)
            {
                str += "\n" + reportLines[i + j];
            }

            cpg.AddPage($"Страница {i / 3 + 1}" + str);
        }

        cpg.AllowCanvas(true);
        cpg.Show();
    }
}
