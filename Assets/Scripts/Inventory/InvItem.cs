using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InvItem : MonoBehaviour, IPointerDownHandler,IPointerUpHandler
{
    public InvItemSO invItemSo;
    public string CustomID;
    
    private Image image;
    
    public event Action<InvItem> OnHoldItem;

    void Start()
    {
        image = GetComponent<Image>();
        SetImage();

    }
    public void OnPointerDown(PointerEventData eventData)
    {
        SetHold(true);   
        OnHoldItem?.Invoke(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetHold(false);
    }
    private void SetImage()
    {
        image.sprite = invItemSo.sprite;
    } 
    
    private void SetHold(bool setHold)
    {
        if(setHold)
        {
            image.color = new Color(1f,1f,1f,0f);
        }
        else
        {
            image.color = new Color(1f,1f,1f,1f);
        }
    }

}
