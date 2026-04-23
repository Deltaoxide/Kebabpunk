using UnityEngine;

public class DestinationTrigger : MonoBehaviour
{
    [SerializeField] private Destination destination;
    private string destID;
    public CustomEvent_str onDestReached;
    void Awake()
    {
        destID = destination.DestID;
        if(destID == null)
        {
            Debug.LogError("Destination ID is Empty.");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            onDestReached.Invoke(destID);
        }
    }
}
