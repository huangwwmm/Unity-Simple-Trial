using UnityEngine;

public class Trail3DTest : hwmTrailBaseEmitter<hwmTrailSection>
{
    public float MoveSpeed;
    public Vector3 RotateSpeed;

    protected void Update()
    {
        transform.eulerAngles += RotateSpeed * Time.deltaTime;
        transform.position += transform.forward * MoveSpeed * Time.deltaTime;
    }
}