using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Diagnostics;
using Whisper.Utils;
using Whisper;
using Debug = UnityEngine.Debug;

public class LoadingManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingTitle;
    public TextMeshProUGUI progressText;
    public Button[] buttonsToDisable;
    public GameObject[] objectsToHide;

    [Header("Loading Steps")]
    public List<string> loadingSteps = new List<string>
    {
        "LibreOffice",
        "Poppler",
        "Scene",
        "Whisper Model"
    };

    [Header("Dependencies")]
    public WhisperManager whisperManager;
    public PresentationSettings settings;

    private List<string> remainingSteps;
    private bool isLoading = false;
    private string presentationPath;

    private string rootDir;
    private string libreOfficeExePath;
    private string popplerPath;
    private string presentationsPath;
    private string outputPath;

    void Start()
    {
        InitializePaths();

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (whisperManager == null)
        {
            whisperManager = WhisperManagerPersist.GetWhisperManager();
        }

        if (whisperManager == null)
        {
            whisperManager = FindFirstObjectByType<WhisperManager>();
        }
    }

    private void InitializePaths()
    {
        rootDir = Directory.GetParent(Application.dataPath).FullName;
        libreOfficeExePath = Path.Combine(rootDir, "LibreOffice", "App", "libreoffice", "program", "soffice.exe");
        presentationsPath = Path.Combine(rootDir, "Presentations");
        popplerPath = Path.Combine(rootDir, "poppler-25.07.0", "Library", "bin", "pdftoppm.exe");
        outputPath = Path.Combine(presentationsPath, "Output");
    }

    public void StartLoading(string pptxPath)
    {
        if (isLoading) return;

        isLoading = true;
        presentationPath = pptxPath;

        SetUIVisibility(false);
        SetButtonsInteractable(false);

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (loadingTitle != null)
            loadingTitle.text = "Подготавливаем сцену";

        remainingSteps = new List<string>(loadingSteps);
        UpdateProgressText();

        StartCoroutine(LoadAllResourcesCoroutine());
    }

    private IEnumerator LoadAllResourcesCoroutine()
    {
        Debug.Log("Starting background loading...");

        // Дать UI время отрисоваться
        yield return null;
        yield return null;

        // ------------------------------------------------------------
        // 1) СНАЧАЛА грузим Whisper + LibreOffice + Poppler
        // ------------------------------------------------------------

        // --- LibreOffice ---
        yield return StartCoroutine(LoadLibreOfficeAndConvertCoroutine());
        RemoveStep("LibreOffice");

        // --- Poppler ---
        string pdfFile = Path.Combine(
            outputPath,
            Path.GetFileNameWithoutExtension(presentationPath) + ".pdf"
        );

        if (!File.Exists(pdfFile))
        {
            Debug.LogError($"PDF файл не создан: {pdfFile}");
        }
        else
        {
            yield return StartCoroutine(LoadPopplerAndConvertCoroutine());
            RemoveStep("Poppler");
        }

        // --- Whisper ---
        yield return StartCoroutine(LoadWhisperModelCoroutine());
        RemoveStep("Whisper Model");

        // ------------------------------------------------------------
        // 2) ТЕПЕРЬ начинаем загрузку сцены
        // ------------------------------------------------------------

        Debug.Log("Starting async scene loading...");

        AsyncOperation sceneLoadOperation =
            SceneManager.LoadSceneAsync("MainScene");
        sceneLoadOperation.allowSceneActivation = false;

        // ------------------------------------------------------------
        // 3) Ждём, пока сцена полностью загрузится (до 0.9)
        // ------------------------------------------------------------

        yield return StartCoroutine(WaitForScenePreload(sceneLoadOperation));
        RemoveStep("Scene");

        // ещё чуть-чуть задержка для мягкой активации
        yield return new WaitForSeconds(0.3f);

        // Активируем сцену
        sceneLoadOperation.allowSceneActivation = true;

        Debug.Log("All loading completed, activating scene...");
    }

    private IEnumerator WaitForScenePreload(AsyncOperation op)
    {
        while (op.progress < 0.9f)
        {
            yield return null;
        }
    }

    private System.Collections.IEnumerator LoadLibreOfficeAndConvertCoroutine()
    {
        string fileName = Path.GetFileName(presentationPath);
        string filePath = Path.Combine(presentationsPath, fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"PPTX файл не найден: {filePath}");
            yield break;
        }

        // Очищаем выходную папку асинхронно
        yield return StartCoroutine(CleanOutputDirectoryCoroutine());

        // Запускаем LibreOffice процесс
        bool conversionSuccess = false;
        yield return StartCoroutine(RunLibreOfficeProcessCoroutine(filePath, success => {
            conversionSuccess = success;
        }));

        if (!conversionSuccess)
        {
            Debug.LogError("LibreOffice conversion failed");
        }
        else
        {
            Debug.Log("LibreOffice conversion completed successfully");
        }
    }

    private System.Collections.IEnumerator RunLibreOfficeProcessCoroutine(string filePath, System.Action<bool> onComplete)
    {
        var tcs = new TaskCompletionSource<bool>();

        Task.Run(() => {
            try
            {
                if (!File.Exists(libreOfficeExePath))
                {
                    Debug.LogError($"LibreOffice not found at: {libreOfficeExePath}");
                    tcs.SetResult(false);
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = libreOfficeExePath,
                    Arguments = $"--headless --convert-to pdf \"{filePath}\" --outdir \"{outputPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(libreOfficeExePath)
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    Debug.Log("Starting LibreOffice process...");
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    bool exited = process.WaitForExit(60000); // 60 секунд таймаут

                    if (!exited)
                    {
                        Debug.LogError("LibreOffice process timeout");
                        process.Kill();
                        tcs.SetResult(false);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(error) && !error.Contains("WARNING"))
                        {
                            Debug.LogWarning($"LibreOffice error: {error}");
                        }

                        if (!string.IsNullOrEmpty(output))
                        {
                            Debug.Log($"LibreOffice output: {output}");
                        }

                        // Проверяем, создался ли PDF файл
                        string pdfFile = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(filePath) + ".pdf");
                        bool success = File.Exists(pdfFile);

                        if (success)
                        {
                            Debug.Log($"PDF file successfully created: {pdfFile}");
                        }
                        else
                        {
                            Debug.LogError($"PDF file not created: {pdfFile}");
                        }

                        tcs.SetResult(success);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"LibreOffice process failed: {e.Message}");
                tcs.SetResult(false);
            }
        });

        while (!tcs.Task.IsCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }

        onComplete?.Invoke(tcs.Task.Result);
    }

    private System.Collections.IEnumerator LoadPopplerAndConvertCoroutine()
    {
        string pdfFile = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(presentationPath) + ".pdf");

        // Дополнительная проверка существования PDF файла
        if (!File.Exists(pdfFile))
        {
            Debug.LogError($"PDF файл не существует: {pdfFile}");
            yield break;
        }

        Debug.Log($"Starting Poppler conversion for: {pdfFile}");

        bool conversionSuccess = false;
        yield return StartCoroutine(RunPopplerProcessCoroutine(pdfFile, success => {
            conversionSuccess = success;
        }));

        if (!conversionSuccess)
        {
            Debug.LogError("Poppler conversion failed");
        }
        else
        {
            Debug.Log("Poppler conversion completed successfully");

            // Проверяем созданные PNG файлы
            string[] pngFiles = Directory.GetFiles(outputPath, "*.png");
            Debug.Log($"Created {pngFiles.Length} PNG files");
        }
    }

    private System.Collections.IEnumerator RunPopplerProcessCoroutine(string pdfFile, System.Action<bool> onComplete)
    {
        var tcs = new TaskCompletionSource<bool>();

        Task.Run(() => {
            try
            {
                if (!File.Exists(popplerPath))
                {
                    Debug.LogError($"Poppler not found at: {popplerPath}");
                    tcs.SetResult(false);
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = popplerPath,
                    Arguments = $"-png -r 100 \"{pdfFile}\" \"{Path.Combine(outputPath, "Slide")}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(popplerPath)
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    Debug.Log("Starting Poppler process...");
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    bool exited = process.WaitForExit(30000); // 30 секунд таймаут

                    if (!exited)
                    {
                        Debug.LogError("Poppler process timeout");
                        process.Kill();
                        tcs.SetResult(false);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.LogError($"Poppler error: {error}");
                        }

                        if (!string.IsNullOrEmpty(output))
                        {
                            Debug.Log($"Poppler output: {output}");
                        }

                        // Проверяем, создались ли PNG файлы
                        string[] pngFiles = Directory.GetFiles(outputPath, "*.png");
                        tcs.SetResult(pngFiles.Length > 0);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Poppler process failed: {e.Message}");
                tcs.SetResult(false);
            }
        });

        while (!tcs.Task.IsCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }

        onComplete?.Invoke(tcs.Task.Result);
    }

    private System.Collections.IEnumerator CleanOutputDirectoryCoroutine()
    {
        var tcs = new TaskCompletionSource<bool>();

        Task.Run(() => {
            try
            {
                if (Directory.Exists(outputPath))
                {
                    var files = Directory.GetFiles(outputPath);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Failed to delete file {file}: {e.Message}");
                        }
                    }
                    Debug.Log($"Cleaned output directory: {outputPath}");
                }
                else
                {
                    Directory.CreateDirectory(outputPath);
                    Debug.Log($"Created output directory: {outputPath}");
                }
                tcs.SetResult(true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Clean output directory failed: {e.Message}");
                tcs.SetResult(false);
            }
        });

        while (!tcs.Task.IsCompleted)
        {
            yield return null;
        }
    }

    private System.Collections.IEnumerator LoadWhisperModelCoroutine()
    {
        if (whisperManager == null)
        {
            Debug.LogError("WhisperManager is not assigned!");
            yield break;
        }

        if (whisperManager.IsLoaded)
        {
            Debug.Log("Whisper model already loaded");
            yield break;
        }

        if (whisperManager.IsLoading)
        {
            Debug.Log("Whisper model is already loading, waiting...");
            // Ждём загрузки, но с timeout
            yield return StartCoroutine(WaitForWhisperLoadCoroutine());
            yield break;
        }

        Debug.Log("Starting Whisper model loading...");

        bool initSuccess = false;
        yield return StartCoroutine(InitializeWhisperModelCoroutine(success => initSuccess = success));

        if (!initSuccess)
        {
            Debug.LogError("Whisper model initialization failed");
            yield break;
        }

        // дождёмся реального состояния IsLoaded (внутри InitModel может быть асинхронная цепочка)
        yield return StartCoroutine(WaitForWhisperLoadCoroutine());

        if (whisperManager.IsLoaded)
            Debug.Log("Whisper model loaded successfully");
        else
            Debug.LogError("Whisper model failed to load after init");
    }


    private System.Collections.IEnumerator InitializeWhisperModelCoroutine(System.Action<bool> onComplete)
    {
        if (whisperManager == null)
        {
            Debug.LogError("WhisperManager is null");
            onComplete?.Invoke(false);
            yield break;
        }

        bool success = false;
        string errorMessage = null;

        Task initTask = null;
        try
        {
            initTask = Task.Run(async () => await whisperManager.InitModel());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to start InitModel task: {ex.Message}");
            onComplete?.Invoke(false);
            yield break;
        }

        // Ждём завершения task без блокировки main thread
        while (!initTask.IsCompleted)
        {
            yield return null;
        }

        if (initTask.IsFaulted)
        {
            var ex = initTask.Exception;
            errorMessage = ex?.GetBaseException()?.Message ?? "Unknown error during InitModel";
            Debug.LogError($"Whisper model initialization failed (task faulted): {errorMessage}");
            success = false;
        }
        else if (initTask.IsCanceled)
        {
            Debug.LogError("Whisper model initialization was cancelled");
            success = false;
        }
        else
        {
            // InitModel завершился (успех/ошибка внутри InitModel выставит IsLoaded/IsLoading)
            success = true;
        }

        onComplete?.Invoke(success);
    }


    private System.Collections.IEnumerator WaitForWhisperLoadCoroutine()
    {
        float timeout = 60f;
        float startTime = Time.time;
        float lastLogTime = Time.time;

        while (!whisperManager.IsLoaded && (Time.time - startTime) < timeout)
        {
            if (!whisperManager.IsLoading && !whisperManager.IsLoaded)
            {
                Debug.LogError("Whisper model is not loading and not loaded - aborting wait");
                yield break;
            }

            // логируем каждые 2 секунды
            if (Time.time - lastLogTime > 2f)
            {
                Debug.Log($"Waiting for Whisper model... IsLoading: {whisperManager.IsLoading}, IsLoaded: {whisperManager.IsLoaded}");
                lastLogTime = Time.time;
            }

            yield return null; // более плавно, чем WaitForSeconds
        }

        if (!whisperManager.IsLoaded)
        {
            Debug.LogError($"Whisper model loading timeout after {timeout} seconds");
        }
    }


    private void RemoveStep(string stepName)
    {
        if (remainingSteps.Contains(stepName))
        {
            remainingSteps.Remove(stepName);
            UpdateProgressText();
            Debug.Log($"Completed: {stepName}");
        }
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            string text = "Загружается:\n";
            foreach (string step in loadingSteps)
            {
                string status = remainingSteps.Contains(step) ? "[ ]" : "[x]";
                text += $"{status} {step}\n";
            }
            progressText.text = text;
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (Button button in buttonsToDisable)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    private void SetUIVisibility(bool visible)
    {
        foreach (GameObject obj in objectsToHide)
        {
            if (obj != null)
                obj.SetActive(visible);
        }
    }

    public bool IsLoadingComplete()
    {
        return !isLoading && (remainingSteps == null || remainingSteps.Count == 0);
    }
}