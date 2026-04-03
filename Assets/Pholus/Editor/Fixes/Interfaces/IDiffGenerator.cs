using Pholus.Editor.Fixes.Models;

namespace Pholus.Editor.Fixes.Interfaces
{
    /// <summary>
    /// Interface for generating diffs between code versions.
    /// Single Responsibility: Only handles diff generation.
    /// </summary>
    public interface IDiffGenerator
    {
        /// <summary>
        /// Generates a diff between original and modified content.
        /// </summary>
        /// <param name="original">Original content.</param>
        /// <param name="modified">Modified content.</param>
        /// <returns>Diff result with line-by-line changes.</returns>
        DiffResult GenerateDiff(string original, string modified);

        /// <summary>
        /// Formats a diff for console/text display.
        /// </summary>
        /// <param name="diff">The diff result.</param>
        /// <returns>Formatted text representation.</returns>
        string FormatDiffForDisplay(DiffResult diff);
    }
}
