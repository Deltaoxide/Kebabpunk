using UnityEngine;

public class ProveYourDonerStep : QuestStep
{
    public CustomEvent_str onDestReached;

    void OnEnable()
    {
        onDestReached.RegisterListener(DestReached);
    }
    void OnDisable()
    {
        onDestReached.UnregisterListener(DestReached);
    }
    [SerializeField] private string destID1;
    void DestReached(string reachedDest)
    {
        if(reachedDest == destID1)
        {
            FinishQuestStep();
        }
    }

}
