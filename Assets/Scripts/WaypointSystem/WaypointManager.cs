using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WaypointManager : MonoBehaviour {
    public static WaypointManager Instance;
    private List<Waypoint> allWaypoints = new List<Waypoint>();

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Debug.LogError("Tried to create second instance.");
            return;
        }
        allWaypoints = FindObjectsByType<Waypoint>(FindObjectsSortMode.None).ToList();
    }

    public Vector3 GetRandomWaypointForNPC(NPCType npcType) {
        // Filter the list based on the NPC's type defined in their ScriptableObject
        var validWaypoints = allWaypoints
            .Where(w => w.CanBeVisitedBy(npcType))
            .ToList();

        if (validWaypoints.Count > 0) {
            int randomIndex = Random.Range(0, validWaypoints.Count);
            return validWaypoints[randomIndex].GetPosition();
        }

        // Fallback to NPC's current position if no waypoint is found
        return Vector2.zero; 
    }
}