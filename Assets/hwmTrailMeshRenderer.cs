using UnityEngine;

public class hwmTrailMeshRenderer : MonoBehaviour
{
    public delegate void OnTrailMeshWillRenderDelegate();
    public OnTrailMeshWillRenderDelegate OnTrailMeshWillRender = null;

    void OnWillRenderObject()
    {
        OnTrailMeshWillRender();
    }
}