using System;

namespace Pholus.Editor.Prompts.Attributes
{
    /// <summary>
    /// Marks a class as a prompt provider for automatic discovery by Pholus.
    /// Apply this to classes implementing IDetectionRule or IFixProvider.
    /// </summary>
    /// <example>
    /// [PromptProvider(Priority = 100)] // Override built-in rules
    /// public class MyCustomRule : IDetectionRule { ... }
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PromptProviderAttribute : Attribute
    {
        /// <summary>
        /// Priority for ordering and override. Higher priority wins when multiple
        /// providers exist for the same issue type.
        /// Default is 100 (higher than built-in rules which use 0).
        /// </summary>
        public int Priority { get; set; } = 100;
    }
}
