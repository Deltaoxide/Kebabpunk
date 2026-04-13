using UnityEngine;

public class InvItemSlot : MonoBehaviour
{
    public InvItem currentItem;

    void Start()
    {
        currentItem = gameObject.GetComponentInChildren<InvItem>();
    }

    void SnapInvItem()
    {
        currentItem.transform.position = Vector2.zero;
        
    }
}
