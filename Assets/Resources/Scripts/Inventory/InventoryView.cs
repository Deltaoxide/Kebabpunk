using System.Collections;
using UnityEngine;

public class InventoryView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Animator inventoryButtonAnimator;
    [SerializeField] private GameObject window;
    public CustomEvent onInventoryOpen;
    public CustomEvent onInventoryClose;

    public bool isOpen;
    void OnEnable()
    {
        onInventoryOpen.RegisterListener(OpenWindow);
        onInventoryClose.RegisterListener(CloseWindow);
    }
    void OnDisable()
    {
        onInventoryOpen.UnregisterListener(OpenWindow);
        onInventoryClose.UnregisterListener(CloseWindow);
    }
    public void OpenWindow()
    {
        window.SetActive(true);
        animator.SetBool("isOpen",true);
        inventoryButtonAnimator.SetBool("isViewOpen",true);
        isOpen = true;
    }
    public void CloseWindow()
    {
        animator.SetBool("isOpen",false);
        inventoryButtonAnimator.SetBool("isViewOpen",false);
        isOpen = false;
        StartCoroutine(Disable());
    }
    public IEnumerator Disable()
    {
        yield return new WaitForSeconds(0.5f);
        window.SetActive(false);
    }
}
