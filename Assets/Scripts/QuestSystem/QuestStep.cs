using UnityEngine;

public class QuestStep : MonoBehaviour
{
    private bool isFinished = false;

    protected void FinishQuestStep()
    {
        if (!isFinished)
        {
            isFinished = true;
            Destroy(this.gameObject);
        }
    }
}
