using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VRMainMenuUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown dropdown;
    public Button startButton;
    public Button refreshButton;

    [Header("Presentations")]
    public string presentationsFolder = "Presentations";
    public PresentationSettings settings;

    [Header("Loading")]
    public LoadingManager loadingManager;

    private List<string> fullPaths = new List<string>();

    void Start()
    {
        if (dropdown == null)
        {
            Debug.LogError("Dropdown missing!");
            return;
        }
        if (startButton == null)
        {
            Debug.LogError("Start Button missing!");
            return;
        }
        if (refreshButton == null)
        {
            Debug.LogError("Refresh Button missing!");
            return;
        }

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(OnDropdownChanged);

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartClicked);

        refreshButton.onClick.RemoveAllListeners();
        refreshButton.onClick.AddListener(OnRefreshClicked);

        RefreshPresentationsList();

        // Предзагрузка путей и проверка доступности
        PreloadDependencies();
    }

    private void PreloadDependencies()
    {
        // Предварительная инициализация путей
        string rootDir = Directory.GetParent(Application.dataPath).FullName;
        string presentationsPath = Path.Combine(rootDir, "Presentations");

        // Создаем папку если не существует
        if (!Directory.Exists(presentationsPath))
        {
            Directory.CreateDirectory(presentationsPath);
        }
    }

    public void RefreshPresentationsList()
    {
        fullPaths.Clear();
        dropdown.ClearOptions();

        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, presentationsFolder);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        string[] pptFiles = Directory.GetFiles(path, "*.pptx", SearchOption.TopDirectoryOnly);

        if (pptFiles == null || pptFiles.Length == 0)
        {
            dropdown.interactable = false;
            startButton.interactable = false;
            var opts = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("No presentations found") };
            dropdown.AddOptions(opts);
            Debug.LogWarning("Презентации не найдены в " + path);
            return;
        }

        var options = new List<TMP_Dropdown.OptionData>(pptFiles.Length);
        for (int i = 0; i < pptFiles.Length; i++)
        {
            string filePath = pptFiles[i];
            string fileName = Path.GetFileName(filePath);
            var option = new TMP_Dropdown.OptionData(fileName);
            options.Add(option);
            fullPaths.Add(filePath);
        }

        dropdown.AddOptions(options);
        dropdown.interactable = true;
        startButton.interactable = true;

        OnDropdownChanged(dropdown.value);
    }

    private void OnDropdownChanged(int index)
    {
        if (index < 0 || index >= fullPaths.Count)
        {
            return;
        }

        string fullPath = fullPaths[index];
        string fileName = Path.GetFileName(fullPath);
    }

    private void OnStartClicked()
    {
        int idx = dropdown.value;
        if (idx < 0 || idx >= fullPaths.Count)
        {
            Debug.LogWarning("Start pressed but no presentation selected.");
            return;
        }

        string fullPath = fullPaths[idx];
        string fileName = Path.GetFileName(fullPath);

        Debug.Log($"Start pressed. Selected presentation: {fileName}");
        if (settings != null)
        {
            settings.selectedPptxName = fileName;
        }

        if (File.Exists(fullPath))
        {
            // Запускаем загрузку в фоне через LoadingManager
            if (loadingManager != null)
            {
                loadingManager.StartLoading(fullPath);
            }
            else
            {
                // Если LoadingManager не назначен, загружаем сцену сразу
                Debug.LogWarning("LoadingManager not assigned, loading scene directly");
                SceneManager.LoadScene("MainScene");
            }
        }
        else
        {
            Debug.Log($"Can't find presentation \"{fileName}\". Refreshing list...");
            RefreshPresentationsList();
        }
    }

    private void OnRefreshClicked()
    {
        RefreshPresentationsList();
    }

    public void OnVADToggleChanged()
    {
        if (settings != null)
            settings.useVAD = !settings.useVAD;
    }
}