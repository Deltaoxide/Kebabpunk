using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Events/Game Event (string)")]
public class CustomEvent_str : ScriptableObject
{
    private event Action<string> onEventRaised;

    public void Invoke(string value) { onEventRaised?.Invoke(value); }

    public void RegisterListener(Action<string> listener) => onEventRaised += listener;
    public void UnregisterListener(Action<string> listener) => onEventRaised -= listener;
}