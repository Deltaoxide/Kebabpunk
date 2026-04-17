using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InvItem : MonoBehaviour, IBeginDragHandler,IDragHandler,IEndDragHandler
{
    public InvItemSO invItemSo;
    public string CustomID;
    
    
    public GameObject CurrentSlot {get; private set;}
    //------------ Event 
    public event Action<InvItem> OnHoldItem;

    
    private Camera _mainCamera;
    private Image image;
    private Transform DraggingTransform;
    
    private Transform currentSlotTransform;
    private CanvasGroup DraggingGroup;
    private CanvasGroup ItemGroup;

    
    

    void Start()
    {
        _mainCamera = Camera.main;
        image = GetComponent<Image>();
        SetImage();
        
        DraggingTransform = GameObject.FindGameObjectWithTag("DraggingInvItem").transform;
        if (DraggingTransform == null)
        {
            Debug.LogError("DraggingInvItem Cannot be found");
        }
        currentSlotTransform = transform.parent;
        
        
        
        CurrentSlot = currentSlotTransform.gameObject;
        DraggingGroup = DraggingTransform.GetComponent<CanvasGroup>();
        ItemGroup = GetComponent<CanvasGroup>();
        

    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Item is begining drag.");
        SetHold(true);   
        OnHoldItem?.Invoke(this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SetHold(false);
    }
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 mouse_worldpos = _mainCamera.ScreenToWorldPoint(eventData.position);
        DraggingTransform.position = mouse_worldpos;
    }
    public void AssignSlot(Transform newSlot)
    {
        currentSlotTransform = newSlot;
        CurrentSlot = currentSlotTransform.gameObject;
    }
    private void SetImage()
    {
        image.sprite = invItemSo.sprite;
    } 
    
    private void SetHold(bool setHold)
    {
        if(setHold)
        {
            
            transform.SetParent(DraggingTransform);
            transform.localPosition = Vector2.zero;
            DraggingGroup.alpha = 1f;
            DraggingGroup.blocksRaycasts = true;
            ItemGroup.blocksRaycasts = false;
        }
        else
        {
            transform.SetParent(currentSlotTransform);
            transform.localPosition = Vector2.zero;
            DraggingGroup.alpha = 0f;
            DraggingGroup.blocksRaycasts = false;
            ItemGroup.blocksRaycasts = true;
        }
    }

}
