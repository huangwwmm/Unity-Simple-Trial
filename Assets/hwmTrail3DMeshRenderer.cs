using UnityEngine;

public class hwmTrail3DMeshRenderer : MonoBehaviour
{
    public delegate void OnTrailMeshWillRenderDelegate();
    public OnTrailMeshWillRenderDelegate OnTrailMeshWillRender = null;

    void OnWillRenderObject()
    {
        OnTrailMeshWillRender();
    }
}
