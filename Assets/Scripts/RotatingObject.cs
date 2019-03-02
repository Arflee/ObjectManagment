using UnityEngine;

public class RotatingObject : PersistableObject
{
    [SerializeField] private Vector3 angularVelocity;

    private void FixedUpdate()
    {
        this.transform.Rotate(angularVelocity * Time.deltaTime);
    }
}