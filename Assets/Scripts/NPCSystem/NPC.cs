using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NPC", menuName = "Scriptable Objects/NPCData")]
public class NPC : ScriptableObject
{
    public string unique_id;
    public Sprite sprite;
    public Sprite spriteSide;

    public NPCType type;
    public NPCJsonData nPCJsonData;
}
