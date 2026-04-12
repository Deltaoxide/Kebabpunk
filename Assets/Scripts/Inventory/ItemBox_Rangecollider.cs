using UnityEngine;

public class ItemBox_Rangecollider : MonoBehaviour
{
    [SerializeField] ItemBox itemBox;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            itemBox.Open();
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            itemBox.Close();
        }
    }
}
