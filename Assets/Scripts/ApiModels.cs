using System;
using JetBrains.Annotations;

[Serializable]
public class LoadPresentationRequest
{
    public string pptx_path;
    public int excerpt_length = 1500;
}

[Serializable]
public class LoadPresentationResponse
{
    public string topic;
    public int excerpt_length;
}

[Serializable]
public class QuestionResponse
{
    public string question;
}

[Serializable]
public class AnswerRequest
{
    public string text;
    public float duration_seconds;
    public string kind; // "presentation" | "answer"
}

[Serializable]
public class FinishSessionResponse
{
    public SpeechStats stats;
    public string report;
}

[Serializable]
public class SpeechStats
{
    public int words_count;
    public int sentences_count;
    public float avg_sentence_length;
    public string parasites;
    public int presentation_duration;
}
