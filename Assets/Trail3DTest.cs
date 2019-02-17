using UnityEngine;

public class Trail3DTest : hwmTrailBaseEmiter<hwmTrailSection>
{
    public Vector3 Speed;

    protected void Update()
    {
        transform.position += Speed * Time.deltaTime;
    }
}