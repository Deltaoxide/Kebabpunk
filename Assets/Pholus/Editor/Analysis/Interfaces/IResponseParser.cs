using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Analysis.Interfaces
{
    /// <summary>
    /// Interface for parsing AI responses.
    /// Single Responsibility: Only handles response parsing.
    /// </summary>
    public interface IResponseParser
    {
        /// <summary>
        /// Parses an AI response into an AnalysisResult.
        /// </summary>
        /// <param name="jsonResponse">The JSON response from the AI.</param>
        /// <returns>Parsed analysis result.</returns>
        /// <exception cref="System.Exception">Thrown if parsing fails.</exception>
        AnalysisResult Parse(string jsonResponse);

        /// <summary>
        /// Attempts to parse an AI response without throwing.
        /// </summary>
        /// <param name="jsonResponse">The JSON response from the AI.</param>
        /// <param name="result">The parsed result if successful.</param>
        /// <param name="error">Error message if parsing failed.</param>
        /// <returns>True if parsing succeeded.</returns>
        bool TryParse(string jsonResponse, out AnalysisResult result, out string error);

        /// <summary>
        /// Extracts fixed code from an AI fix response.
        /// </summary>
        /// <param name="response">The AI response containing fixed code.</param>
        /// <returns>The extracted code, or the original response if no code block found.</returns>
        string ExtractCode(string response);
    }
}
