using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Events/Game Event (quest)")]
public class CustomEvent_quest : ScriptableObject
{
    private event Action<Quest> onEventRaised;

    public void Invoke(Quest value) { onEventRaised?.Invoke(value); }

    public void RegisterListener(Action<Quest> listener) => onEventRaised += listener;
    public void UnregisterListener(Action<Quest> listener) => onEventRaised -= listener;
}