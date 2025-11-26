using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PresControllerGrabHandler : MonoBehaviour
{
    [Header("Controller Visuals")]
    [Tooltip("Объект с MeshRenderer стандартного VR контроллера, который нужно скрыть при поднятии")]
    public GameObject vrControllerVisual;

    [Tooltip("Копия пульта, которая будет активна после подъема")]
    public GameObject remoteCopy;

    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("XRGrabInteractable отсутствует на объекте " + name);
            enabled = false;
            return;
        }

        grabInteractable.selectEntered.AddListener(OnGrab);

        if (remoteCopy != null) remoteCopy.SetActive(false);
        if (vrControllerVisual != null) vrControllerVisual.SetActive(true);
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (vrControllerVisual != null) vrControllerVisual.SetActive(false);

        if (remoteCopy != null) remoteCopy.SetActive(true);

        gameObject.SetActive(false);
    }
}