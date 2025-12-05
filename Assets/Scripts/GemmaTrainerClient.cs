using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class GemmaTrainerClient : MonoBehaviour
{
    private string apiBaseUrl = "http://localhost:5000/api";
    private string currentSessionId = "";

    // Callbacks для обработки результатов
    public delegate void OnResponseReceived(string response);
    public delegate void OnAnalysisComplete(Dictionary<string, object> analysis);

    public IEnumerator StartSession(string presentationPath, OnResponseReceived callback)
    {
        string url = $"{apiBaseUrl}/session/start";

        var requestData = new SessionStartRequest
        {
            presentation_file = presentationPath
        };

        string json = JsonUtility.ToJson(requestData);
        Debug.Log($"Sending JSON: {json}");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                Debug.Log($"Response: {response}");

                var sessionData = JsonUtility.FromJson<SessionStartResponse>(response);
                currentSessionId = sessionData.session_id;

                Debug.Log($"Session started: {currentSessionId}, Topic: {sessionData.topic}");
                callback?.Invoke(sessionData.topic);
            }
            else
            {
                Debug.LogError($"Error starting session: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");
                callback?.Invoke("");
            }
        }
    }

    public IEnumerator GenerateNPCQuestion(string speakerAnswer, OnResponseReceived callback)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogError("No active session");
            callback?.Invoke("");
            yield break;
        }

        string url = $"{apiBaseUrl}/session/{currentSessionId}/generate-question";

        var requestData = new NPCQuestionRequest { speaker_answer = speakerAnswer };
        string json = JsonUtility.ToJson(requestData);
        Debug.Log($"Sending JSON (generate-question): {json}");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            Debug.Log($"HTTP code: {www.responseCode}, text: {www.downloadHandler.text}");

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<QuestionResponse>(www.downloadHandler.text);
                Debug.Log($"Generated question: {response.question}");
                callback?.Invoke(response.question);
            }
            else
            {
                Debug.LogError($"Error generating question: {www.error}");
                Debug.LogError($"Response body: {www.downloadHandler.text}");
                callback?.Invoke("");
            }
        }
    }

    public IEnumerator AnalyzeAnswer(string question, string speakerAnswer, OnAnalysisComplete callback)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogError("No active session");
            yield break;
        }

        string url = $"{apiBaseUrl}/session/{currentSessionId}/analyze-answer";

        var requestData = new
        {
            question = question,
            speaker_answer = speakerAnswer
        };

        string json = JsonUtility.ToJson(requestData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                Debug.Log($"Analysis received: {response}");

                callback?.Invoke(new Dictionary<string, object> { { "feedback", response } });
            }
            else
            {
                Debug.LogError($"Error analyzing answer: {www.error}");
            }
        }
    }

    public IEnumerator AnalyzeSpeechStyle(string speechText, OnAnalysisComplete callback)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogError("No active session");
            yield break;
        }

        string url = $"{apiBaseUrl}/session/{currentSessionId}/analyze-speech-style";

        var requestData = new SpeechAnalysisRequest { speech_text = speechText };
        string json = JsonUtility.ToJson(requestData);
        Debug.Log($"Sending JSON (analyze-speech-style): {json}");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            Debug.Log($"HTTP code: {www.responseCode}, text: {www.downloadHandler.text}");

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                Debug.Log($"Speech analysis received: {response}");
                callback?.Invoke(new Dictionary<string, object> { { "analysis", response } });
            }
            else
            {
                Debug.LogError($"Error analyzing speech: {www.error}");
                Debug.LogError($"Response body: {www.downloadHandler.text}");
            }
        }
    }

    public IEnumerator GenerateFinalReport(float durationMinutes, List<AnalysisItem> analysisList, OnResponseReceived callback)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogError("No active session");
            callback?.Invoke("");
            yield break;
        }

        string url = $"{apiBaseUrl}/session/{currentSessionId}/final-report";

        var requestData = new FinalReportRequest
        {
            duration_minutes = durationMinutes,
            analysis_list = analysisList
        };

        string json = JsonUtility.ToJson(requestData);
        Debug.Log($"Sending JSON: {json}");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<FinalReportResponse>(www.downloadHandler.text);
                Debug.Log($"Final report generated successfully for session: {response.session_id}");
                callback?.Invoke(response.report);
            }
            else
            {
                Debug.LogError($"Error generating report: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");
                callback?.Invoke("");
            }
        }
    }
}

[System.Serializable]
public class SessionStartRequest
{
    public string presentation_file;
}

[System.Serializable]
public class SpeechAnalysisRequest
{
    public string speech_text;
}

[System.Serializable]
public class NPCQuestionRequest
{
    public string speaker_answer;
}

[System.Serializable]
public class AnalyzeAnswerRequest
{
    public string question;
    public string speaker_answer;
}

[System.Serializable]
public class AnalysisItem
{
    public string question;
    public string answer;
    public string detailed_feedback;
}

[System.Serializable]
public class FinalReportRequest
{
    public float duration_minutes;
    public List<AnalysisItem> analysis_list;
}

[System.Serializable]
public class SessionStartResponse
{
    public string session_id;
    public string topic;
    public string message;
}

[System.Serializable]
public class QuestionResponse
{
    public string question;
    public string session_id;
}

[System.Serializable]
public class FinalReportResponse
{
    public string report;
    public string session_id;
}

