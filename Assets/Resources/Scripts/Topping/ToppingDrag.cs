
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToppingDrag : MonoBehaviour,IBeginDragHandler,IEndDragHandler,IDragHandler
{
    public GameObject ToppingIcon;
    public Sprite ToppingIconSprite;
    public Vector2 ToppingIconOffset;

    // --------- Private Variables -----------
    private Vector2 mouse_pos_start;
    private Transform ToppingIcon_Transform;
    private SpriteRenderer ToppingIcon_SpriteRenderer;
    private Camera _mainCamera;


// Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ToppingIcon_Transform = ToppingIcon.transform;
        ToppingIcon_SpriteRenderer = ToppingIcon.GetComponent<SpriteRenderer>();
        _mainCamera = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        mouse_pos_start = eventData.position;
        ToppingIcon.SetActive(true);
        ToppingIcon_SpriteRenderer.sprite = ToppingIconSprite;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(ToppingIcon.activeInHierarchy && mouse_pos_start != Vector2.zero)
        {
            Vector2 worldpos = _mainCamera.ScreenToWorldPoint(eventData.position);
            ToppingIcon_Transform.position = worldpos + ToppingIconOffset;
            
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        mouse_pos_start = Vector2.zero;
        ToppingIcon.SetActive(false);
    }

    

}
