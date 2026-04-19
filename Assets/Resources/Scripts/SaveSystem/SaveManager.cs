using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class SaveManager: MonoBehaviour
{
    [Header("File Storage Config")]
    [SerializeField] private string fileName;
    public static SaveManager Instance {get; private set;}
    private GameData gameData; 

    private List<ISaveLoadable> allSaveLoadableObjects;
    private SaveFileHandler saveFileHandler;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Found more than one Data Persistence Manager in the scene.");
        }
        Instance = this;
    }

    void Start()
    {
        this.saveFileHandler = new SaveFileHandler(Path.Combine(Application.dataPath,"Saves"),fileName);
        this.allSaveLoadableObjects = FindAllSaveLoadableObjects();
    }

    public void NewGame()
    {
        this.gameData = new GameData();
    }
    public void LoadGame(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        
        this.gameData = saveFileHandler.LoadFile();

        if (this.gameData == null)
        {
            Debug.LogError("Error getting save file. Please open new game.");
            NewGame();
        }   

        foreach (ISaveLoadable entity in allSaveLoadableObjects)
        {
            entity.LoadGameData(this.gameData);
        }
    }
    

    public void SaveGame(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        
        foreach (ISaveLoadable entity in allSaveLoadableObjects)
        {
            entity.SaveGameData(ref this.gameData);
        }
        saveFileHandler.SaveFile(gameData);
    }

    public List<ISaveLoadable> FindAllSaveLoadableObjects()
    {
        IEnumerable<ISaveLoadable> saveLoadableObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveLoadable>();
        return new List<ISaveLoadable>(saveLoadableObjects);
    }

}
