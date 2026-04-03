using System.Collections;
using TMPro;
using UnityEngine;
using Ink.Runtime;
using UnityEngine.InputSystem;

public class DialogBoxWrapper : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header ("Gameobjects")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private NPCDataManager _NPCDataManager;
    [SerializeField] private NPCOrderManager _NPCOrderManager;
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Header ("Setting")]
    [SerializeField] private float delayBetweenChars = 0.05f;
    
    // ------------ private Gameobjects
    private Animator dialogueAnimator;

    // ------------ variables
    private bool typewriterStillWriting;
    private float delayWithModifier;
    
    
    private bool _waitingForOrder = false;
    public bool WaitingForOrder
    {
        get {
            return _waitingForOrder && !typewriterStillWriting;
        }
        private set
        {
            _waitingForOrder = value;
        }
    }
    private Story story;
    public string Dialogue_id {get; private set;}
    void Start()
    {
        dialogueBox.SetActive(false);
        dialogueAnimator = dialogueBox.GetComponent<Animator>();
        typewriterStillWriting = false;
    } 
    
    public void EnterDialogueMode(string npc_id)
    {
        Dialogue_id = npc_id;
        dialogueManager.EnterDialogueMode(Dialogue_id);
        story = dialogueManager.InkStory;
        BindInkFunctions();
        
        dialogueBox.SetActive(true);
        dialogueText.text = "";
        dialogueAnimator.ResetTrigger("Pop");
        dialogueAnimator.SetTrigger("Pop");
    }

    public void ContinueStory(InputAction.CallbackContext ctx = default)
    {
        if (ctx.action != null && !ctx.performed)
        {
            return; // If action is triggered by input system AND ctx state is not performed: Return.
        }
        if (typewriterStillWriting)
        {
            delayWithModifier = delayBetweenChars / 4;
            return; 
        }
        if (WaitingForOrder)
        {
            return;
        }
        dialogueText.text = "";

        dialogueManager.ContinueStory();
        if (dialogueManager.dialogueIsPlaying)
        {
            StartCoroutine(TypeTextToDialogueBox(dialogueManager.dialogueOutput));
        }
        else
        {
            QuitDialogueMode();
        }
        
    }

    IEnumerator TypeTextToDialogueBox(string text)
    {
        delayWithModifier = delayBetweenChars;
        typewriterStillWriting = true;
        string _TextToWrite = text;
        foreach (char _char in _TextToWrite)
        {
            dialogueText.text += _char;
            yield return new WaitForSeconds(delayWithModifier);
        }
        
        typewriterStillWriting = false;
        delayWithModifier = delayBetweenChars;
    }

    private void QuitDialogueMode()
    {
        dialogueAnimator.ResetTrigger("Close");
        dialogueAnimator.SetTrigger("Close");
        UnbindInkFunctions();
        
    }
    public void QuitCurrentNPC()
    {

        _NPCDataManager.QuitCurrentNPC();
        Dialogue_id = null;

    }

    private void BindInkFunctions()
    {
        story = dialogueManager.InkStory;
        story.BindExternalFunction("normalOrder", (InkList order) =>
        {
            CreateOrder(order);
        });
        story.BindExternalFunction("excludedOrder", (InkList order) =>
        {
            CreateOrder(order,excludedMode:true);
        });
        story.BindExternalFunction("waitForOrder", () =>
        {
            Debug_wait_for_order();
        });
    }
    private void UnbindInkFunctions()
    {
        story.UnbindExternalFunction("normalOrder");
        story.UnbindExternalFunction("excludedOrder");
        story.UnbindExternalFunction("waitForOrder");
    }

    private void CreateOrder(InkList order, bool excludedMode=false)
    {
        if(excludedMode)
        {
            _NPCOrderManager.CreateExcludedOrder(order);
        }
        else
        {
            _NPCOrderManager.CreateOrder(order);
        }
    }
    public void DeliverOrder(int order_success_value)
    {
        // Order Success values are ->
        // 1 -> Good delivery
        // 2 -> Bad delivery
        story.variablesState["order_state"] = order_success_value;
        WaitingForOrder = false;
        story.ChoosePathString($"{Dialogue_id}.order_check");
        ContinueStory();
    }
    private void Debug_wait_for_order()
    {
        WaitingForOrder = true;
    }
   
}
