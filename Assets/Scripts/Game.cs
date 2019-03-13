using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : PersistableObject
{
    private const int saveVersion = 6;

    private int loadedLevelBuildIndex;

    private int dyingShapeCount;

    private float creationProgress, destructionProgress;

    private bool inGameUpdateLoop;

    private List<Shape> shapes;

    private List<ShapeInstance> killList, markAsDyingList;

    private Random.State mainRandomState;

    // Настраиваемые кнопки управления
    [SerializeField] private KeyCode createKey = KeyCode.C;
    [SerializeField] private KeyCode destroyKey = KeyCode.X;
    [SerializeField] private KeyCode newGameKey = KeyCode.N;
    [SerializeField] private KeyCode saveGameKey = KeyCode.S;
    [SerializeField] private KeyCode loadGameKey = KeyCode.L;

    [SerializeField] private int levelCount;
    [SerializeField] private bool reseedOnLoad;
    [SerializeField] private Slider creationSpeedSlider;
    [SerializeField] private Slider destructionSpeedSlider;
    [SerializeField] private float destroyDuration;

    // Подключаемые скрипты
    [SerializeField] private PersistentStorage storage;
    [SerializeField] private ShapeFactory[] shapeFactories;

    public float CreationSpeed { get; set; }
    public float DestructionSpeed { get; set; }
    public static Game Instance { get; set; }

    private void OnEnable()
    {
        Instance = this;

        if (shapeFactories[0].FactoryId != 0)
        {
            for (int i = 0; i < shapeFactories.Length; i++)
            {
                shapeFactories[i].FactoryId = i;
            }
        }
    }

    private void Start()
    {
        mainRandomState = Random.state;
        shapes = new List<Shape>();
        killList = new List<ShapeInstance>();
        markAsDyingList = new List<ShapeInstance>();

        if (Application.isEditor)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name.Contains("Level"))
                {
                    SceneManager.SetActiveScene(loadedScene);
                    loadedLevelBuildIndex = loadedScene.buildIndex;
                    return;
                }
            }
        }
        BeginNewGame();
        StartCoroutine(LoadLevel(1));
    }

    private void FixedUpdate()
    {
        inGameUpdateLoop = true;

        for (int i = 0; i < shapes.Count; i++)
        {
            shapes[i].GameUpdate();
        }

        inGameUpdateLoop = false;

        #region KeyControls
        //куча ифов для кнопок
        if (Input.GetKey(createKey))
        {
            GameLevel.Current.SpawnShapes();
        }
        else if (Input.GetKey(destroyKey))
        {
            DestroyShape();
        }
        else if (Input.GetKeyDown(newGameKey))
        {
            BeginNewGame();
            StartCoroutine(LoadLevel(loadedLevelBuildIndex));
        }
        else if (Input.GetKeyDown(saveGameKey))
        {
            storage.Save(this, saveVersion);
        }
        else if (Input.GetKeyDown(loadGameKey))
        {
            BeginNewGame();
            storage.Load(this);
        }
        else
        {
            for (int i = 1; i <= levelCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    BeginNewGame();
                    StartCoroutine(LoadLevel(i));
                    return;
                }
            }
        }
        #endregion

        //Управление скоростью создания шейпов
        creationProgress += Time.deltaTime * CreationSpeed;

        while (creationProgress >= 1f)
        {
            creationProgress -= 1f;
            GameLevel.Current.SpawnShapes();
        }

        //управление скоростью разрушения шейпов
        destructionProgress += Time.deltaTime * DestructionSpeed;

        while (destructionProgress >= 1f)
        {
            destructionProgress -= 1f;
            DestroyShape();
        }

        int limit = GameLevel.Current.PopulationLimit;
        if (limit > 0)
        {
            while (shapes.Count - dyingShapeCount > limit)
            {
                DestroyShape();
            }
        }
        if (killList.Count > 0)
        {
            for (int i = 0; i < killList.Count; i++)
            {
                if (killList[i].IsValid)
                {
                    KillImmediately(killList[i].Shape);
                }   
            }
            killList.Clear();
        }
        if (markAsDyingList.Count > 0)
        {
            for (int i = 0; i < markAsDyingList.Count; i++)
            {
                if (markAsDyingList[i].IsValid)
                {
                    MarkAsDyingImmediately(markAsDyingList[i].Shape);
                }
            }
            markAsDyingList.Clear();
        }
    }

    //Сносим все шейпы и чистим список
    private void BeginNewGame()
    {
        Random.state = mainRandomState;
        int seed = Random.Range(0, int.MaxValue) ^ (int)Time.unscaledTime;
        Random.InitState(seed);
        mainRandomState = Random.state;

        creationSpeedSlider.value = CreationSpeed = 0;
        destructionSpeedSlider.value = DestructionSpeed = 0;

        for (int i = 0; i < shapes.Count; i++)
        {
            shapes[i].Recycle();
        }

        dyingShapeCount = 0;
        shapes.Clear();
    }

    //Выбирает рандомный индекс для списка
    //Меняет местами последний и выбранный и удаляет
    private void DestroyShape()
    {
        if (shapes.Count - dyingShapeCount > 0)
        {
            Shape shape = shapes[Random.Range(dyingShapeCount, shapes.Count)];

            if (destroyDuration <= 0f)
            {
                KillImmediately(shape);
            }
            else
            {
                shape.AddBehavior<DyingShapeBehavior>().Initialize(shape, destroyDuration);
            }
        }
    }

    public void Kill(Shape shape)
    {
        if (inGameUpdateLoop)
        {
            killList.Add(shape);
        }
        else
        {
            KillImmediately(shape);
        }
    }

    public void KillImmediately(Shape shape)
    {
        int index = shape.SaveIndex;
        int lastIndex = shapes.Count - 1;

        shape.Recycle();

        if (index < dyingShapeCount && index < -- dyingShapeCount)
        {
            shapes[dyingShapeCount].SaveIndex = index;
            shapes[index] = shapes[dyingShapeCount];
            index = dyingShapeCount;
        }
        
        if (index < lastIndex)
        {
            shapes[lastIndex].SaveIndex = index;
            shapes[index] = shapes[lastIndex];
        }
        shapes.RemoveAt(lastIndex);
    }

    private void MarkAsDyingImmediately(Shape shape)
    {
        int index = shape.SaveIndex;

        if (index < dyingShapeCount)
        {
            return;
        }

        shapes[dyingShapeCount].SaveIndex = index;
        shapes[index] = shapes[dyingShapeCount];
        shape.SaveIndex = dyingShapeCount;
        shapes[dyingShapeCount++] = shape;
    }

    public void MarkAsDying(Shape shape)
    {
        if (inGameUpdateLoop)
        {
            markAsDyingList.Add(shape);
        }
        else
        {
            MarkAsDyingImmediately(shape);
        }
    }

    public bool IsMarkedAsDying(Shape shape)
    {
        return shape.SaveIndex < dyingShapeCount;
    }

    public void AddShape(Shape shape)
    {
        shape.SaveIndex = shapes.Count;
        shapes.Add(shape);
    }
    
    public Shape GetShape(int index)
    {
        return shapes[index];
    }

    //Просто записываем числов шейпов в списке, Id и Id материала
    public override void Save(GameDataWriter writer)
    {
        writer.Write(shapes.Count);
        writer.Write(Random.state);
        writer.Write(CreationSpeed);
        writer.Write(creationProgress);
        writer.Write(DestructionSpeed);
        writer.Write(destructionProgress);
        writer.Write(loadedLevelBuildIndex);
        GameLevel.Current.Save(writer);

        for (int i = 0; i < shapes.Count; i++)
        {
            writer.Write(shapes[i].OriginFactory.FactoryId);
            writer.Write(shapes[i].ShapeId);
            writer.Write(shapes[i].MaterialId);
            shapes[i].Save(writer);
        }
    }

    // По перевернутой версии смотрим подходящая или нет
    public override void Load(GameDataReader reader)
    {
        int version = reader.Version;

        if (version > saveVersion)
        {
            Debug.LogError("Unsupported future save version " + version);
            return;
        }
        StartCoroutine(LoadGame(reader));
    }

    private IEnumerator LoadGame(GameDataReader reader)
    {
        int version = reader.Version;
        int count = version <= 0 ? -version : reader.ReadInt();

        if (version >= 3)
        {
            Random.State state = reader.ReadRandomState();
            if (!reseedOnLoad)
            {
                Random.state = state;
            }

            CreationSpeed = creationSpeedSlider.value = reader.ReadFloat();
            creationProgress = reader.ReadFloat();
            DestructionSpeed = destructionSpeedSlider.value = reader.ReadFloat();
            destructionProgress = reader.ReadFloat();

            for (int i = 0; i < shapes.Count; i++)
            {
                shapes[i].ResolveShapeInstances();
            }
        }

        yield return LoadLevel(version < 2 ? 1 : reader.ReadInt());
        if (version >= 3)
        {
            GameLevel.Current.Load(reader);
        }

        for (int i = 0; i < count; i++)
        {
            int factoryId = version >= 5 ? reader.ReadInt() : 0;
            int shapeId = version > 0 ? reader.ReadInt() : 0;
            int materialId = version > 0 ? reader.ReadInt() : 0;

            Shape instance = shapeFactories[factoryId].GetShape(shapeId, materialId);
            instance.Load(reader);
        }
    }

    private IEnumerator LoadLevel(int levelBuildIndex)
    {
        enabled = false;
        if (loadedLevelBuildIndex > 0)
        {
            yield return SceneManager.UnloadSceneAsync(loadedLevelBuildIndex);
        }
        yield return SceneManager.LoadSceneAsync(levelBuildIndex, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelBuildIndex));
        loadedLevelBuildIndex = levelBuildIndex;
        enabled = true;
    }
}