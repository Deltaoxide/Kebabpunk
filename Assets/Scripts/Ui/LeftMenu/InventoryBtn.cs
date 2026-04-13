using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryBtn : MonoBehaviour, IPointerEnterHandler, IDragHandler, IPointerExitHandler
{
    [SerializeField] Animator animator;
    public void OnDrag(PointerEventData eventData)
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        animator.SetBool("isHovering",true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        animator.SetBool("isHovering",false);
    }

    
}
