using UnityEngine;

public abstract class ShapeBehavior
#if UNITY_EDITOR
    : ScriptableObject
#endif
{
    public abstract ShapeBehaviorType BehaviorType { get; }

    public bool IsReclaimed { get; set; }

    public abstract bool GameUpdate(Shape shape);

    public abstract void Save(GameDataWriter writer);

    public abstract void Load(GameDataReader reader);

    public abstract void Recycle();

    public virtual void ResolveShapeInstances() { }

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (IsReclaimed)
        {
            Recycle();
        }
    }
#endif

}