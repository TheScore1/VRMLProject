using UnityEngine;
using System.Diagnostics;
using System.IO;
using System;

public class GemmaServerPersist : MonoBehaviour
{
    [Header("Настройки сервера")]
    [SerializeField] private string pythonPath = "python";
    [SerializeField] private string serverScriptPath = "main.py";
    [SerializeField] private string arguments = "--host 127.0.0.1 --port 5000";
    [SerializeField] private bool showConsoleWindow = false;
    [SerializeField] private bool autoStartOnPlay = true;

    [Header("Отладка")]
    [SerializeField] private bool logOutput = true;
    [SerializeField] private bool logErrors = true;

    private Process pythonProcess;
    private bool isServerRunning = false;

    private static GemmaServerPersist instance;

    public static GemmaServerPersist Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GemmaServerPersist>();

                if (instance == null)
                {
                    GameObject obj = new GameObject("GemmaServer");
                    instance = obj.AddComponent<GemmaServerPersist>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (autoStartOnPlay)
        {
            StartServer();
        }
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    public void StartServer()
    {
        if (isServerRunning)
        {
            UnityEngine.Debug.LogWarning("Сервер уже запущен!");
            return;
        }

        try
        {
            //todo
            serverScriptPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "vr_speech_trainer/main.py");

            if (!File.Exists(serverScriptPath))
            {
                UnityEngine.Debug.LogError($"Файл скрипта не найден: {serverScriptPath}");
                return;
            }

            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = pythonPath;
            pythonProcess.StartInfo.Arguments = $"\"{serverScriptPath}\" {arguments}";
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.RedirectStandardOutput = true;
            pythonProcess.StartInfo.RedirectStandardError = true;
            pythonProcess.StartInfo.CreateNoWindow = !showConsoleWindow;

            pythonProcess.OutputDataReceived += OnOutputDataReceived;
            pythonProcess.ErrorDataReceived += OnErrorDataReceived;

            pythonProcess.Start();
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();

            isServerRunning = true;

            UnityEngine.Debug.Log($"Python сервер запущен (PID: {pythonProcess.Id})");
            UnityEngine.Debug.Log($"Команда: {pythonProcess.StartInfo.FileName} {pythonProcess.StartInfo.Arguments}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Ошибка при запуске сервера: {e.Message}");
            isServerRunning = false;
        }
    }

    public void StopServer()
    {
        if (!isServerRunning || pythonProcess == null)
            return;

        try
        {
            if (!pythonProcess.HasExited)
            {
                pythonProcess.Kill();
                pythonProcess.WaitForExit(5000);
            }

            pythonProcess.Dispose();
            pythonProcess = null;
            isServerRunning = false;

            UnityEngine.Debug.Log("Python сервер остановлен");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Ошибка при остановке сервера: {e.Message}");
        }
    }

    public void RestartServer()
    {
        if (isServerRunning)
        {
            StopServer();
            Invoke(nameof(StartServer), 1f);
        }
        else
        {
            StartServer();
        }
    }

    public bool IsServerRunning()
    {
        return isServerRunning && pythonProcess != null && !pythonProcess.HasExited;
    }

    public string GetServerInfo()
    {
        if (!IsServerRunning())
            return "Сервер не запущен";

        try
        {
            return $"Сервер работает (PID: {pythonProcess.Id}, Время: {pythonProcess.StartTime})";
        }
        catch
        {
            return "Сервер работает (информация недоступна)";
        }
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data) && logOutput)
        {
            UnityEngine.Debug.Log($"[Python Server] {e.Data}");
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data) && logErrors)
        {
            UnityEngine.Debug.LogError($"[Python Server ERROR] {e.Data}");
        }
    }

    public void CheckServerHealth(Action<bool, string> callback = null)
    {
        if (!IsServerRunning())
        {
            callback?.Invoke(false, "Сервер не запущен");
            return;
        }

        StartCoroutine(CheckHealthCoroutine(callback));
    }

    private System.Collections.IEnumerator CheckHealthCoroutine(Action<bool, string> callback)
    {
        UnityEngine.Networking.UnityWebRequest request =
            UnityEngine.Networking.UnityWebRequest.Get("http://127.0.0.1:5000/api/health");

        yield return request.SendWebRequest();

        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            callback?.Invoke(true, "Сервер работает нормально");
        }
        else
        {
            callback?.Invoke(false, $"Ошибка: {request.error}");
        }
    }

    void OnDestroy()
    {
        if (isServerRunning && pythonProcess != null && !pythonProcess.HasExited)
        {
            try
            {
                pythonProcess.Kill();
                pythonProcess.Dispose();
            }
            catch
            {
            }
        }
    }
}