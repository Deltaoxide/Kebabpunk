using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

[CreateAssetMenu(fileName = "NPC", menuName = "Scriptable Objects/NPCData")]
public class NPCSO : ScriptableObject
{
    public string unique_id;
    public Sprite sprite;
    public Sprite spriteSide;
    public SpriteAsset npcMiniSpriteAsset;

    public NPCType type;
    public NPCJsonData nPCJsonData;
}
