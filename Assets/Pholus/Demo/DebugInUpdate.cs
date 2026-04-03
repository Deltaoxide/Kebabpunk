using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO: Debug.Log in Update is expensive in builds
    /// </summary>
    public class DebugInUpdate : MonoBehaviour
    {
        private float _health = 100f;

        void Update()
        {
            // BAD: Debug.Log runs every frame in builds
            Debug.Log("Health: " + _health);

            if (_health < 20f)
            {
                // BAD: LogWarning in hot path
                Debug.LogWarning("Low health!");
            }
        }

        void FixedUpdate()
        {
            // BAD: Debug in FixedUpdate
            Debug.Log("Physics tick");
        }
    }
}
