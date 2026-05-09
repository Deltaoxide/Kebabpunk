using UnityEngine;
using UnityEngine.InputSystem;

public class MainCharInteractor : MonoBehaviour
{
    public bool allowInteract = true;
    private IInteractable interactableInRange = null;

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && allowInteract)
        {
            interactableInRange?.Interact();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision);
        if (collision.TryGetComponent<IInteractable>(out IInteractable interactable) && interactable.CanInteract())
        {
            
            interactableInRange?.IsInInteractionRange(false);
            interactableInRange = interactable;
            interactableInRange.IsInInteractionRange(true);
        }
    }
    void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log("exit");
        if (collision.TryGetComponent<IInteractable>(out IInteractable interactable) && interactable == interactableInRange)
        {
            
            interactableInRange.IsInInteractionRange(false);
            interactableInRange = null;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
