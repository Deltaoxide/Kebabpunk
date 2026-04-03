using System;

namespace Pholus.Editor.Analysis.Models
{
    /// <summary>
    /// Response from a CLI provider command execution.
    /// </summary>
    [Serializable]
    public class CLIResponse
    {
        /// <summary>
        /// Whether the command executed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Standard output from the command.
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Standard error from the command, if any.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// How long the command took to execute.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Exit code from the process.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Token usage and cost information (if available).
        /// </summary>
        public TokenUsage Usage { get; set; }

        public static CLIResponse Successful(string output, TimeSpan duration, TokenUsage usage = null)
        {
            return new CLIResponse
            {
                Success = true,
                Output = output,
                Error = null,
                Duration = duration,
                ExitCode = 0,
                Usage = usage
            };
        }

        public static CLIResponse Failed(string error, int exitCode, TimeSpan duration)
        {
            return new CLIResponse
            {
                Success = false,
                Output = null,
                Error = error,
                Duration = duration,
                ExitCode = exitCode
            };
        }
    }
}
