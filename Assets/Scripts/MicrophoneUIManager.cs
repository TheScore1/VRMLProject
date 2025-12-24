using UnityEngine;
using UnityEngine.UI;

public class MicrophoneUIManager : MonoBehaviour
{
    [SerializeField] private Sprite MutedIcon;
    [SerializeField] private Sprite UnMutedIcon;
    [SerializeField] private Image ImgComponent;

    void Start()
    {
        MicrophoneStateManager.OnStateChanged += UpdateUI;
        UpdateUI();
    }

    void OnDestroy()
    {
        MicrophoneStateManager.OnStateChanged -= UpdateUI;
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (ImgComponent == null) return;

        if (!MicrophoneStateManager.IsRemoteHeld)
        {
            ImgComponent.sprite = MutedIcon;
            ImgComponent.color = Color.red;
        }
        else if (MicrophoneStateManager.IsProcessing)
        {
            ImgComponent.sprite = MutedIcon;
            ImgComponent.color = Color.yellow;
        }
        else
        {
            ImgComponent.sprite = UnMutedIcon;
            ImgComponent.color = MicrophoneStateManager.IsActuallySpeaking ?
                Color.green : Color.black;
        }
    }
}