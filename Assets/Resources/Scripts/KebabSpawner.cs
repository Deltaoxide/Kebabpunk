using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class KebabSpawner : MonoBehaviour,IDropHandler,ISaveLoadable
{
    [Header("Toppings")]
    public GameObject ToppingBox_Bread;
    
    [Header("Kebab Prefab")]
    public GameObject KebabPrefab;
    public int MaxKebabOnCounter = 1;
    
    [Header("Kebab Spawn Parent")]
    public GameObject Counter;

    private Camera _mainCamera;

    public int totalKebabsSpawned;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }


    public void OnDrop(PointerEventData eventData)
    {
        if (_mainCamera == null) return;
        if (eventData.pointerDrag == ToppingBox_Bread)
        {
            if(Counter.transform.childCount < MaxKebabOnCounter)
            {
                
                Vector2 worldpos = _mainCamera.ScreenToWorldPoint(new Vector3(eventData.position.x,eventData.position.y,0));
                Vector3 spawnpos = new(worldpos.x,worldpos.y,0);
                Instantiate(KebabPrefab,spawnpos,Quaternion.identity,Counter.transform);
                totalKebabsSpawned += 1;
            }
            else
            {
                Debug.Log("Max Kebabs On Counter Reached");
            }
        }
    }
/*    public void Debug_TotalKebabsSpawned(InputAction.CallbackContext ctx)
    {
        if(!ctx.performed) return;
        Debug.Log("Total Kebabs Spawned: "+ totalKebabsSpawned);
    }
*/

    public void LoadGameData(GameData gameSaveData)
    {
        totalKebabsSpawned = gameSaveData.totalKebabsSpawned;
    }

    public void SaveGameData(ref GameData gameSaveData)
    {
        gameSaveData.totalKebabsSpawned = totalKebabsSpawned;
    }
}