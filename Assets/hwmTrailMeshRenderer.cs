using UnityEngine;

public class hwmTrailMeshRenderer : MonoBehaviour
{
    public delegate void OnTrailMeshWillRenderDelegate();
    public OnTrailMeshWillRenderDelegate OnTrailMeshWillRender = null;

    internal MeshFilter _MeshFilter;
    internal MeshRenderer _MeshRenderer;

    public void SetRendererEnable(bool enable)
    {
        if (_MeshRenderer.enabled != enable)
        {
            _MeshRenderer.enabled = enable;
        }
    }

    protected void Awake()
    {
        _MeshFilter = gameObject.GetComponent<MeshFilter>();
        if (_MeshFilter == null)
        {
            _MeshFilter = gameObject.AddComponent<MeshFilter>();
        }

        _MeshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (_MeshRenderer == null)
        {
            _MeshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
    }

    protected void OnDestroy()
    {
        _MeshRenderer = null;
        _MeshFilter = null;
    }

    protected void OnWillRenderObject()
    {
        OnTrailMeshWillRender();
    }
}