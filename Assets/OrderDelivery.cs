using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OrderDelivery : MonoBehaviour,IDropHandler
{
    [SerializeField] private NPCOrderManager nPCOrderManager;
    [SerializeField] private DialogBoxWrapper dialogBoxWrapper;
    public void OnDrop(PointerEventData eventData)
    {
        GameObject _object = eventData.pointerDrag;
        if (_object.TryGetComponent(out KebabToppingManager _kebabToppingManager))
        {
            Dictionary<ToppingType, int> _KebabData = _kebabToppingManager.Data;

            if (dialogBoxWrapper.WaitingForOrder)
            {
                nPCOrderManager.DeliverOrder(_KebabData);
                Destroy(_object);
            }
            ;
        }
        
    }
}
