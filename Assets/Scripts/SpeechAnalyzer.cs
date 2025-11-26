using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class SpeechAnalyzer : MonoBehaviour
{
    [System.Serializable]
    public class SpeechAnalysis
    {
        public float speakingRate; // слов в минуту
        public int fillerWordsCount;
        public float clarityScore;
        public List<string> suggestions = new List<string>();
        public float confidenceLevel;
    }

    private readonly string[] fillerWords = {
        "это", "ну", "вот", "как бы", "типа", "типо", "значит", "короче"
    };

    public SpeechAnalysis AnalyzeSpeech(string text)
    {
        SpeechAnalysis analysis = new SpeechAnalysis();

        // Анализ темпа речи
        string[] words = text.Split(' ');
        analysis.speakingRate = words.Length / 0.083f; // Для 5-секундного отрезка

        analysis.fillerWordsCount = CountFillerWords(text);

        analysis.clarityScore = CalculateClarityScore(text);

        analysis.confidenceLevel = CalculateConfidenceLevel(text);

        GenerateSuggestions(analysis);

        FindObjectOfType<NPCManager>().TriggerNPCReactions(analysis);

        return analysis;
    }

    private int CountFillerWords(string text)
    {
        int count = 0;
        string lowerText = text.ToLower();

        foreach (string filler in fillerWords)
        {
            count += Regex.Matches(lowerText, @"\b" + filler + @"\b").Count;
        }

        return count;
    }

    private float CalculateClarityScore(string text)
    {
        // чем больше длинных слов, тем выше оценка
        string[] words = text.Split(' ');
        int longWords = 0;

        foreach (string word in words)
        {
            if (word.Length > 5) longWords++;
        }

        return Mathf.Clamp01((float)longWords / words.Length);
    }

    private float CalculateConfidenceLevel(string text)
    {
        var analysis = AnalyzeSpeech(text);

        // Комбинированная оценка уверенности
        float tempoScore = Mathf.Clamp01(analysis.speakingRate / 150f);
        float fillerPenalty = Mathf.Clamp01(1f - (analysis.fillerWordsCount * 0.1f));

        return (tempoScore + fillerPenalty + analysis.clarityScore) / 3f;
    }

    private void GenerateSuggestions(SpeechAnalysis analysis)
    {
        analysis.suggestions.Clear();

        if (analysis.speakingRate < 100)
            analysis.suggestions.Add("Говорите немного быстрее");
        else if (analysis.speakingRate > 180)
            analysis.suggestions.Add("Снизьте темп речи для лучшего восприятия");

        if (analysis.fillerWordsCount > 3)
            analysis.suggestions.Add("Старайтесь избегать слов-паразитов");

        if (analysis.clarityScore < 0.3f)
            analysis.suggestions.Add("Используйте более четкие формулировки");
    }
}