using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pholus.Demo
{
    /// <summary>
    /// DEMO: LINQ in hot paths
    /// </summary>
    public class LinqInUpdate : MonoBehaviour
    {
        private List<int> _numbers = new List<int> { 1, 2, 3, 4, 5 };
        private List<string> _names = new List<string> { "Alice", "Bob", "Charlie" };

        void Update()
        {
            // BAD: Where + ToList allocates
            var evens = _numbers.Where(n => n % 2 == 0).ToList();

            // BAD: OrderBy allocates
            var sorted = _names.OrderBy(n => n.Length).ToList();

            // BAD: FirstOrDefault with predicate
            var first = _names.FirstOrDefault(n => n.StartsWith("A"));

            // BAD: Any with predicate in Update
            bool hasLong = _names.Any(n => n.Length > 5);
        }
    }
}
