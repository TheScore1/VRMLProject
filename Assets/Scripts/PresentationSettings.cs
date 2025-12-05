using UnityEngine;

[CreateAssetMenu(fileName = "PresentationSettings", menuName = "VR/PresentationSettings")]
public class PresentationSettings : ScriptableObject
{
    public string selectedPptxName;
    public bool useVAD = true;

    public string debugSpeechText;
}
