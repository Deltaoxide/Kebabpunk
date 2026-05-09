using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Events/Game Event")]
public class CustomEvent : ScriptableObject
{
    private event Action onEventRaised;

    public void Invoke() => onEventRaised?.Invoke();

    public void RegisterListener(Action listener) => onEventRaised += listener;
    public void UnregisterListener(Action listener) => onEventRaised -= listener;
}