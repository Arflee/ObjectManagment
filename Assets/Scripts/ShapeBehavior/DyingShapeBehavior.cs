using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class DyingShapeBehavior : ShapeBehavior
{
    Vector3 originalScale;
    float duration, dyingAge;

    public override ShapeBehaviorType BehaviorType
    {
        get
        {
            return ShapeBehaviorType.Dying;
        }
    }

    public override bool GameUpdate(Shape shape)
    {
        float dyingDuration = shape.Age - dyingAge;
        if (dyingDuration < duration)
        {
            float s = 1f - dyingDuration / duration;
            s = (3f - 2f * s) * s * s;
            shape.transform.localScale = s * originalScale;
            return true;
        }

        shape.Die();
        return true;
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(originalScale);
        writer.Write(duration);
        writer.Write(dyingAge);
    }

    public override void Load(GameDataReader reader)
    {
        originalScale = reader.ReadVector3();
        duration = reader.ReadFloat();
        dyingAge = reader.ReadFloat();
    }

    public override void Recycle()
    {
        ShapeBehaviorPool<DyingShapeBehavior>.Reclaim(this);
    }

    public void Initialize(Shape shape, float duration)
    {
        originalScale = shape.transform.localScale;
        this.duration = duration;
        dyingAge = shape.Age;
        shape.MarkAsDying();
    }
}
