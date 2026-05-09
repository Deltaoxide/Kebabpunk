using UnityEngine;

public interface IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    void IsInInteractionRange(bool is_in_range);
    void Interact();
    bool CanInteract();
}
