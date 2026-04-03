using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Fixes.Models;

namespace Pholus.Editor.Fixes.Interfaces
{
    /// <summary>
    /// Result of a file modification validation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; }

        public static ValidationResult Valid() => new ValidationResult { IsValid = true };
        public static ValidationResult Invalid(string error) => new ValidationResult { IsValid = false, Error = error };
    }

    /// <summary>
    /// Interface for modifying script files.
    /// Single Responsibility: Only handles file writing.
    /// </summary>
    public interface IFileModifier
    {
        /// <summary>
        /// Applies fixed content to a file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="fixedContent">New content to write.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the modification.</returns>
        Task<FixResult> ApplyFixAsync(
            string filePath,
            string fixedContent,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates content before writing.
        /// </summary>
        /// <param name="content">Content to validate.</param>
        /// <returns>Validation result.</returns>
        ValidationResult ValidateContent(string content);

        /// <summary>
        /// Checks if a file can be modified.
        /// </summary>
        /// <param name="filePath">Path to check.</param>
        /// <returns>True if the file can be modified.</returns>
        bool CanModify(string filePath);
    }
}
