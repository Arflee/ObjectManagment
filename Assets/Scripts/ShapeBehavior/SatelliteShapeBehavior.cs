using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SatelliteShapeBehavior : ShapeBehavior
{
    Shape focalShape;

    float frequency;

    Vector3 cosOffset, sinOffset;

    public override ShapeBehaviorType BehaviorType
    {
        get
        {
            return ShapeBehaviorType.Satellite;
        }
    }

    public override void GameUpdate(Shape shape)
    {
        float t = 2f * Mathf.PI * frequency * shape.Age;
        shape.transform.localPosition = 
            focalShape.transform.localPosition +
            cosOffset * Mathf.Cos(t) + sinOffset * Mathf.Sin(t);
    }

    public override void Load(GameDataReader reader)
    {
        
    }

    public override void Recycle()
    {
        ShapeBehaviorPool<SatelliteShapeBehavior>.Reclaim(this);
    }

    public override void Save(GameDataWriter writer)
    {
        
    }

    public void Initialize(Shape shape, Shape focalShape, float radius, float frequency)
    {
        this.focalShape = focalShape;
        this.frequency = frequency;
        Vector3 orbitAxis = Random.onUnitSphere;

        do
        {
            cosOffset = Vector3.Cross(orbitAxis, Random.onUnitSphere).normalized;
        }
        while (cosOffset.sqrMagnitude < 0.1f);

        sinOffset = Vector3.Cross(cosOffset, orbitAxis);

        cosOffset *= radius;
        sinOffset *= radius;

        GameUpdate(shape);
    }
}
