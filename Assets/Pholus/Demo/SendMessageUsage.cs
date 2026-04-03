using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO: SendMessage is slow (reflection-based)
    /// </summary>
    public class SendMessageUsage : MonoBehaviour
    {
        public GameObject target;

        void Start()
        {
            // BAD: SendMessage uses reflection
            SendMessage("OnDamage", 10);

            // BAD: SendMessage with options
            SendMessage("OnHeal", 5, SendMessageOptions.DontRequireReceiver);

            // BAD: BroadcastMessage is even slower
            BroadcastMessage("OnEvent");

            // BAD: SendMessageUpwards
            SendMessageUpwards("OnNotify");
        }

        void OnDamage(int amount) { }
        void OnHeal(int amount) { }
        void OnEvent() { }
        void OnNotify() { }
    }
}
