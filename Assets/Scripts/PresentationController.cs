using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PresentationController : MonoBehaviour
{
    [SerializeField] public int maxSlides = 20;
    [Tooltip("Нужно указывать .pptx в конце")]
    string pptxName = "Example.pptx";

    [SerializeField] GameObject[] planesToDisplay;

    [Header("Settings")]
    public PresentationSettings settings;

    string rootDir;
    string presentationsPath;
    string outputPath;

    public int currentSlideIndex = 0;
    public List<Texture2D> slideTextures = new List<Texture2D>();

    void Start()
    {
        if (settings == null || string.IsNullOrEmpty(settings.selectedPptxName))
        {
            UnityEngine.Debug.LogWarning("Презентация не выбрана!");
            return;
        }

        pptxName = settings.selectedPptxName;
        UnityEngine.Debug.Log($"Запускаем презентацию: {pptxName}");

        rootDir = Directory.GetParent(Application.dataPath).FullName;
        presentationsPath = Path.Combine(rootDir, "Presentations");
        outputPath = Path.Combine(presentationsPath, "Output");

        // Только загружаем готовые текстуры, конвертация уже выполнена в меню
        LoadSlideTextures();

        // Показываем первый слайд
        ShowCurrentSlide();
    }

    void LoadSlideTextures()
    {
        slideTextures.Clear();
        if (!Directory.Exists(outputPath))
        {
            UnityEngine.Debug.LogWarning($"Папка Output не найдена: {outputPath}");
            return;
        }

        string[] files = Directory.GetFiles(outputPath, "*.png");
        if (files.Length == 0)
        {
            UnityEngine.Debug.LogWarning("Не найдено PNG изображений в Output!");
            return;
        }

        System.Array.Sort(files);

        foreach (var file in files)
        {
            byte[] bytes = File.ReadAllBytes(file);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
                slideTextures.Add(tex);
        }

        UnityEngine.Debug.Log($"Загружено {slideTextures.Count} слайдов.");
    }

    void ShowCurrentSlide()
    {
        Texture2D currentTexture = GetCurrentSlideTexture();
        if (currentTexture == null) return;

        foreach (GameObject plane in planesToDisplay)
        {
            Renderer renderer = plane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.mainTexture = currentTexture;
            }
        }
    }

    public Texture2D GetCurrentSlideTexture()
    {
        if (slideTextures.Count == 0) return null;
        int idx = Mathf.Clamp(currentSlideIndex, 0, slideTextures.Count - 1);
        return slideTextures[idx];
    }

    public int TotalSlides => slideTextures.Count;

    // Методы для переключения слайдов
    public void NextSlide()
    {
        if (slideTextures.Count == 0) return;

        currentSlideIndex = (currentSlideIndex + 1) % slideTextures.Count;
        ShowCurrentSlide();
    }

    public void PreviousSlide()
    {
        if (slideTextures.Count == 0) return;

        currentSlideIndex = (currentSlideIndex - 1 + slideTextures.Count) % slideTextures.Count;
        ShowCurrentSlide();
    }
}