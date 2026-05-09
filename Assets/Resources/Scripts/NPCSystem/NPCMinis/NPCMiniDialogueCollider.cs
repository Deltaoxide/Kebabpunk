using System;
using UnityEngine;

public class NPCMiniDialogueCollider : MonoBehaviour
{
    [SerializeField] private Destination destination;
    public bool playerInRange;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }
    void OnTriggerExit2D(Collider2D collision)
    {
       if (collision.CompareTag("Player"))
        {
            playerInRange = false;
        } 
    }
}
