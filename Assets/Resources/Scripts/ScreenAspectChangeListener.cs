using System;
using UnityEngine;

public class ScreenAspectChangeListener : MonoBehaviour
{
    private float lastAspect;
    private Camera cam;
    public event Action<float, float> OnScreenSizeChanged;

    void Start()
    {
        cam = Camera.main;
        lastAspect = cam.aspect;
        OnScreenSizeChangedFunction();
    }

    void Update()
    {
        if (!Mathf.Approximately(cam.aspect, lastAspect))
        {
            lastAspect = cam.aspect;
            OnScreenSizeChangedFunction();
        }
    }

    void OnScreenSizeChangedFunction()
    {
        
        float height = cam.orthographicSize * 2;
        float width = height * cam.aspect;
        OnScreenSizeChanged?.Invoke(width,height);
        
    }
}