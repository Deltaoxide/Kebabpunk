using UnityEngine;

[CreateAssetMenu(fileName = "InvItem", menuName = "Scriptable Objects/InvItem")]
public class InvItemSO : ScriptableObject
{
    public string id;
    public Sprite sprite; 
    private void OnValidate()
    {
        #if UNITY_EDITOR
        if (id != name)
        {
            id = name;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        #endif
    }
}
