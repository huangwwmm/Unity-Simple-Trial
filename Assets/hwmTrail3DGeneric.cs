using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 当自己实现的条带段上希望记录额外数据时，可以利用C#的泛型传入一个mfTrail3DSection的子类，在其中添加自己需要的变量。
/// </summary>
/// <typeparam name="SectionT"></typeparam>
public class hwmTrail3DGeneric<SectionT> : MonoBehaviour where SectionT : hwmTrail3DSection, new()
{
    public Camera RendererCamera;

    /// <summary>
    /// 条带使用的材质
    /// </summary>
    public Material TrailMateiral;

    /// <summary>
    /// 每米条带折算多少贴图坐标，这个数值越高，条带纹理的重复度就越高，纹理看上去就越密
    /// </summary>
    public float WorldToTexcoordU = 1.0f;

    /// <summary>
    /// 条带顶端的宽度，也就是条带发射出时的起始宽度
    /// </summary>
    public float StartHalfWidth = 0.5f;

    /// <summary>
    /// 条带末端的宽度
    /// </summary>
    public float EndHalfWidth = 0.5f;

    /// <summary>
    /// 条带Section的寿命，也就是每段条带的寿命，这个数值越长条带在场景中停留的时间越长，拉出的条带Section总数自然也会上升
    /// </summary>
    public float LifeTime = 1.0f;

    /// <summary>
    /// 条带顶端的颜色，也就是条带发射出时的起始颜色
    /// </summary>
    public Color StartColor = Color.white;

    /// <summary>
    /// 条带末端颜色
    /// </summary>
    public Color EndColor = new Color(1, 1, 1, 0);

    /// <summary>
    /// 自动发射Section模式下。两个Section之间的最少间距，条带发射器同上一个条带Section的间距的平方如果小于这个数值的话，条带系统不会创建新的Section，而是移动头部Section的位置到发射点。
    /// </summary>
    public float AutoEmitMinVertexDistanceSquare = 0.01f;

    /// <summary>
    /// 自动发射Section模式下，条带发射器移动距离必须超过以下屏幕上的尺寸才会显示。这个数值是屏幕高的单位化数值，也就是说当这个值为1时，代表发射器必须走过屏幕底部移到顶部这么大的距离时才会创建新的Section
    /// </summary>
    public float AutoEmitMinVertexDistanceRelativeHeight = 1.0f;

    /// <summary>
    /// 当条带发射器距离相机超过以下距离时，条带停止发射
    /// </summary>
    public float AutoEmitStopWhenDistanceSquareToCameraIsGreaterThan = 250000;

    /// <summary>
    /// 当条带发射器处于屏幕外时停止发射条带？
    /// </summary>
    public bool AutoEmitStopWhenOutOfCamera = true;

    /// <summary>
    /// 当条带发射器距离相机大于此值时条带系统彻底停止工作
    /// </summary>
    public float HideWhenDistanceSquareToCameraIsGreaterThan = 360000;

    /// <summary>
    /// Trail所支持的最大Section数量，默认情况下每个Section对应两个三角形。需要注意的是，这个条带系统为了提升效率，避免动态内存分配，所有的IndexBuffer和VertexBuffer都是提前分配的！而且每次渲染时都是渲染的所有三角型。FIXME: 渲染所有三角型完全是不得已，因为Unity的SetIndices只接收一个数组作为参数，而不是数组+数量这个只能希望Unity未来能够提供这种接口来解决这个问题了。
    /// </summary>
    public int MaxTrailSectionCount = 32;

    /// <summary>
    /// 子类可以重载UpdateSections方法来更新Sections
    /// </summary>
    protected List<SectionT> m_Sections = new List<SectionT>();

    /// <summary>
    /// 子类可以重载UpdateBuffers方法实现自己的填充函数
    /// </summary>
    protected Vector3[] m_PositionBuffer;
    /// <summary>
    /// 子类可以重载UpdateBuffers方法实现自己的填充函数
    /// </summary>
    protected Color32[] m_ColorBuffer;
    /// <summary>
    /// 子类可以重载UpdateBuffers方法实现自己的填充函数
    /// </summary>
    protected Vector2[] m_UVBuffer;
    /// <summary>
    /// 子类可以重载UpdateBuffers方法实现自己的填充函数
    /// </summary>
    protected int[] m_IndexBuffer;

    private Mesh m_Mesh0;
    private Mesh m_Mesh1;
    private bool m_UsingMesh1;
    private Mesh m_DummyMesh;

    private GameObject m_TrailRendererGameObject;
    private MeshFilter m_MeshFilter;
    private MeshRenderer m_MeshRenderer;
    private Transform m_Transform;

    /// <summary>
    /// 所以Pool中的Section数量其实是MaxTrailSectionCount-1
    /// </summary>
    private Stack<SectionT> m_SectionPool = new Stack<SectionT>();

    private bool m_Emitting = true;
    private bool m_ManualEmit = false;
    private bool m_NewHead = true;

    private float m_SqrDistanceToCamera;

    private Quaternion m_PreviousRotation;

    public bool IsStopEmittingAndNoSectionLeft()
    {
        return m_Sections.Count <= 2 && !m_Emitting && !m_ManualEmit;
    }

    public void StartTrail()
    {
        if (m_Emitting)
        {
            Debug.LogWarning(string.Format("The trail({0}) has already started", name));
            return;
        }

        m_Emitting = true;
        m_NewHead = true;
    }

    public bool IsTrailStarted()
    {
        return m_Emitting;
    }

    public void EndTrail()
    {
        if (!m_Emitting)
        {
            Debug.LogWarning(string.Format("The trail({0}) has already ended", name));
            return;
        }

        m_Emitting = false;
    }

    public void ClearTrail()
    {
        while (m_Sections.Count > 0)
        {
            RemoveSection(m_Sections.Count - 1);
        }
        m_NewHead = true;
    }

    public void SetManualEmitting(bool manual)
    {
        m_ManualEmit = manual;
    }

    public void ManualEmit()
    {
        if (m_ManualEmit && m_Emitting)
        {
            TryAddSection(transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning(string.Format("Can't manual emit trail section for trail({0}), because it either not started or is not working in manual mode", name));
        }
    }

    public void UpdateMaterial()
    {
        m_MeshRenderer.sharedMaterial = TrailMateiral;
    }

    /// <summary>
    /// 如果希望在发射新的条带段时，能够对这个新产生的段上的数据做一些特殊处理（例如随机颜色），或者对自己扩展的变量进行初始化赋值时，重载本方法。
    /// </summary>
    protected virtual void SetupNewSection(Vector3 position, Quaternion rotation, ref SectionT newSection, bool newHead, int iPreviousSection)
    {
        newSection.Position = position;
        newSection.BirthTime = Time.time;
        newSection.RightDirection = rotation * Vector3.right;
        newSection.NormalizedAge = 0;
        newSection.HalfWidth = StartHalfWidth;
        newSection.Color = StartColor;
        newSection.Connect = newHead ? 0 : 1;

        newSection.TexcoordU = newHead
            ? 0
            : (transform.position - m_Sections[iPreviousSection].Position).magnitude * WorldToTexcoordU + m_Sections[iPreviousSection].TexcoordU;
    }


    /// <summary>
    /// 用于更新条带段的属性。自定制的特殊动画、过渡等逻辑请写在这个方法的重载中。（见osPlaneTrail范例中如何实现的“自定制曲线的条带颜色变化”）
    /// </summary>
    protected virtual void UpdateSections()
    {
        RemoveOldSections();

        for (int i = 0; i < m_Sections.Count; i++)
        {
            SectionT currentSection = m_Sections[i];

            currentSection.NormalizedAge = Mathf.Clamp01((Time.time - currentSection.BirthTime) / LifeTime);
            currentSection.Color = Color.Lerp(StartColor, EndColor, currentSection.NormalizedAge);
            currentSection.HalfWidth = Mathf.Lerp(StartHalfWidth, EndHalfWidth, currentSection.NormalizedAge);
        }
    }

    protected void RemoveOldSections()
    {
        // Remove old sections
        int iLastOutdateSection = -1;
        float minBirthTime = Time.time - LifeTime;
        while (iLastOutdateSection + 1 < m_Sections.Count
               && m_Sections[iLastOutdateSection + 1].BirthTime < minBirthTime) // 满足这个条件说明这个Section过期了
        {
            ++iLastOutdateSection;
        }

        if (iLastOutdateSection > -1) // 说明有过期的Section
        {
            /* 断裂效果说明：例
				下图第一行是Section Index 第二行中"*"是Section "----"是画出来的Trail
				0    1    2    3    4
				*----*----*----*----*
				当有2个Section过期时，如果移除两个Section，结果如下图
			             2    3    4
				      ----*----*----*
					 ↑这里断裂
				为了避免断裂效果，我们只移除1个Section（iLastOutdateSection = 1）
				     1    2    3    4
				     *----*----*----*
			*/
            RemoveSection(0, iLastOutdateSection + 1 == m_Sections.Count
                ? m_Sections.Count // 所有的都过期了，我们可以彻底移除了
                : iLastOutdateSection); // 只要不是全部过期了就保留一个过期的Section，避免断裂效果	
        }
    }

    /// <summary>
    /// 用于将条带的Section转换为用于渲染的顶点、索引缓冲。有自定制的填充策略请写在这个方法的重载中。（例如可断裂的条带；更高性能的填充策略；定向Billboard等功能）
    /// </summary>
    protected virtual void UpdateBuffers(ref List<SectionT> sections)
    {
        for (int i = 0; i < sections.Count; i++)
        {
            SectionT iterSection = sections[i];

            // Generate vertices
            Vector3 vHalfWidth = iterSection.RightDirection * iterSection.HalfWidth;

            m_PositionBuffer[i * 4 + 0] = iterSection.Position - vHalfWidth;
            m_PositionBuffer[i * 4 + 1] = iterSection.Position + vHalfWidth;
            // fade colors out over time
            m_ColorBuffer[i * 4 + 0] = iterSection.Color;
            m_ColorBuffer[i * 4 + 1] = iterSection.Color;

            m_UVBuffer[i * 4 + 0] = new Vector2(iterSection.TexcoordU, 0);
            m_UVBuffer[i * 4 + 1] = new Vector2(iterSection.TexcoordU, 1);
        }

        for (int i = 0; i < sections.Count - 1; i++)
        {
            SectionT iterSection = sections[i + 1];

            m_PositionBuffer[i * 4 + 2] = m_PositionBuffer[i * 4 + iterSection.Connect * 4];
            m_PositionBuffer[i * 4 + 3] = m_PositionBuffer[i * 4 + iterSection.Connect * 5];
            // fade colors out over time
            m_ColorBuffer[i * 4 + 2] = m_ColorBuffer[i * 4 + 4];
            m_ColorBuffer[i * 4 + 3] = m_ColorBuffer[i * 4 + 5];

            m_UVBuffer[i * 4 + 2] = m_UVBuffer[i * 4 + 4];
            m_UVBuffer[i * 4 + 3] = m_UVBuffer[i * 4 + 5];
        }


        for (int i = (sections.Count - 1) * 4; i < m_PositionBuffer.Length; ++i)
        {
            m_PositionBuffer[i] = new Vector3(0, -100000.0f, 0);
        }
    }

    /// <summary>
    /// 默认的计算bound方法。
    /// </summary>
    /// <returns></returns>
    protected virtual Bounds RecaculateBound()
    {
        Bounds bound = new Bounds(m_Transform.position, Vector3.zero);
        if (m_Sections.Count > 0)
        {
            // 这种计算方式更适合处理一直朝某方向飞的条带，但不适合所有情况。。。
            // TODO: 考虑改造为使用头部、中间、尾部三个Section计算Bounds
            SectionT centerSection = m_Sections[m_Sections.Count / 2];
            SectionT lastSection = m_Sections[m_Sections.Count - 1];
            bound.Encapsulate(centerSection.Position);
            bound.Encapsulate(lastSection.Position);
        }
        return bound;
    }

    protected SectionT TryAddSection(Vector3 position, Quaternion rotation)
    {
        // 我们预留了一个Section时刻作为条带的头部
        if (m_Sections.Count == MaxTrailSectionCount - 1)
        {
            return null;
        }

        m_NewHead |= m_Sections.Count == 0;

        SectionT newSection;

        // FIXME: 这里可以进一步优化，封装一个CachedList专门做这种事情，就不用一个单独的State来Pop和Push了 [12/26/2013 ShenYuan]
        newSection = m_SectionPool.Pop();
        SetupNewSection(position, rotation, ref newSection, m_NewHead, m_Sections.Count - 1);
        m_Sections.Add(newSection);

        m_NewHead = false;

        return newSection;
    }

    protected void RemoveSection(int index)
    {
        // FIXME: 这里可以进一步优化，封装一个CachedList专门做这种事情，就不用一个单独的State来Pop和Push了 [12/26/2013 ShenYuan]
        m_SectionPool.Push(m_Sections[index]);
        m_Sections.RemoveAt(index);
    }

    protected void RemoveSection(int index, int length)
    {
        // FIXME: 这里可以进一步优化，封装一个CachedList专门做这种事情，就不用一个单独的State来Pop和Push了 [12/26/2013 ShenYuan]
        for (int i = index; i < index + length; ++i)
        {
            m_SectionPool.Push(m_Sections[i]);
        }
        m_Sections.RemoveRange(index, length);
    }

    protected void Awake()
    {
        // 所以Pool中的Section数量其实是MaxTrailSectionCount-1
        for (int i = 0; i < MaxTrailSectionCount - 1; i++)
        {
            m_SectionPool.Push(new SectionT());
        }

        m_TrailRendererGameObject = new GameObject("(TrailRenderer)" + name);
        m_MeshFilter = m_TrailRendererGameObject.AddComponent<MeshFilter>();
        m_Mesh0 = new Mesh();
        m_Mesh0.MarkDynamic();
        m_Mesh1 = new Mesh();
        m_Mesh1.MarkDynamic();
        m_UsingMesh1 = true;
        m_DummyMesh = new Mesh();
        // m_DummyMesh.vertices = new Vector3[1]{ Vector3.zero };
        // m_DummyMesh.SetIndices(new int[1]{0}, MeshTopology.Points, 0);
        m_MeshFilter.mesh = m_DummyMesh;

        m_MeshRenderer = m_TrailRendererGameObject.AddComponent<MeshRenderer>();
        m_MeshRenderer.sharedMaterial = TrailMateiral;
        hwmTrail3DMeshRenderer onWillRenderMessageSender = m_TrailRendererGameObject.AddComponent<hwmTrail3DMeshRenderer>();
        onWillRenderMessageSender.OnTrailMeshWillRender += OnTrailMeshWillRender;

        m_PositionBuffer = new Vector3[MaxTrailSectionCount * 4];
        m_Mesh0.vertices = m_PositionBuffer;
        m_Mesh1.vertices = m_PositionBuffer;
        m_ColorBuffer = new Color32[MaxTrailSectionCount * 4];
        m_UVBuffer = new Vector2[MaxTrailSectionCount * 4];

        m_IndexBuffer = new int[(MaxTrailSectionCount - 1) * 6];
        for (int i = 0; i < MaxTrailSectionCount - 1; i++)
        {
            m_IndexBuffer[i * 6] = i * 4;
            m_IndexBuffer[i * 6 + 1] = i * 4 + 1;
            m_IndexBuffer[i * 6 + 2] = i * 4 + 2;

            m_IndexBuffer[i * 6 + 3] = i * 4 + 2;
            m_IndexBuffer[i * 6 + 4] = i * 4 + 1;
            m_IndexBuffer[i * 6 + 5] = i * 4 + 3;
        }
        m_Mesh0.triangles = m_IndexBuffer;
        m_Mesh1.triangles = m_IndexBuffer;

        m_Transform = transform;
    }

    protected void LateUpdate()
    {
        m_SqrDistanceToCamera = (RendererCamera.transform.position - m_Transform.position).sqrMagnitude;

        bool hide = m_SqrDistanceToCamera > HideWhenDistanceSquareToCameraIsGreaterThan;
        if (hide)
        {
            m_MeshRenderer.enabled = false;
            ClearTrail();
            return;
        }
        else if (!m_MeshRenderer.enabled)
        {
            m_MeshRenderer.enabled = true;
        }

        if (!m_ManualEmit && m_Emitting)
        {
            if (AutoEmitStopWhenDistanceSquareToCameraIsGreaterThan < m_SqrDistanceToCamera
                || (AutoEmitStopWhenOutOfCamera && !IsInRendererCamera(m_Transform.position)))
            {
                // 这次不发射，而且下回如果有人发射的话还是一个新开始的条带
                m_NewHead = true;
            }
            else
            {
                if (m_Sections.Count == 0)
                {
                    // 在发射器上一帧的位置添加一个Section
                    //					TryAddSection(m_PreviousPosition, m_PreviousRotation);
                    //TODO 这里暂时这么写，等加了US的事件之后再改回去。改成上面这样。
                    TryAddSection(m_Transform.position, m_PreviousRotation);

                    // 在发射器当前位置添加一个Section作为HeadSection
                    TryAddSection(m_Transform.position, m_Transform.rotation);
                }

                // 将Head Section的属性设置为初始状态（Normalized Age 为1），并将Position和Rotation设置为同当前发射器一致
                SectionT headSection = m_Sections[m_Sections.Count - 1];
                int iNextToHead = m_Sections.Count < 2 ? 0 : m_Sections.Count - 2;
                Vector3 headDirection = m_Transform.position - m_Sections[iNextToHead].Position;
                //位置设置成 m_Transform.position + headDirection*.01f， 为的是不让head和次head两个section重合。
                //否则，两个section位置重合，导致计算出的左右展开vector长度为0。-- yuenze 2014-10-28 15:26:31
                SetupNewSection(m_Transform.position - headDirection * .01f, m_Transform.rotation, ref headSection,
                    headSection.Connect == 0, iNextToHead);

                float minVertexDisntanceSqr = AutoEmitMinVertexDistanceSquare;
                float rendererCameraHalfTanFOV = Mathf.Tan(RendererCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

                float sqrDisntaceRelativeMinVertex = rendererCameraHalfTanFOV *
                                                     AutoEmitMinVertexDistanceRelativeHeight;
                sqrDisntaceRelativeMinVertex *= sqrDisntaceRelativeMinVertex;
                sqrDisntaceRelativeMinVertex *= m_SqrDistanceToCamera;

                minVertexDisntanceSqr = Mathf.Max(minVertexDisntanceSqr, sqrDisntaceRelativeMinVertex);

                float vetexDistanceSq = (headDirection).sqrMagnitude;
                if (vetexDistanceSq > minVertexDisntanceSqr)
                {
                    TryAddSection(m_Transform.position, m_Transform.rotation);
                }
            }
        }

        UpdateSections();

        // TODO: 这里可以降低更新频率，或者使用一个固定尺寸、跟随发射器移动的包围球来替代每帧计算
        // 世界空间的包围球
        Bounds bound = RecaculateBound();

        m_Mesh0.bounds = bound;
        m_Mesh1.bounds = bound;
        m_DummyMesh.bounds = bound;

        m_PreviousRotation = m_Transform.rotation;
        m_Transform.hasChanged = false;
    }

    private bool IsInRendererCamera(Vector3 worldPosition)
    {
        Vector3 viewPosition = RendererCamera.WorldToViewportPoint(worldPosition);
        return viewPosition.x > 0 && viewPosition.x < 1 && viewPosition.y > 0 && viewPosition.y < 1 && viewPosition.z > 0;
    }

    protected void OnDestroy()
    {
        Destroy(m_TrailRendererGameObject);
    }

    protected void OnDisable()
    {
        // 条带系统停止时要同时停止Renderer
        if (m_MeshRenderer) // 如果MeshRenderer先销毁（例如退出场景时），这里会为null
        {
            m_MeshRenderer.enabled = false;
        }
    }

    protected void OnEnable()
    {
        // 但是条带系统启动时是不需要手动启动Renderer的，因为Update中会做这件事情
        m_PreviousRotation = transform.rotation;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        GUIStyle guiStyle = new GUIStyle();
        guiStyle.fontSize = 15;
        guiStyle.normal.textColor = Color.green;

        for (int i = 0; i < m_Sections.Count; ++i)
        {
            SectionT section = m_Sections[i];
            if (section.Connect == 1)
            {
                Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            }
            else
            {
                Gizmos.color = new Color(1.0f, 0, 0, 0.5f);
            }

            float radius = Mathf.Lerp(StartHalfWidth, EndHalfWidth, section.NormalizedAge);
            Gizmos.DrawWireSphere(section.Position, radius);
            UnityEditor.Handles.Label(section.Position, (i).ToString(), guiStyle);

        }

        for (int i = 0; i < m_PositionBuffer.Length; i++)
        {
            if (i % 2 == 0)
            {
                Vector3 start = m_PositionBuffer[i];
                Vector3 end = m_PositionBuffer[i + 1];

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(start, end);

                //				Vector3 dir = (end - start).normalized;

                if (i % 4 == 0)
                {
                    Gizmos.color = Color.green;
                    guiStyle.normal.textColor = Color.green;
                    //Gizmos.DrawSphere(start, 0.05f);
                    //					UnityEditor.Handles.Label(start + Vector3.right*.2f, i.ToString(), guiStyle);
                    Gizmos.color = Color.red;
                    guiStyle.normal.textColor = Color.red;
                    //Gizmos.DrawSphere(end, 0.05f);
                    //					UnityEditor.Handles.Label(end - Vector3.right * .2f, (i + 1).ToString(), guiStyle); 
                }
                else
                {
                    Gizmos.color = Color.green;
                    guiStyle.normal.textColor = Color.green;
                    //Gizmos.DrawSphere(start, 0.05f);
                    //					UnityEditor.Handles.Label(start + Vector3.right * .2f + Vector3.forward * .2f, i.ToString(), guiStyle);
                    Gizmos.color = Color.red;
                    guiStyle.normal.textColor = Color.red;
                    //Gizmos.DrawSphere(end, 0.05f);
                    //					UnityEditor.Handles.Label(end - Vector3.right * .2f + Vector3.forward * .2f, (i + 1).ToString(), guiStyle); 
                }

            }
        }
        Bounds bounds = m_MeshRenderer.bounds;
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(bounds.center, bounds.extents.magnitude);
    }
#endif

    private void OnTrailMeshWillRender()
    {
        // TODO: 对于非Billboard的条带，每帧只最多需要填充一次 [12/25/2013 ShenYuan]
        m_UsingMesh1 = !m_UsingMesh1;

        if (m_Sections.Count >= 2)
        {
            UpdateBuffers(ref m_Sections);

            Mesh fillMesh = m_UsingMesh1 ? m_Mesh1 : m_Mesh0;
            fillMesh.vertices = m_PositionBuffer;
            fillMesh.colors32 = m_ColorBuffer;
            fillMesh.uv = m_UVBuffer;

            m_MeshFilter.mesh = fillMesh;
        }
        else
        {
            m_MeshFilter.mesh = m_DummyMesh;
        }
    }
}