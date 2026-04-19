using UnityEngine;

public class ItemBox : MonoBehaviour
{
    
    [Header("GameObject References")]
    [SerializeField] GameObject Popup;
    [Header("Assets")]
    [SerializeField] Sprite spriteOpen;
    [SerializeField] Sprite spriteClose;

    private SpriteRenderer ItemBoxSR;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ItemBoxSR = GetComponent<SpriteRenderer>();
    }


    public void Open()
    {
        ItemBoxSR.sprite =  spriteOpen;
        Popup.SetActive(true);

    }
    public void Close()
    {
        ItemBoxSR.sprite =  spriteClose;
        Popup.SetActive(false);
    }
}
