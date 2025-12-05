using UnityEngine;
using TMPro;

public class SpeakerBubble : MonoBehaviour
{
    [Header("Bubble Elements")]
    [SerializeField] private GameObject bubbleObject;
    [SerializeField] private Vector2 offset = new Vector2(0, 1.0f);

    private TextMeshPro textMesh;
    private Transform targetPoint;

    private void Awake()
    {
        if (bubbleObject != null)
            bubbleObject.SetActive(false);

        if (bubbleObject != null)
            textMesh = bubbleObject.GetComponentInChildren<TextMeshPro>(true);

        if (textMesh == null)
            Debug.LogWarning("SpeakerBubble: TextMeshProUGUI не найден внутри bubbleObject!");
    }

    public void SetText(string text)
    {
        if (textMesh != null)
            textMesh.text = text;
    }

    public void Show()
    {
        if (bubbleObject != null)
            bubbleObject.SetActive(true);
    }

    public void Hide()
    {
        if (bubbleObject != null)
            bubbleObject.SetActive(false);
    }

    public void SetPosition(Vector3 worldPosition)
    {
        if (bubbleObject == null) return;

        Vector3 adjusted = worldPosition + new Vector3(offset.x, offset.y, 0f);

        bubbleObject.transform.position = adjusted;
    }

    public void AttachTo(Transform target)
    {
        targetPoint = target;
    }

    private void LateUpdate()
    {
        if (targetPoint != null && bubbleObject.activeSelf)
        {
            SetPosition(targetPoint.position);
        }
    }
}
