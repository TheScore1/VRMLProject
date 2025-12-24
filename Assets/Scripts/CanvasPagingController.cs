using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CanvasPagingController : MonoBehaviour
{
    [Header("UI references (assign in inspector)")]
    [Tooltip("Root Canvas (or GameObject with Canvas). Will be forced inactive initially.")]
    [SerializeField] private Canvas canvasRoot;

    [SerializeField] private TextMeshProUGUI pageTextTMP;

    [Space]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button exitButton;

    [Header("Behaviour")]
    [SerializeField] private bool startHidden = true;

    public UnityEvent onExit;

    public UnityEvent<int> onPageChanged;

    private readonly List<string> pages = new List<string>();
    private int currentIndex = -1;
    private bool isAllowedToShow = false;

    private void Awake()
    {
        if (canvasRoot == null)
            canvasRoot = FindAnyObjectByType<Canvas>();

        if (canvasRoot == null)
            Debug.LogWarning("[CanvasPagingController] Canvas not assigned and none found in scene.");

        if (canvasRoot != null && startHidden)
            canvasRoot.gameObject.SetActive(false);

        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (exitButton != null) exitButton.onClick.AddListener(HandleExit);

        UpdateUIState();
    }

    private void OnDestroy()
    {
        if (prevButton != null) prevButton.onClick.RemoveListener(PrevPage);
        if (nextButton != null) nextButton.onClick.RemoveListener(NextPage);
        if (exitButton != null) exitButton.onClick.RemoveListener(HandleExit);
    }

    public void AllowCanvas(bool allow)
    {
        isAllowedToShow = allow;
        if (!allow)
            Hide();
    }

    public void Show()
    {
        if (!isAllowedToShow)
        {
            Debug.Log("[CanvasPagingController] Show() called but canvas is not allowed to be shown yet.");
            return;
        }
        if (canvasRoot != null)
            canvasRoot.gameObject.SetActive(true);

        if (pages.Count == 0)
        {
            currentIndex = -1;
            SetTextToUI(string.Empty);
        }
        else
        {
            if (currentIndex < 0 || currentIndex >= pages.Count)
                currentIndex = 0;
            DisplayCurrentPage();
        }
        UpdateUIState();
    }

    public void Hide()
    {
        if (canvasRoot != null)
            canvasRoot.gameObject.SetActive(false);
    }

    public bool IsOpen => canvasRoot != null && canvasRoot.gameObject.activeSelf;

    public void AddPage(string text)
    {
        pages.Add(text ?? string.Empty);
        if (currentIndex == -1)
            currentIndex = 0;
        UpdateUIState();
    }

    public void AddPages(IEnumerable<string> texts)
    {
        if (texts == null) return;
        foreach (var t in texts) pages.Add(t ?? string.Empty);
        if (currentIndex == -1 && pages.Count > 0) currentIndex = 0;
        UpdateUIState();
    }

    public void ClearPages()
    {
        pages.Clear();
        currentIndex = -1;
        SetTextToUI(string.Empty);
        UpdateUIState();
    }

    public void NextPage()
    {
        if (pages.Count == 0 || currentIndex < 0) return;
        if (currentIndex >= pages.Count - 1) return;
        currentIndex++;
        DisplayCurrentPage();
        UpdateUIState();
    }

    public void PrevPage()
    {
        if (pages.Count == 0 || currentIndex <= 0) return;
        currentIndex--;
        DisplayCurrentPage();
        UpdateUIState();
    }

    public bool SetPageIndex(int index)
    {
        if (index < 0 || index >= pages.Count) return false;
        currentIndex = index;
        DisplayCurrentPage();
        UpdateUIState();
        return true;
    }

    private void DisplayCurrentPage()
    {
        if (currentIndex < 0 || currentIndex >= pages.Count)
        {
            SetTextToUI(string.Empty);
            onPageChanged?.Invoke(currentIndex);
            return;
        }

        SetTextToUI(pages[currentIndex]);
        onPageChanged?.Invoke(currentIndex);
    }

    private void SetTextToUI(string text)
    {
        if (pageTextTMP != null)
        {
            pageTextTMP.text = text;
            return;
        }

        Debug.LogWarning("[CanvasPagingController] No text component assigned (TMP or UI Text).");
    }

    private void UpdateUIState()
    {
        bool hasPages = pages.Count > 0 && currentIndex >= 0;

        if (prevButton != null)
            prevButton.interactable = hasPages && currentIndex > 0;
        if (nextButton != null)
            nextButton.interactable = hasPages && currentIndex < pages.Count - 1;

        if (exitButton != null)
            exitButton.interactable = true;
    }

    private void HandleExit()
    {
        Hide();

        try { onExit?.Invoke();
            SceneManager.LoadScene("Menu");
        }

        catch (Exception ex) { Debug.LogException(ex); }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Print Pages Count")]
    private void DebugPrintPages()
    {
        Debug.Log($"Pages: {pages.Count}, currentIndex={currentIndex}");
    }
#endif
}
