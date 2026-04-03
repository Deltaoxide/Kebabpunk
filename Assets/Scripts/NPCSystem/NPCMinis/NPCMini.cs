using System.Collections;
using UnityEngine;

public class NPCMini : MonoBehaviour
{
    private float minWaitSecond = 5f;
    private float maxWaitSecond = 10f;
    private WaypointManager waypointManager;
    private NPCNav npcNav;

    //NPC Data
    [SerializeField] private NPC npc; //Remove serializefield and make a npcmini spawner later

    private bool isWaiting;
    private bool isMovingToDestination;
    public Vector2 GetPosition()
    {
        return transform.position;
    }

    void Awake()
    {
        npcNav = GetComponent<NPCNav>();
    }
    void Start()
    {

        waypointManager = FindFirstObjectByType<WaypointManager>(FindObjectsInactive.Include);
        npcNav.OnArrivedAtDest += ArrivedAtDest;

        isWaiting = false;
        MakeDecision();
    }
    

    void MakeDecision()
    {
        if (!isWaiting && !isMovingToDestination)
        {
            Vector3 newdest = waypointManager.GetRandomWaypointForNPC(npc.type);
            StartCoroutine(npcNav.MoveToPosition(newdest));
            isMovingToDestination=true;
        }
    }
    private void ArrivedAtDest()
    {
        StartCoroutine(Wait());
        isMovingToDestination = false;
    }
    private IEnumerator Wait()
    {
        isWaiting = true;
        yield return new WaitForSeconds(Random.Range(minWaitSecond,maxWaitSecond));
        isWaiting = false;
        MakeDecision();
    }
}
