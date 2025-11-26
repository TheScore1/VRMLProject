using UnityEngine;
using UnityEngine.UI;
using static SpeechAnalyzer;

public class AnalysisUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text transcribedText;
    public Text confidenceText;
    public Text suggestionsText;
    public Slider confidenceSlider;
    public GameObject analysisPanel;

    public void DisplayAnalysis(SpeechAnalysis analysis, string originalText)
    {
        analysisPanel.SetActive(true);

        transcribedText.text = $"Текст: {originalText}";
        confidenceText.text = $"Уверенность: {(analysis.confidenceLevel * 100):F1}%";
        confidenceSlider.value = analysis.confidenceLevel;

        suggestionsText.text = "Рекомендации:\n";
        foreach (string suggestion in analysis.suggestions)
        {
            suggestionsText.text += $"• {suggestion}\n";
        }
    }
}