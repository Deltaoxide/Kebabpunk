using UnityEngine;

public class InvItemSlot : MonoBehaviour
{
    public InvItem currentItem;
    

    void Start()
    {
        currentItem = gameObject.GetComponentInChildren<InvItem>();
        if (currentItem != null)
        {
            
        }
    }

    void SnapInvItem()
    {
        currentItem.transform.position = Vector2.zero;
        
    }
    void InvItemOnHold()
    {
    }
}
