using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPCNav : MonoBehaviour
{
    // --- Component & State References ---
    private NavMeshAgent agent;
    private Vector2 direction;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    private bool lockCoroutine = false; // Coroutine lock prevents overlapping movement logic cycles
    public bool isMovingToDest;
    
    public event Action OnArrivedAtDest; // Event fired when the NPC successfully reaches its target or fails the path

    void Awake()
    {
        
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Start()
    {
        
        // Setup Input System: Grabs the second action (index 1) from the current map for mouse position
        //mousePos = GetComponent<PlayerInput>().currentActionMap.actions[1];
        
        // Standard 2D NavMesh setup: Prevents the agent from tilting or rotating in 3D space
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        isMovingToDest = false;
        
    }

    void Update()
    {
        direction = agent.velocity.normalized;
        if (direction != Vector2.zero)
        {
            animator.SetBool("isWalking",true);
            if (direction.x > 0)
            {
                spriteRenderer.flipX = false;
            }
            else if (direction.x < 0)
            {
                spriteRenderer.flipX = true;
            }
        }
        else
        {
            animator.SetBool("isWalking",false);
        }
    }
    /*
    // Manual navigation method triggered by Input System events (e.g., clicking the map)
    public void MoveOnCursor(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            // Convert screen space mouse coordinates to world space for the NavMesh
            Vector3 worldpos = mainCam.ScreenToWorldPoint(mousePos.ReadValue<Vector2>());
            
            // Force Z to 0 to ensure the destination stays on the 2D plane
            worldpos = new Vector3(worldpos.x,worldpos.y,0);
            agent.SetDestination(worldpos);
        }
    }*/

    // Managed movement coroutine that handles pathing lifecycle and status reporting
    public IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        // Safety check to ensure we don't try to calculate two paths simultaneously
        if(lockCoroutine)
        {
            Debug.LogError("Tried to run multiple coroutines at the same time.");
           yield break; 
        } 
        
        lockCoroutine = true;
        agent.SetDestination(targetPosition);
        
        // Essential: Wait for the NavMesh system to finish calculating the initial path
        yield return new WaitUntil(() => agent.pathPending == false);

        isMovingToDest = true;
        
        // Stay in the loop as long as the agent is still calculating or hasn't reached the destination
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            // Error handling: Stop if the agent finds a path that is blocked (Partial) or unreachable (Invalid)
            if (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning("Pathing failed for " + gameObject.name);
                break; 
            }
            
            // Poll distance/status every 0.1 seconds to save CPU cycles compared to every frame
            yield return new WaitForSeconds(0.1f);
        }
        
        // Clean up states and notify listeners (like NPCMini) that the movement cycle is over
        isMovingToDest=false;
        OnArrivedAtDest?.Invoke();
        lockCoroutine=false;

    }
}