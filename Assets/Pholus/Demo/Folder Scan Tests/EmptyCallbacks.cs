using UnityEngine;

namespace Pholus.Demo.Folder_Scan_Tests
{
    /// <summary>
    /// DEMO: Empty Unity callbacks waste CPU
    /// </summary>
    public class EmptyCallbacks : MonoBehaviour
    {
        // BAD: Empty Update still gets called every frame
        void Update() { }

        // BAD: Empty FixedUpdate still gets called
        void FixedUpdate() { }

        // BAD: Empty LateUpdate still gets called
        void LateUpdate() { }

        // BAD: Empty callbacks
        void OnEnable() { }
        void OnDisable() { }
        void OnBecameVisible() { }
        void OnBecameInvisible() { }
    }
}
