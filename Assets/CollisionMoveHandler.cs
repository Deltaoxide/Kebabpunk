using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DragMovePhysicsHandler))]
public class CollisionMoveHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody2D SelfRB;
    private SortingGroup SelfSortingGroup;
    void Start()
    {
        SelfRB = GetComponent<Rigidbody2D>();

    }
    private void OnTriggerStay2D(Collider2D other)
    {
        // Only react if the other object is the same type (using a Tag or Layer)
        bool isDraggingOther = other.GetComponent<DragMovePhysicsHandler>().isDragging;
        Debug.Log("OnTriggerStay - ShotBy:" + gameObject.name + " - isDraggingOther: " + isDraggingOther);
        if (!isDraggingOther) 
        {
            // 1. Get the distance and direction needed to stop overlapping
            ColliderDistance2D dist = GetComponent<Collider2D>().Distance(other);

            // 2. If 'isOverlapped' is true, 'dist.distance' will be negative
            if (dist.isOverlapped)
            {
                // 3. Push this object away by the overlap amount
                // We multiply by a small factor (e.g., 0.1f) to make it a smooth "shove" 
                // rather than a teleport.
                Vector2 pushDir = dist.normal * Mathf.Abs(dist.distance);
                
                // Move this object out of the way
                SelfRB.MovePosition(SelfRB.position + pushDir);
            }
        }
    }
}
