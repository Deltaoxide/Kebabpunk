using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO: Tag comparison using == allocates
    /// </summary>
    public class TagComparison : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            // BAD: .tag == allocates a string
            if (other.gameObject.tag == "Player")
            {
                Debug.Log("Player entered");
            }

            // BAD: .tag != also allocates
            if (other.tag != "Enemy")
            {
                return;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            // BAD: Same issue in collision
            if (collision.gameObject.tag == "Obstacle")
            {
                Destroy(gameObject);
            }
        }
    }
}
