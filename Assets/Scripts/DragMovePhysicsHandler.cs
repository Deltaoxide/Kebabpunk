using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class DragMovePhysicsHandler : MonoBehaviour, IBeginDragHandler,IDragHandler,IEndDragHandler
{
    private Rigidbody2D SelfRB;
    private Vector2 dragOffset;
    public bool isDragging = false;
    private Vector2 targetPos;
    private Vector2 lastMousePos;
    private Vector2 currentVelocity;
    private Camera _mainCamera;
    private BoxCollider2D SelfCollider;
    private EdgeCollider2D TableCollider;
    private SafeZone safeZone;
    private Vector2 lastValidPos;

    void Start()
    {
        _mainCamera = Camera.main;
        SelfRB = GetComponent<Rigidbody2D>();
        SelfCollider = GetComponent<BoxCollider2D>();

        GameObject KebabDragArea = GameObject.FindWithTag("KebabDragArea");
        if (KebabDragArea == null) Debug.Log("Error. No Objects with a tag <KebabDragArea> . ");
        TableCollider = KebabDragArea.GetComponent<EdgeCollider2D>();

    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        float zDistanceToCamera = Mathf.Abs(_mainCamera.transform.position.z - transform.position.z);
        Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(new Vector3(eventData.position.x,eventData.position.y,zDistanceToCamera));
        dragOffset = SelfRB.position - (Vector2)worldPoint;
        isDragging = true;
        Physics2D.IgnoreCollision(SelfCollider, TableCollider, true);
        
        safeZone = CalculateDragAreaBounds();


    }

    public void OnDrag(PointerEventData eventData)
    {
        
        // --------- Position Calculations
        Vector2 currentWorldMousePos = _mainCamera.ScreenToWorldPoint(eventData.position);
        Vector2 currentWorldPos = transform.position;
        // --------- Being used in FixedTime Function
        targetPos = currentWorldMousePos + dragOffset; 
        // --------- To be used in OnEndDrag
        lastMousePos = currentWorldMousePos;
        // --------- Calculation for if the last object position is outside of the dragable bounds. Maybe for order delivery or trash purposes.
        bool isOutOfBounds = currentWorldPos.x < safeZone.MinX || currentWorldPos.x > safeZone.MaxX ||
                            currentWorldPos.y < safeZone.MinY || currentWorldPos.y > safeZone.MaxY;
        if (!isOutOfBounds)
        {
            // --------- Getting the last object world position to be used in OnEndDrag
            Vector2 clampedPos = new(
                Mathf.Clamp(currentWorldPos.x, safeZone.MinX, safeZone.MaxX),
                Mathf.Clamp(currentWorldPos.y, safeZone.MinY, safeZone.MaxY)
            );
            lastValidPos = clampedPos;
        }

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        Physics2D.IgnoreCollision(SelfCollider, TableCollider, false);

        Vector3 currentWorldPos = transform.position;
        bool isOutOfBounds = currentWorldPos.x < safeZone.MinX || currentWorldPos.x > safeZone.MaxX ||
                            currentWorldPos.y < safeZone.MinY || currentWorldPos.y > safeZone.MaxY;
        if (!isOutOfBounds)
        {
            Vector2 currentWorldMousePos = _mainCamera.ScreenToWorldPoint(eventData.position);
            currentVelocity = (currentWorldMousePos - lastMousePos) / Time.unscaledDeltaTime;
            SelfRB.linearVelocity = currentVelocity;
        }
        else
        {
            SelfRB.linearVelocity = Vector2.zero;
            transform.position = new Vector3(lastValidPos.x,lastValidPos.y,currentWorldPos.z);
        }
    }

    void FixedUpdate()
    {
        if (isDragging)
        {
            SelfRB.MovePosition(targetPos);
        }
    }

    private SafeZone CalculateDragAreaBounds()
    {
        Bounds DragAreaBounds = TableCollider.bounds;
        Vector2 objectExtents = SelfCollider.bounds.extents;
        
        return new SafeZone
        {
            MinX = DragAreaBounds.min.x + objectExtents.x,
            MaxX = DragAreaBounds.max.x - objectExtents.x,
            MinY = DragAreaBounds.min.y + objectExtents.y,
            MaxY = DragAreaBounds.max.y - objectExtents.y
        };
    }

    private class SafeZone
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }

}
