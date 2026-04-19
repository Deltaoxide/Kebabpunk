using UnityEngine;

public class DialogBoxHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private DialogBoxWrapper dialogBoxWrapper;
    public void DeactivateDialogBox()
    {
        gameObject.SetActive(false);
        dialogBoxWrapper.QuitCurrentNPC();
    }
    public void StartDialogue()
    {
        dialogBoxWrapper.ContinueStory();
    }
}
