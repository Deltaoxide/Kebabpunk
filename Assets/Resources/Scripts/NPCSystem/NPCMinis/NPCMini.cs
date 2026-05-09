using System.Collections;
using UnityEngine;

public class NPCMini : MonoBehaviour,IInteractable
{
    private float minWaitSecond = 5f;
    private float maxWaitSecond = 10f;
    private WaypointManager waypointManager;
    private NPCNav npcNav;

    //NPC Data
    [SerializeField] private NPCSO npc; //Remove serializefield and make a npcmini spawner later

    private bool isWaiting;
    private bool isMovingToDestination;
    public bool isInteractable = true;

    [SerializeField] GameObject dialogueHint;

    
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
        npcNav.OnArrivedAtDest += ArrivedAtWaypoint;

        isWaiting = false;
        SelectNewWaypoint();
    }
    void SelectNewWaypoint()
    {
        if (!isWaiting && !isMovingToDestination)
        {
            Vector3 newdest = waypointManager.GetRandomWaypointForNPC(npc.type);
            StartCoroutine(npcNav.MoveToPosition(newdest));
            isMovingToDestination=true;
        }
    }
    private void ArrivedAtWaypoint()
    {
        StartCoroutine(Wait());
        isMovingToDestination = false;
    }
    private IEnumerator Wait()
    {
        isWaiting = true;
        yield return new WaitForSeconds(Random.Range(minWaitSecond,maxWaitSecond));
        isWaiting = false;
        SelectNewWaypoint();
    }

    public void IsInInteractionRange(bool is_in_range)
    {
        dialogueHint.SetActive(is_in_range);
    }

    public void Interact()
    {
        throw new System.NotImplementedException();
    }

    public bool CanInteract()
    {
        return isInteractable;
    }

}
