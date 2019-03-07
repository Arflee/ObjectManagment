using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : PersistableObject
{
    private const int saveVersion = 6;

    private int loadedLevelBuildIndex;

    private float creationProgress, destructionProgress;

    private List<Shape> _shapes;

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
        _shapes = new List<Shape>();

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
        for (int i = 0; i < _shapes.Count; i++)
        {
            _shapes[i].GameUpdate();
        }
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
        //
        //
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
            while (_shapes.Count > limit)
            {
                DestroyShape();
            }
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

        for (int i = 0; i < _shapes.Count; i++)
        {
            _shapes[i].Recycle();
        }

        _shapes.Clear();
    }

    //Выбирает рандомный индекс для списка
    //Меняет местами последний и выбранный и удаляет
    private void DestroyShape()
    {
        if (_shapes.Count > 0)
        {
            int index = Random.Range(0, _shapes.Count);
            _shapes[index].Recycle();
            int lastIndex = _shapes.Count - 1;
            _shapes[lastIndex].SaveIndex = index;
            _shapes[index] = _shapes[lastIndex];
            _shapes.RemoveAt(lastIndex);
        }
    }

    public void AddShape(Shape shape)
    {
        shape.SaveIndex = _shapes.Count;
        _shapes.Add(shape);
    }
    
    public Shape GetShape(int index)
    {
        return _shapes[index];
    }

    //Просто записываем числов шейпов в списке, Id и Id материала
    public override void Save(GameDataWriter writer)
    {
        writer.Write(_shapes.Count);
        writer.Write(Random.state);
        writer.Write(CreationSpeed);
        writer.Write(creationProgress);
        writer.Write(DestructionSpeed);
        writer.Write(destructionProgress);
        writer.Write(loadedLevelBuildIndex);
        GameLevel.Current.Save(writer);

        for (int i = 0; i < _shapes.Count; i++)
        {
            writer.Write(_shapes[i].OriginFactory.FactoryId);
            writer.Write(_shapes[i].ShapeId);
            writer.Write(_shapes[i].MaterialId);
            _shapes[i].Save(writer);
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
            for (int i = 0; i < _shapes.Count; i++)
            {
                _shapes[i].ResolveShapeInstances();
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