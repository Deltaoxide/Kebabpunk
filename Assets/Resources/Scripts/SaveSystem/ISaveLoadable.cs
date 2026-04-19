using UnityEngine;

public interface ISaveLoadable
{
    public void LoadGameData(GameData gameSaveData);
    public void SaveGameData(ref GameData gameSaveData);
}
