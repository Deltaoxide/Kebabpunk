using System.Collections;
using UnityEngine;

namespace Pholus.Demo.Folder_Scan_Tests
{
    /// <summary>
    /// DEMO: Coroutine allocation issues
    /// </summary>
    public class BadCoroutines : MonoBehaviour
    {
        void Start()
        {
            StartCoroutine(SpawnLoop());
        }

        IEnumerator SpawnLoop()
        {
            while (true)
            {
                // BAD: new WaitForSeconds allocates every iteration
                yield return new WaitForSeconds(1f);

                // Do something
                Debug.Log("Spawned");
            }
        }

        IEnumerator MultipleWaits()
        {
            
            // BAD: Each new WaitForSeconds is an allocation
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(1.0f);
            yield return new WaitForSeconds(2.0f);
        }
    }
}
