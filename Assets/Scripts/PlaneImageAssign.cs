using System.Collections.Generic;
using UnityEngine;

public class PlaneImageAssign : MonoBehaviour
{
    [Header("Material / Settings")]
    public Material targetMaterial;
    public Color backgroundColor = Color.black;
    public bool setSharedMaterial = true;

    [Header("Planes")]
    public List<MeshRenderer> planeRenderers;

    [Header("Presentation Controller")]
    public PresentationController presentationController;

    int lastSlideIndex = -1;

    void Start()
    {
        if (targetMaterial == null)
        {
            Debug.LogError("Target material не назначен!");
            return;
        }

        if (planeRenderers == null || planeRenderers.Count == 0)
        {
            var found = GameObject.FindGameObjectsWithTag("Plane");
            planeRenderers = new List<MeshRenderer>();
            foreach (var go in found)
            {
                var r = go.GetComponent<MeshRenderer>();
                if (r != null) planeRenderers.Add(r);
            }
        }

        if (planeRenderers.Count == 0)
        {
            Debug.LogError("Ќе найдено плоскостей дл€ отображени€ слайдов!");
            return;
        }

        float planeAspect = ComputePlaneAspect(planeRenderers[0].GetComponent<MeshFilter>(), planeRenderers[0].transform);
        targetMaterial.SetFloat("_PlaneAspect", planeAspect);
        targetMaterial.SetColor("_BackgroundColor", backgroundColor);

        foreach (var rend in planeRenderers)
        {
            if (setSharedMaterial)
                rend.sharedMaterial = targetMaterial;
            else
                rend.material = new Material(targetMaterial) { };
        }

        UpdateSlideTexture();
    }

    void Update()
    {
        if (presentationController == null) return;

        if (presentationController.currentSlideIndex != lastSlideIndex)
        {
            UpdateSlideTexture();
        }
    }

    void UpdateSlideTexture()
    {
        lastSlideIndex = presentationController.currentSlideIndex;
        Texture2D tex = presentationController.GetCurrentSlideTexture();
        if (tex == null)
        {
            Debug.LogWarning("“екуща€ текстура слайда отсутствует.");
            return;
        }

        targetMaterial.SetTexture("_MainTex", tex);
        foreach (var rend in planeRenderers)
        {
            if (!setSharedMaterial)
                rend.material.SetTexture("_MainTex", tex);
        }
    }

    float ComputePlaneAspect(MeshFilter mf, Transform t)
    {
        if (mf == null || mf.sharedMesh == null) return t.localScale.x / t.localScale.z;

        Vector3 meshSize = mf.sharedMesh.bounds.size;
        float worldWidth = Mathf.Abs(meshSize.x * t.lossyScale.x);
        float worldHeight = Mathf.Abs(meshSize.z * t.lossyScale.z);
        return worldHeight > 0.0001f ? worldWidth / worldHeight : 1f;
    }
}
