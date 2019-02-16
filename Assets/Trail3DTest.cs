using UnityEngine;

public class Trail3DTest : hwmTrail3DGeneric<hwmTrail3DSection>
{
    public Vector3 Speed;

    protected void Update()
    {
        transform.position += Speed * Time.deltaTime;
    }
}