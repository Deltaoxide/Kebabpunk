using System.Collections.Generic;

[System.Serializable]
public class NPCJsonData
{
    public string unique_id;
    public int visitCounter;
    public int totalInteractionPositive;
    public bool isLastInteractionPositive;
    public List<NPCQuestData> Quests;

}


[System.Serializable]
public class NPCQuestData
{
    public string NPCQuestID;
    public int QuestState;

}