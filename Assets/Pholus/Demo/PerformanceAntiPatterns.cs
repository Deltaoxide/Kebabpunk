using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO SCRIPT - Intentionally contains performance anti-patterns for testing Pholus.
    /// DO NOT use this code in production!
    /// </summary>
    public class PerformanceAntiPatterns : MonoBehaviour
    {
        public GameObject prefab;
        public string targetTag = "Enemy";
        public int spawnCount = 10;

        private List<string> _names = new List<string>();
        private Rigidbody _rigidbody;
        private GameObject _player; // Cached player reference (alternatively use [SerializeField] to assign in Inspector)

        // =====================================================
        // CRITICAL ISSUES
        // =====================================================

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _player = GameObject.Find("Player");
        }

        void Update()
        {
            // CRITICAL: GetComponent in Update - should be cached in Awake/Start
            if (_rigidbody != null)
            {
                _rigidbody.AddForce(Vector3.up);
            }

            // Using cached player reference
            if (_player != null)
            {
                transform.LookAt(_player.transform);
            }

            // CRITICAL: FindObjectOfType in Update - searches entire scene
            var gameManager = FindObjectOfType<PerformanceAntiPatterns>();

            // CRITICAL: String concatenation in Update - allocates garbage
            string status = "Player: " + _player?.name + " at " + Time.time + " seconds";
            Debug.Log(status);

            // CRITICAL: new List in Update - allocates every frame
            var tempList = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                tempList.Add(i);
            }

            // CRITICAL: LINQ in Update - hidden allocations
            var filtered = _names.Where(n => n.StartsWith("A")).ToList();
            var ordered = _names.OrderBy(n => n.Length).FirstOrDefault();

            // CRITICAL: Instantiate in loop without object pooling
            if (Input.GetKeyDown(KeyCode.Space))
            {
                for (int i = 0; i < spawnCount; i++)
                {
                    Instantiate(prefab, Random.insideUnitSphere * 10, Quaternion.identity);
                }
            }
        }

        void FixedUpdate()
        {
            // CRITICAL: GetComponent in FixedUpdate
            var collider = GetComponent<BoxCollider>();

            // CRITICAL: FindWithTag in FixedUpdate
            var enemy = GameObject.FindWithTag(targetTag);
        }

        void LateUpdate()
        {
            // CRITICAL: Camera.main in LateUpdate - not cached, calls FindWithTag internally
            var cam = Camera.main;
            if (cam != null)
            {
                transform.LookAt(cam.transform);
            }

            // CRITICAL: new Dictionary in hot path
            var lookup = new Dictionary<string, int>();
        }

        // =====================================================
        // HIGH PRIORITY ISSUES
        // =====================================================

        void Start()
        {
            // HIGH: Resources.Load at runtime - should preload or use Addressables
            var material = Resources.Load<Material>("Materials/Default");

            // HIGH: SendMessage - slow reflection-based messaging
            SendMessage("OnCustomEvent", SendMessageOptions.DontRequireReceiver);

            // HIGH: BroadcastMessage - even slower, traverses hierarchy
            BroadcastMessage("OnBroadcastEvent", SendMessageOptions.DontRequireReceiver);

            StartCoroutine(SpawnRoutine());
            StartCoroutine(BadWaitRoutine());
        }

        IEnumerator SpawnRoutine()
        {
            while (true)
            {
                // HIGH: yield return new WaitForSeconds - allocates every iteration
                yield return new WaitForSeconds(1f);

                // Spawn something
                if (prefab != null)
                {
                    Instantiate(prefab, transform.position, Quaternion.identity);
                }
            }
        }

        IEnumerator BadWaitRoutine()
        {
            // HIGH: Multiple new WaitForSeconds allocations
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(1.0f);
            yield return new WaitForSeconds(1.5f);
            yield return new WaitForSeconds(2.0f);
        }

        void PerformPhysicsCheck()
        {
            // HIGH: Physics query without layer mask - checks all layers
            var hits = Physics.RaycastAll(transform.position, transform.forward, 100f);

            // HIGH: SphereCast without layer mask
            RaycastHit hit;
            Physics.SphereCast(transform.position, 1f, transform.forward, out hit, 50f);

            // HIGH: OverlapSphere without layer mask
            var colliders = Physics.OverlapSphere(transform.position, 10f);
        }

        // =====================================================
        // MEDIUM PRIORITY ISSUES
        // =====================================================

        // MEDIUM: Empty Unity callback methods - still get called, waste CPU
        void OnEnable() { }
        void OnDisable() { }
        void OnBecameVisible() { }
        void OnBecameInvisible() { }

        void ProcessItems()
        {
            // Get a fresh list every call (simulating external data)
            var items = GetExternalItems();

            // MEDIUM: foreach on non-cached collection in frequently called method
            foreach (var item in items)
            {
                ProcessItem(item);
            }

            // MEDIUM: Multiple GetComponent calls for same type
            var renderer1 = GetComponent<Renderer>();
            var renderer2 = GetComponent<Renderer>();
            var renderer3 = GetComponent<Renderer>();
        }

        void OnTriggerEnter(Collider other)
        {
            // MEDIUM: Debug.Log in production path - should use conditional compilation
            Debug.Log("Trigger entered by: " + other.gameObject.name);

            // MEDIUM: GetComponent on other object in callback
            var health = other.GetComponent<PerformanceAntiPatterns>();
        }

        void OnCollisionEnter(Collision collision)
        {
            // MEDIUM: String concatenation in collision callback
            Debug.Log("Collision with " + collision.gameObject.name + " at " + collision.contacts[0].point);

            // MEDIUM: Tag comparison using string
            if (collision.gameObject.tag == "Enemy")
            {
                // Do something
            }
        }

        // =====================================================
        // HELPER METHODS (not issues themselves)
        // =====================================================

        private List<string> GetExternalItems()
        {
            return new List<string> { "Item1", "Item2", "Item3" };
        }

        private void ProcessItem(string item)
        {
            // Processing logic
        }

        void OnCustomEvent() { }
        void OnBroadcastEvent() { }
    }
}