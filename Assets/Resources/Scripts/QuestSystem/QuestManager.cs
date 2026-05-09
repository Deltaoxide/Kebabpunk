using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    private Dictionary<string, Quest> questMap;

    public CustomEvent_str onStartQuest;
    public CustomEvent_str onAdvanceQuest;
    public CustomEvent_str onFinishQuest;
    public CustomEvent_quest onQuestStateChange;

    private void Awake()
    {
        questMap = CreateQuestMap();

        Quest quest = GetQuestById("ProveYourDoner");
        Debug.Log(quest.info.displayName);

    }
    private void OnEnable()
    {
        onStartQuest.RegisterListener(StartQuest);
        onAdvanceQuest.RegisterListener(AdvanceQuest);
        onFinishQuest.RegisterListener(FinishQuest);
    }
    private void OnDisable()
    {
        onStartQuest.UnregisterListener(StartQuest);
        onAdvanceQuest.UnregisterListener(AdvanceQuest);
        onFinishQuest.UnregisterListener(FinishQuest);
    }
    private void Start()
    {
        foreach (Quest quest in questMap.Values)
        {
            onQuestStateChange.Invoke(quest);
        }
    }
    private Dictionary<string, Quest> CreateQuestMap()
    {
        QuestInfoSo[] allQuests = Resources.LoadAll<QuestInfoSo>("Quests");
        Dictionary<string, Quest> idToQuestMap = new Dictionary<string, Quest>();
        foreach (QuestInfoSo questInfo in allQuests)
        {
            if (idToQuestMap.ContainsKey(questInfo.id))
            {
                Debug.LogWarning("Duplicate ID found when creating quest map: " + questInfo.id);
            }
            idToQuestMap.Add(questInfo.id, new Quest(questInfo));
        }
        return idToQuestMap;
    }

    private Quest GetQuestById(string id)
    {
        Quest quest = questMap[id];
        if (quest == null)
        {
            Debug.LogError("ID not found in the Quest Map: " + id);
        }
        return quest;
    }

    private void StartQuest(string id)
    {
        
    }
    private void AdvanceQuest(string id)
    {
        
    }
    private void FinishQuest(string id)
    {
        
    }
}
