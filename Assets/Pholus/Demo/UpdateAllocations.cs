using System.Collections.Generic;
using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO: Allocations in Update loop
    /// </summary>
    public class UpdateAllocations : MonoBehaviour
    {
        void Update()
        {
            // BAD: new List every frame
            var enemies = new List<GameObject>();

            // BAD: new Dictionary every frame
            var scores = new Dictionary<string, int>();

            // BAD: String concatenation
            string msg = "Frame: " + Time.frameCount + " Time: " + Time.time;

            // BAD: Boxing allocation
            object boxed = Time.deltaTime;
        }
    }
}
