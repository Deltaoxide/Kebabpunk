using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO: Expensive lookups in hot paths
    /// </summary>
    public class ExpensiveLookups : MonoBehaviour
    {
        void Update()
        {
            // BAD: Find in Update
            var player = GameObject.Find("Player");

            // BAD: FindWithTag in Update
            var enemy = GameObject.FindWithTag("Enemy");

            // BAD: FindObjectOfType in Update
            var manager = FindObjectOfType<Camera>();

            // BAD: Camera.main (calls FindWithTag internally)
            var cam = Camera.main;
        }
    }
}
