using UnityEngine;
using System.Collections.Generic;

public class Trail3DTest : MonoBehaviour
{
    public float MoveSpeed;
    public Vector3 RotateSpeed;

    protected void Update()
    {
        transform.eulerAngles += RotateSpeed * Time.deltaTime;
        transform.position += transform.forward * MoveSpeed * Time.deltaTime;
    }

    [ContextMenu("T")]
    private void DEBUG_Test()
    {
        Vector3 emitter2RendererDir = transform.position - m_TrailRenderer.transform.position;
        Quaternion inverseEmitterRotation = Quaternion.Inverse(transform.rotation);
        m_TrailRenderer.transform.rotation *= inverseEmitterRotation;
        m_TrailRenderer.transform.position += emitter2RendererDir - inverseEmitterRotation * emitter2RendererDir - transform.position;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// 如果希望在发射新的条带段时，能够对这个新产生的段上的数据做一些特殊处理（例如随机颜色），或者对自己扩展的变量进行初始化赋值时，重载本方法
    /// </summary>
    protected void SetupSection(hwmTrailSection section, Vector3 position, Quaternion rotation, int previousSectionIndex)
    {
        bool isHeadSection = previousSectionIndex < 0;

        section.Position = position;
        section.BirthTime = Time.time;
        section.RightDirection = rotation * Vector3.right;
        section.NormalizedAge = 0;
        section.HalfWidth = StartHalfWidth;
        section.Color = StartColor;
        section.TexcoordU = isHeadSection
            ? 0
            : (position - m_Sections[previousSectionIndex].Position).magnitude * WorldToTexcoordU + m_Sections[previousSectionIndex].TexcoordU;
    }

#if UNITY_EDITOR
    [Header("Debug")]
    public bool DEBUG_DrawGizmos = true;
#endif

    /// <summary>
    /// 渲染这个Trail的相机
    /// </summary>
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
    /// 当条带发射器到相机距离的平方小于此值时条带系统工作，大于此值时清除条带
    /// </summary>
    public float DisplayWhenMe2CameraDistanceSqrLessThan = 360000;

    /// <summary>
    /// Trail所支持的最大Section数量，默认情况下每个Section对应两个三角形。需要注意的是，这个条带系统为了提升效率，避免动态内存分配，所有的IndexBuffer和VertexBuffer都是提前分配的！而且每次渲染时都是渲染的所有三角型
    /// FIXME 渲染所有三角型完全是不得已，因为Unity的SetIndices只接收一个数组作为参数，而不是数组+数量这个只能希望Unity未来能够提供这种接口来解决这个问题了
    /// </summary>
    public int MaxSectionCount = 32;

    /// <summary>
    /// 是否在Awake时自动开始Trail
    /// </summary>
    public bool TrailAutomatically = true;

    [Header("Auto Emitting")]
    /// <summary>
    /// 条带发射器在世界空间中移动的距离的平方超过这个值时会发射新的Section，否则会把HeadSection挪到发射器位置
    /// 同时满足<see cref="AutoEmittingAddSectionWhenMeMoveDistanceSqrInWorldSpcaeGreaterThen"/>和<see cref="AutoEmittingAddSectionWhenMeMoveDistanceInScreenSpaceGreaterThen"/>时才会发射新的Section
    /// </summary>
    public float AutoEmittingAddSectionWhenMeMoveDistanceSqrInWorldSpcaeGreaterThen = 0.01f;
    /// <summary>
    /// 条带发射器在屏幕上移动的距离除以屏幕高度超过这个值时会发射新的Section，否则会把HeadSection挪到发射器位置
    /// </summary>
    public float AutoEmittingAddSectionWhenMeMoveDistanceInScreenSpaceGreaterThen = 1.0f;
    /// <summary>
    /// 当条带发射器到相机距离的平方小于此值时，会自动发射条带
    /// </summary>
    public float AutoEmittingWhenMe2CameraDistanceSqrIsLessThan = 250000;
    /// <summary>
    /// 当条带发射器处于屏幕外是否自动发射条带
    /// </summary>
    public bool EnableAutoEmittingWhenMeOutOfCamera = true;


    /// <summary>
    /// Section数量其实是<see cref="MaxSectionCount"/> - 1
    /// 子类可以重载UpdateSections方法来更新Sections
    /// TODO 自己实现一个CachedList以优化效率
    /// </summary>
    protected List<hwmTrailSection> m_Sections = new List<hwmTrailSection>();

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

    /// <summary>
    /// 这个类是条带的发射器，本地空间的
    /// 渲染器是世界空间的
    /// </summary>
    protected hwmTrailMeshRenderer m_TrailRenderer;

    /// <summary>
    /// Mesh0和Mesh1用于双缓冲？不是很确定
    /// </summary>
    private Mesh m_Mesh0;
    private Mesh m_Mesh1;
    private bool m_UsingMesh1;
    /// <summary>
    /// 当Trail没有Section时渲染这个空的Mesh
    /// </summary>
    private Mesh m_DummyMesh;

    /// <summary>
    /// 是否正在发着Trail
    /// TODO 用一个EmittingState的枚举来取代<see cref="m_IsEmitting"/>和<see cref="m_IsManualEmit"/>
    /// </summary>
    private bool m_IsEmitting = false;
    /// <summary>
    /// 为false时，会在LateUpdate中自动添加Section
    /// 为true时，需要主动调用<see cref="ManualEmit"/>添加Section
    /// </summary>
    private bool m_IsManualEmit = false;

    /// <summary>
    /// 发射器到相机距离的平方
    /// </summary>
    private float m_Me2CameraDistanceSqr;

    public void StartTrail()
    {
        if (m_IsEmitting)
        {
            Debug.LogWarning(string.Format("The trail({0}) has already started", name));
            return;
        }

        m_IsEmitting = true;
    }

    public void StopTrail()
    {
        if (!m_IsEmitting)
        {
            Debug.LogWarning(string.Format("The trail({0}) has already ended", name));
            return;
        }

        m_IsEmitting = false;
    }

    public void ClearTrail()
    {
        m_Sections.Clear();
    }

    public bool IsEmitting()
    {
        return m_IsEmitting;
    }

    public void SetManualEmit(bool manual)
    {
        m_IsManualEmit = manual;
    }

    /// <summary>
    /// 在发射器当前位置添加Section
    /// </summary>
    public void ManualEmit()
    {
        if (m_IsManualEmit && m_IsEmitting)
        {
            TryAddSection(base.transform.position, base.transform.rotation);
        }
        else
        {
            Debug.LogWarning(string.Format("Can't manual emit trail section for trail({0}), because it either not started or is not working in manual mode", name));
        }
    }


    /// <summary>
    /// 用于更新条带段的属性。自定制的特殊动画、过渡等逻辑请写在这个方法的重载中。（见osPlaneTrail范例中如何实现的“自定制曲线的条带颜色变化”）
    /// </summary>
    protected void UpdateSections()
    {
        for (int iSection = 0; iSection < m_Sections.Count; iSection++)
        {
            hwmTrailSection currentSection = m_Sections[iSection];

            currentSection.NormalizedAge = Mathf.Clamp01((Time.time - currentSection.BirthTime) / LifeTime);
            currentSection.Color = Color.Lerp(StartColor, EndColor, currentSection.NormalizedAge);
            currentSection.HalfWidth = Mathf.Lerp(StartHalfWidth, EndHalfWidth, currentSection.NormalizedAge);
        }
    }

    protected void UpdateRemoveDeadSections()
    {
        int lastDeadSectionIndex = -1;
        float minLiveBirthTime = Time.time - LifeTime;
        for (int iSection = 0; iSection < m_Sections.Count; iSection++)
        {
            hwmTrailSection iterSection = m_Sections[iSection];
            if (iterSection.BirthTime > minLiveBirthTime)
            {
                lastDeadSectionIndex = iSection - 1;
                break;
            }
        }

        if (lastDeadSectionIndex > -1) // 说明有过期的Section
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
            m_Sections.RemoveRange(0, lastDeadSectionIndex + 1 == m_Sections.Count
                ? m_Sections.Count // 所有的都过期了，我们可以彻底移除了
                : lastDeadSectionIndex); // 只要不是全部过期了就保留一个过期的Section，避免断裂效果	
        }
    }

    /// <summary>
    /// 当前Section数量超过<see cref="MaxSectionCount"/>时不会添加Section
    /// </summary>
    protected hwmTrailSection TryAddSection(Vector3 position, Quaternion rotation)
    {
        // TODO 不能添加Section会导致条带发射器终止发射，暂时的解决方法是把MaxSectionCount配的很大。预计以后顶点数可以动态改变
        if (m_Sections.Count == MaxSectionCount - 1)
        {
            return null;
        }

        hwmTrailSection newSection = new hwmTrailSection();
        m_Sections.Add(newSection);
        SetupSection(newSection, position, rotation, m_Sections.Count - 2);

        return newSection;
    }

    /// <summary> 
    /// 用于将条带的Section转换为用于渲染的顶点、索引缓冲
    /// 可以重载这个方法实现特定的填充策略，例如：
    ///     可断裂的条带
    ///     更高性能的填充策略
    ///     定向Billboard
    /// </summary>
    protected void UpdateBuffers()
    {
        // UNDONE 这里有点蒙蔽啊。懒得看了，直接问祝锐把
        for (int iSection = 0; iSection < m_Sections.Count; iSection++)
        {
            hwmTrailSection iterSection = m_Sections[iSection];

            // Generate vertices
            Vector3 vHalfWidth = iterSection.RightDirection * iterSection.HalfWidth;

            m_PositionBuffer[iSection * 4 + 0] = iterSection.Position - vHalfWidth;
            m_PositionBuffer[iSection * 4 + 1] = iterSection.Position + vHalfWidth;
            // fade colors out over time
            m_ColorBuffer[iSection * 4 + 0] = iterSection.Color;
            m_ColorBuffer[iSection * 4 + 1] = iterSection.Color;

            m_UVBuffer[iSection * 4 + 0] = new Vector2(iterSection.TexcoordU, 0);
            m_UVBuffer[iSection * 4 + 1] = new Vector2(iterSection.TexcoordU, 1);
        }

        for (int iSection = 0; iSection < m_Sections.Count - 1; iSection++)
        {
            hwmTrailSection iterSection = m_Sections[iSection + 1];
            // TODO 就这里不懂，找祝锐
            m_PositionBuffer[iSection * 4 + 2] = m_PositionBuffer[iSection * 4 + Mathf.Min(iSection , 1) * 4];
            m_PositionBuffer[iSection * 4 + 3] = m_PositionBuffer[iSection * 4 + Mathf.Min(iSection, 1) * 5];
            // fade colors out over time
            m_ColorBuffer[iSection * 4 + 2] = m_ColorBuffer[iSection * 4 + 4];
            m_ColorBuffer[iSection * 4 + 3] = m_ColorBuffer[iSection * 4 + 5];

            m_UVBuffer[iSection * 4 + 2] = m_UVBuffer[iSection * 4 + 4];
            m_UVBuffer[iSection * 4 + 3] = m_UVBuffer[iSection * 4 + 5];
        }


        for (int iSection = (m_Sections.Count - 1) * 4; iSection < m_PositionBuffer.Length; ++iSection)
        {
            // TODO 这里也不懂。这样为什么不会渲染出从最后一个Section到这个位置的Trail？是被裁剪掉了么。不是会把一个三角形切开，然后渲染在屏幕中的部分么。为什么全裁掉了
            m_PositionBuffer[iSection] = new Vector3(0, -100000.0f, 0);
        }
    }

    /// <summary>
    /// 计算Trail世界空间的包围球
    /// 如果当前没有Section，则返回中心为发射器的位置，大小为0的Bounds
    /// TODO 优化计算方式，降低计算频率
    ///     这种计算方式更适合处理一直朝某方向飞的条带
    ///     遍历所有的Section计算Bounds太废了，又不可能实现通用且高效的算法
    ///     考虑让子类重载，根据不同情况去优化计算
    /// </summary>
    protected Bounds CaculateBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        if (m_Sections.Count > 0)
        {
            hwmTrailSection centerSection = m_Sections[m_Sections.Count / 2];
            hwmTrailSection lastSection = m_Sections[m_Sections.Count - 1];
            bounds.Encapsulate(centerSection.Position);
            bounds.Encapsulate(lastSection.Position);
        }
        return bounds;
    }

    protected void UpdateAutoEmitting()
    {
        if (!m_IsEmitting
            || m_IsManualEmit)
        {
            return;
        }

        if (!(m_Me2CameraDistanceSqr < AutoEmittingWhenMe2CameraDistanceSqrIsLessThan
            && (EnableAutoEmittingWhenMeOutOfCamera
                || IsInRendererCamera(transform.position))))
        {
            return;
        }

        Vector3 emitterPosition_RendererSpace = Quaternion.Inverse(m_TrailRenderer.transform.rotation) * (transform.position - m_TrailRenderer.transform.position);
        Quaternion emitterRotation_RendererSpace = transform.rotation * Quaternion.Inverse(m_TrailRenderer.transform.rotation);
        if (m_Sections.Count == 0)
        {
            // 在发射器上一帧的位置添加一个Section
            TryAddSection(emitterPosition_RendererSpace, emitterRotation_RendererSpace);
            // 在发射器当前位置添加一个Section作为HeadSection,
            TryAddSection(emitterPosition_RendererSpace, emitterRotation_RendererSpace);
        }
        else
        {
            #region 判断是否需要新的Section
            // TDOO 如果FOV不经常变动，可以把这个结果Cache下来。或者把这个值放在相机的管理中每帧计算
            float cameraHalfTanFOV = Mathf.Tan(RendererCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            // 把需要发射新Section的发射器在屏幕上移动的距离换算成世界空间的距离
            float minMeMoveDistanceSqr = cameraHalfTanFOV * AutoEmittingAddSectionWhenMeMoveDistanceInScreenSpaceGreaterThen;
            minMeMoveDistanceSqr *= minMeMoveDistanceSqr;
            minMeMoveDistanceSqr *= m_Me2CameraDistanceSqr;
            // 需要同时满足世界空间距离和屏幕空间距离
            minMeMoveDistanceSqr = Mathf.Max(AutoEmittingAddSectionWhenMeMoveDistanceSqrInWorldSpcaeGreaterThen, minMeMoveDistanceSqr);
            bool needAddSection = (transform.position - m_Sections[m_Sections.Count - 2].Position).sqrMagnitude > minMeMoveDistanceSqr;
            #endregion
            if (needAddSection)
            {
                TryAddSection(emitterPosition_RendererSpace, emitterRotation_RendererSpace);
            }
            else
            {
                // 将HeadSection挪到发射器的位置
                hwmTrailSection headSection = m_Sections[m_Sections.Count - 1];
                SetupSection(headSection, emitterPosition_RendererSpace, emitterRotation_RendererSpace, m_Sections.Count - 2);
            }
        }
    }

    #region Unity Event
    protected void Awake()
    {
        m_TrailRenderer = new GameObject("(TrailRenderer)" + name).AddComponent<hwmTrailMeshRenderer>();
        m_TrailRenderer.OnTrailMeshWillRender += OnTrailMeshWillRender;

        #region Create Buffers
        // TODO 这里应该可以优化，减少Buffer数量
        // 魔法数字4: 一个Section是一个长方形Mesh，一个长方形4个顶点
        m_PositionBuffer = new Vector3[MaxSectionCount * 4];
        m_ColorBuffer = new Color32[MaxSectionCount * 4];
        m_UVBuffer = new Vector2[MaxSectionCount * 4];
        m_IndexBuffer = new int[(MaxSectionCount - 1) * 6];
        for (int iIndex = 0; iIndex < MaxSectionCount - 1; iIndex++)
        {
            m_IndexBuffer[iIndex * 6] = iIndex * 4;
            m_IndexBuffer[iIndex * 6 + 1] = iIndex * 4 + 1;
            m_IndexBuffer[iIndex * 6 + 2] = iIndex * 4 + 2;

            m_IndexBuffer[iIndex * 6 + 3] = iIndex * 4 + 2;
            m_IndexBuffer[iIndex * 6 + 4] = iIndex * 4 + 1;
            m_IndexBuffer[iIndex * 6 + 5] = iIndex * 4 + 3;
        }
        #endregion

        // Mesh要放在Create Buffer后面
        m_Mesh0 = CreateDefaultMesh();
        m_Mesh1 = CreateDefaultMesh();
        m_DummyMesh = new Mesh();

        m_TrailRenderer._MeshFilter.mesh = m_DummyMesh;
        m_TrailRenderer._MeshRenderer.sharedMaterial = TrailMateiral;

        if (TrailAutomatically)
        {
            StartTrail();
        }
    }

    protected void OnDestroy()
    {
        // 已知在Editor下结束游戏时m_TrailRenderer可能为null
        if (m_TrailRenderer != null)
        {
            m_TrailRenderer.OnTrailMeshWillRender -= OnTrailMeshWillRender;
            Destroy(m_TrailRenderer.gameObject);
            m_TrailRenderer = null;
        }
    }

    protected void LateUpdate()
    {
        #region Check need display trail
        bool needDisplay;
        if (RendererCamera == null)
        {
            needDisplay = false;
        }
        else
        {
            m_Me2CameraDistanceSqr = (RendererCamera.transform.position - transform.position).sqrMagnitude;
            needDisplay = m_Me2CameraDistanceSqr < DisplayWhenMe2CameraDistanceSqrLessThan;
        }
        #endregion

        m_TrailRenderer.SetRendererEnable(needDisplay);
        if (!needDisplay)
        {
            ClearTrail();
            return;
        }

        UpdateAutoEmitting();
        UpdateRemoveDeadSections();
        UpdateSections();

        Bounds bound = CaculateBounds();

        m_Mesh0.bounds = bound;
        m_Mesh1.bounds = bound;
        m_DummyMesh.bounds = bound;
    }

    protected void OnDisable()
    {
        // 条带系统停止时要同时停止Renderer
        m_TrailRenderer.SetRendererEnable(false);
    }

    protected void OnEnable()
    {
        // 但是条带系统启动时是不需要手动启动Renderer的，因为Update中会做这件事情
        //m_MeshRenderer.enabled = true;
    }

    protected void OnTrailMeshWillRender()
    {
        Mesh fillMesh;
        if (m_Sections.Count > 1)
        {
            UpdateBuffers();

            m_UsingMesh1 = !m_UsingMesh1;
            fillMesh = m_UsingMesh1 ? m_Mesh1 : m_Mesh0;
            fillMesh.vertices = m_PositionBuffer;
            fillMesh.colors32 = m_ColorBuffer;
            fillMesh.uv = m_UVBuffer;
        }
        else
        {
            fillMesh = m_DummyMesh;
        }
        m_TrailRenderer._MeshFilter.mesh = fillMesh;
    }

#if UNITY_EDITOR
    protected void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !DEBUG_DrawGizmos)
        {
            return;
        }

        GUIStyle guiStyle = new GUIStyle();
        guiStyle.fontSize = 15;
        guiStyle.normal.textColor = Color.green;

        for (int iSection = 0; iSection < m_Sections.Count; ++iSection)
        {
            hwmTrailSection iterSection = m_Sections[iSection];
            Gizmos.color = iSection == 0
                ? new Color(1.0f, 0, 0, 0.5f)
                : new Color(1.0f, 1.0f, 1.0f, 0.5f);
            float radius = Mathf.Lerp(StartHalfWidth, EndHalfWidth, iterSection.NormalizedAge);
            Gizmos.DrawWireSphere(iterSection.Position, radius);
            UnityEditor.Handles.Label(iterSection.Position, (iSection).ToString(), guiStyle);
        }

        for (int i = 0; i < m_PositionBuffer.Length; i++)
        {
            if (i % 2 == 0)
            {
                Vector3 start = m_PositionBuffer[i];
                Vector3 end = m_PositionBuffer[i + 1];

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(start, end);

                if (i % 4 == 0)
                {
                    Gizmos.color = Color.green;
                    guiStyle.normal.textColor = Color.green;
                    Gizmos.DrawSphere(start, 0.05f);
                    UnityEditor.Handles.Label(start + Vector3.right * .2f, i.ToString(), guiStyle);
                    Gizmos.color = Color.red;
                    guiStyle.normal.textColor = Color.red;
                    Gizmos.DrawSphere(end, 0.05f);
                    UnityEditor.Handles.Label(end - Vector3.right * .2f, (i + 1).ToString(), guiStyle);
                }
                else
                {
                    Gizmos.color = Color.green;
                    guiStyle.normal.textColor = Color.green;
                    Gizmos.DrawSphere(start, 0.05f);
                    UnityEditor.Handles.Label(start + Vector3.right * .2f + Vector3.forward * .2f, i.ToString(), guiStyle);
                    Gizmos.color = Color.red;
                    guiStyle.normal.textColor = Color.red;
                    Gizmos.DrawSphere(end, 0.05f);
                    UnityEditor.Handles.Label(end - Vector3.right * .2f + Vector3.forward * .2f, (i + 1).ToString(), guiStyle);
                }
            }
        }
        Bounds bounds = m_TrailRenderer._MeshRenderer.bounds;
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(bounds.center, bounds.extents.magnitude);
    }

#endif
    #endregion

    private bool IsInRendererCamera(Vector3 worldPosition)
    {
        if (RendererCamera)
        {
            Vector3 viewPosition = RendererCamera.WorldToViewportPoint(worldPosition);
            return viewPosition.x > 0 && viewPosition.x < 1 && viewPosition.y > 0 && viewPosition.y < 1 && viewPosition.z > 0;
        }
        else
        {
            return false;
        }
    }

    private Mesh CreateDefaultMesh()
    {
        Mesh mesh = new Mesh();
        mesh.MarkDynamic();
        mesh.vertices = m_PositionBuffer;
        mesh.colors32 = m_ColorBuffer;
        mesh.uv = m_UVBuffer;
        mesh.triangles = m_IndexBuffer;
        return mesh;
    }
}