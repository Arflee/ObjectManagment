using UnityEngine;

public class CompositeSpawnZone : SpawnZone
{
    [SerializeField] private bool overrideConfig;
    [SerializeField] private bool sequential;

    [SerializeField] private SpawnZone[] spawnZones;

    private int nextSequentialIndex;

    public override Vector3 SpawnPoint
    {
        get
        {
            int index;
            if (sequential)
            {
                index = nextSequentialIndex++;
                if (nextSequentialIndex >= spawnZones.Length)
                {
                    nextSequentialIndex = 0;
                }
            }
            else
            {
                index = Random.Range(0, spawnZones.Length);
            }
            return spawnZones[index].SpawnPoint;
        }
    }

    public override void SpawnShapes()
    {
        if (overrideConfig)
        {
            base.SpawnShapes();
        }
        else
        {
            int index;
            if (sequential)
            {
                index = nextSequentialIndex++;
                if (nextSequentialIndex >= spawnZones.Length)
                {
                    nextSequentialIndex = 0;
                }
            }
            else
            {
                index = Random.Range(0, spawnZones.Length);
            }
            spawnZones[index].SpawnShapes();
        }
    }

    public override void Load(GameDataReader reader)
    {
        nextSequentialIndex = reader.ReadInt();
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(nextSequentialIndex);
    }
}