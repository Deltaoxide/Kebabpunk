using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [Header("Settings")]
    public List<NPCType> allowedNPCTypes = new List<NPCType>{ NPCType.All };
    public List<WaypointGroup> Group = new List<WaypointGroup>();

    public Vector3 GetPosition() {
        return transform.position;
    }

    public bool CanBeVisitedBy(NPCType type) {
        // Returns true if the list contains 'All' or the specific NPC type
        return allowedNPCTypes.Contains(NPCType.All) || allowedNPCTypes.Contains(type);
    }
}
