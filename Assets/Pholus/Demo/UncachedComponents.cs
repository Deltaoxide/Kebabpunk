using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO: Uncached GetComponent calls
    /// </summary>
    public class UncachedComponents : MonoBehaviour
    {
        void Update()
        {
            // BAD: GetComponent in Update
            var rb = GetComponent<Rigidbody>();
            rb?.AddForce(Vector3.up);

            // BAD: Another GetComponent in Update
            var col = GetComponent<Collider>();

            // BAD: GetComponentInChildren in Update
            var renderer = GetComponentInChildren<Renderer>();
        }

        void FixedUpdate()
        {
            // BAD: GetComponent in FixedUpdate
            var rb = GetComponent<Rigidbody>();
            rb?.MovePosition(transform.position + Vector3.forward);
        }
    }
}
