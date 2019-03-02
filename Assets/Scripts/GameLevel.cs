using UnityEngine;

public class GameLevel : PersistableObject
{
    [SerializeField] private SpawnZone spawnZone;
    [SerializeField] private PersistableObject[] persistableObjects;

    public static GameLevel Current { get; private set; }

    private void OnEnable()
    {
        Current = this;

        if (persistableObjects == null)
        {
            persistableObjects = new PersistableObject[0];
        }
    }

    public Shape SpawnShape()
    {
        return spawnZone.SpawnShape();
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(persistableObjects.Length);

        for (int i = 0; i < persistableObjects.Length; i++)
        {
            persistableObjects[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
        int savedCount = reader.ReadInt();

        for (int i = 0; i < savedCount; i++)
        {
            persistableObjects[i].Load(reader);
        }
    }
}