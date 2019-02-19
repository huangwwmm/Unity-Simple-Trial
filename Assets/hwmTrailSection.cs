using UnityEngine;

public class hwmTrailSection
{
    public float BirthTime;
    public Vector3 Position;
    public Vector3 RightDirection;
    public float NormalizedAge;
    public float HalfWidth;
    public Color32 Color;
    public float TexcoordU;
    /// <summary>
    /// TDDO 搞不懂啊搞不懂
    /// 我理解的是这个Section前面有没有链接其他Section，如果是HeadSection，这个值为0，否则为1
    /// 那应该用bool，为啥用int，还有<see cref="hwmTrailBaseEmitter{SectionT}.UpdateBuffers"/>中的用法是啥啊
    /// </summary>
    public int Connect;
}