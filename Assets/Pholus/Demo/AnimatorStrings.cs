using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO: Animator methods with string parameters
    /// </summary>
    public class AnimatorStrings : MonoBehaviour
    {
        private Animator _animator;

        void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        void Update()
        {
            // BAD: String parameter hashes every frame
            _animator.SetFloat("Speed", 1.5f);
            _animator.SetBool("IsGrounded", true);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                // BAD: SetTrigger with string
                _animator.SetTrigger("Jump");
            }
        }

        public void TakeDamage()
        {
            // BAD: String in frequently called method
            _animator.SetTrigger("Hit");
            _animator.SetInteger("Health", 50);
        }
    }
}
