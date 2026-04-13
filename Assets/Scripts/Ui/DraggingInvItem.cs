using UnityEngine;
using UnityEngine.UI;

public class DraggingInvItem : MonoBehaviour
{
    [SerializeField] Image imageComp;

    void Awake()
    {
        imageComp.GetComponent<Image>();
    }
    public void Enable()
    {
        
    }
    public void Disable()
    {
        imageComp.sprite = null;
    }
}
