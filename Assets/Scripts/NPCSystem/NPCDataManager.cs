using UnityEngine;

public class NPCDataManager : MonoBehaviour
{
    // ----------- NPC DATAS ------------
    public string unique_id;
    public Sprite normal_sprite;
    public Sprite sprite_Side;
    public NPCJsonData jsonData;

    // ----------- Settings ------------
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogBoxWrapper dialogBoxWrapper;

    // ----------- Private Variables ------------
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }
    public void SetNpcData(NPC _NPCData)
    {
        unique_id = _NPCData.unique_id;
        normal_sprite = _NPCData.sprite;
        sprite_Side = _NPCData.spriteSide;
        jsonData = _NPCData.nPCJsonData;
    } 

    public void TriggerDialogue()
    {
        if (dialogueManager.dialogueIsPlaying)
        {
            Debug.LogWarning("Tried to trigger dialogue while dialogue is playing.");
            return;
        }
        dialogBoxWrapper.EnterDialogueMode(unique_id);
    }

    public void QuitCurrentNPC()
    {
        //Call when interaction is complete.
        if (dialogueManager.dialogueIsPlaying)
        {
            Debug.LogWarning("Tried to quit NPC while dialog is still playing.");
            return;
        }
        animator.ResetTrigger("ExitTrigger");
        animator.SetTrigger("ExitTrigger");
    }
    public void SendNPC()
    {
        animator.ResetTrigger("EnterTrigger");
        animator.SetTrigger("EnterTrigger");
    }
}
