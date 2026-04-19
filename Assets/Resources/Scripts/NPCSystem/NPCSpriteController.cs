using System.ComponentModel;
using UnityEngine;

[RequireComponent(typeof(NPCDataManager))]
public class NPCSpriteController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private NPCDataManager npcDataManager;
    private SpriteRenderer spriteRenderer;
    void Start()
    {
        npcDataManager = GetComponent<NPCDataManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void SetSideSprite()
    {
        spriteRenderer.sprite = npcDataManager.sprite_Side;
    }
    public void SetNormalSprite()
    {
        spriteRenderer.sprite = npcDataManager.normal_sprite;
        Debug.Log("setting normal sprite");
    }

}
