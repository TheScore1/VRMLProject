using UnityEngine;

public static class MicrophoneStateManager
{
    public static bool IsRemoteHeld { get; set; } = false;
    public static bool IsProcessing { get; set; } = false;
    public static bool IsActuallySpeaking { get; set; } = false;
    public static bool MuteAnyway { get; set; } = true;

    public static bool CanSpeakNow => IsRemoteHeld && !IsProcessing && !MuteAnyway;

    public static event System.Action OnStateChanged;

    public static void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}