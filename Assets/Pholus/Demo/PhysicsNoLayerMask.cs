using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO: Physics queries without layer masks
    /// </summary>
    public class PhysicsNoLayerMask : MonoBehaviour
    {
        void Update()
        {
            // BAD: Raycast without layer mask checks all layers
            if (Physics.Raycast(transform.position, transform.forward, 100f))
            {
                Debug.Log("Hit something");
            }

            // BAD: RaycastAll without layer mask
            var hits = Physics.RaycastAll(transform.position, transform.forward);

            // BAD: SphereCast without layer mask
            RaycastHit hit;
            Physics.SphereCast(transform.position, 1f, transform.forward, out hit, 50f);

            // BAD: OverlapSphere without layer mask
            var colliders = Physics.OverlapSphere(transform.position, 10f);
        }
    }
}
