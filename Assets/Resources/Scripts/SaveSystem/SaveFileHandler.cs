using UnityEngine;
using System;
using System.IO;

public class SaveFileHandler 
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private string dataDirPath = "";
    private string dataFileName = "";
    
    public SaveFileHandler(string dataDirPath, string dataFileName)
    {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName;
    }

    public GameData LoadFile()
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        GameData loadedData = null;
        if (File.Exists(fullPath))
        {
            try
            {
                string dataToLoad = "";
                using (FileStream fs = new FileStream(fullPath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
            }
            catch (Exception e)
            {
                Debug.LogError("SaveFileHandler: Error LOADING data to File: " + fullPath + "\n" + e);
            }
        }
        return loadedData;
    }
    public void SaveFile(GameData gameData)
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToStore = JsonUtility.ToJson(gameData,true);
            using (FileStream fs = new FileStream(fullPath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.Write(dataToStore);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("SaveFileHandler: Error SAVING data to File: " + fullPath + "\n" + e);
        }
    }
}
