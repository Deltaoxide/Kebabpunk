using Ink.Runtime;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private TextAsset inkJson;

    
    private static DialogueManager Instance;


    public string dialogueOutput;
    public bool dialogueIsPlaying {get; private set;}
    public bool Locked;
    public Story InkStory {get; private set;}
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Found more than one DialogueManager in the scene.");
        }
        Instance = this;
    }

    public static DialogueManager GetInstance()
    {
        return Instance;
    }

    private void Start()
    {
        dialogueIsPlaying = false;
    }

    public void EnterDialogueMode(string knotID)
    {
        InkStory = new Story(inkJson.text);
        dialogueIsPlaying = true;
        dialogueOutput = "";
        InkStory.ChoosePathString(knotID);
        
    }

    public void ContinueStory()
    {
        if (!dialogueIsPlaying)
        {
            Debug.LogWarning("ContinueStory triggered without a dialogue is playing.");
        }
        if (!Locked)
        {
            if (InkStory.canContinue)
            {
                dialogueOutput = InkStory.Continue();
            }
            else
            {
                dialogueOutput = "";
                dialogueIsPlaying = false;
                InkStory = null;
            }
        }
    }

}
