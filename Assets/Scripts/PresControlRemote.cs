using UnityEngine;
using UnityEngine.InputSystem;

public class PresControlRemote : MonoBehaviour
{
    [Header("Presentation Controller")]
    public PresentationController presentationController;

    [Header("Input Actions")]
    public InputActionReference nextSlideAction;
    public InputActionReference prevSlideAction;

    void OnEnable()
    {
        if (nextSlideAction?.action != null) nextSlideAction.action.Enable();
        if (prevSlideAction?.action != null) prevSlideAction.action.Enable();
    }

    void OnDisable()
    {
        if (nextSlideAction?.action != null) nextSlideAction.action.Disable();
        if (prevSlideAction?.action != null) prevSlideAction.action.Disable();
    }

    void Update()
    {
        if (nextSlideAction?.action != null && nextSlideAction.action.WasPressedThisFrame())
        {
            if (presentationController.currentSlideIndex != presentationController.slideTextures.Count - 1)
                presentationController.currentSlideIndex++;
            Debug.Log("[RemoteCopy] NextSlide -> " + presentationController.currentSlideIndex);
        }

        if (prevSlideAction?.action != null && prevSlideAction.action.WasPressedThisFrame())
        {
            if (presentationController.currentSlideIndex > 0)
                presentationController.currentSlideIndex--;
            Debug.Log("[RemoteCopy] PrevSlide -> " + presentationController.currentSlideIndex);
        }
    }
}
