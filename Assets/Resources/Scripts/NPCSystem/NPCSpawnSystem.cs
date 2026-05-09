using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NPCSpawnSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private List<NPCSO> NPCDatas;
    [SerializeField] private GameObject NPCPanelPrefab;
    
    [Header("Variables")]
    public NPCSO CurrentNPCDataForPanel;

    private NPCDataManager nPCManager;
    private int NPCTodayNum;

    void Start()
    {
        NPCTodayNum = 0;
        nPCManager = NPCPanelPrefab.GetComponent<NPCDataManager>();
    }
    private NPCSO SelectRandomNPCData()
    {
        // TODO write better system to select npcdata.
        // Currently it picks a random NPC from list.

        if (NPCDatas.Count == 0)
        {
            Debug.LogWarning("The NPCDatas List is empty! Add some items in the Inspector.");
            return null;
        }
        int randomIndex = Random.Range(0, NPCDatas.Count);
        NPCSO selectedItem = NPCDatas[randomIndex];

        return selectedItem;
    }
    private NPCSO SelectOneByOne()
    {
        int selectNpcIndex = NPCTodayNum % NPCDatas.Count;
        NPCSO selectedItem = NPCDatas[selectNpcIndex];
        NPCTodayNum += 1;
        return selectedItem;

    }


    public void SendNPC()
    {
        
        CurrentNPCDataForPanel = SelectOneByOne(); // Maybe pass it in as an argument later ? TODO

        nPCManager.SetNpcData(CurrentNPCDataForPanel);
        nPCManager.SendNPC();
    }

    public void SpawnNPCWithKeyDEBUG(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }
        SendNPC();
    }
}
