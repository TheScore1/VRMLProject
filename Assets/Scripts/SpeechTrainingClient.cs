using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class SpeechTrainingClient : MonoBehaviour
{
    private const string BaseUrl = "http://localhost:8000";

    public IEnumerator StartSession()
    {
        yield return Post("/session/start", "{}");
    }

    public IEnumerator LoadPresentation(
        string pptxPath,
        System.Action<LoadPresentationResponse> onSuccess)
    {
        var req = new LoadPresentationRequest
        {
            pptx_path = pptxPath,
            excerpt_length = 1500
        };

        yield return Post(
            "/presentation/load",
            JsonUtility.ToJson(req),
            json => onSuccess(JsonUtility.FromJson<LoadPresentationResponse>(json))
        );
    }

    public IEnumerator RequestNextQuestion(System.Action<string> onQuestion)
    {
        yield return Post(
            "/questions/next",
            "{}",
            json =>
            {
                var resp = JsonUtility.FromJson<QuestionResponse>(json);
                onQuestion?.Invoke(resp.question);
            }
        );
    }

    public IEnumerator SendAnswer(string text, float durationSeconds, string kind)
    {
        var req = new AnswerRequest
        {
            text = text,
            duration_seconds = durationSeconds,
            kind = kind
        };

        yield return Post("/answers/add", JsonUtility.ToJson(req));
    }

    public IEnumerator FinishSession(System.Action<FinishSessionResponse> onFinish)
    {
        yield return Post(
            "/session/finish",
            "{}",
            json =>
            {
                var resp = JsonUtility.FromJson<FinishSessionResponse>(json);
                onFinish?.Invoke(resp);
            }
        );
    }

    private IEnumerator Post(
        string endpoint,
        string json,
        System.Action<string> onSuccess = null)
    {
        using var request = new UnityWebRequest(
            BaseUrl + endpoint,
            UnityWebRequest.kHttpVerbPOST
        );

        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"API error ({endpoint}): {request.error}");
            yield break;
        }

        onSuccess?.Invoke(request.downloadHandler.text);
    }
}
