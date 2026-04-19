using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,IPointerClickHandler
{
    [SerializeField] Animator animator;
    [SerializeField] InventoryView inventoryView;


    public CustomEvent onInventoryOpen;
 
    public void OnPointerEnter(PointerEventData eventData)
    {
        animator.SetBool("isHovering",true);
        var dragobj = eventData.pointerDrag;
        var invItem = dragobj != null ? dragobj.GetComponent<InvItem>() : null;
        if(invItem != null)
        {
            onInventoryOpen.Invoke();
        }
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        animator.SetBool("isHovering",false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
            onInventoryOpen.Invoke();
    }
}
