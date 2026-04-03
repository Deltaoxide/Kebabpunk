using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Core
{
    /// <summary>
    /// Static configuration of available models per provider.
    /// Contains hardcoded fallback defaults since CLIs don't expose model lists programmatically.
    /// </summary>
    public static class ModelConfiguration
    {
        /// <summary>
        /// Fallback models for Claude CLI if discovery fails.
        /// </summary>
        public static readonly string[] DefaultClaudeModels =
        {
            "sonnet",
            "opus",
            "haiku",
            "opusplan",
            "sonnet1m"
        };

        /// <summary>
        /// Fallback models for OpenAI Codex CLI if discovery fails.
        /// </summary>
        public static readonly string[] DefaultCodexModels =
        {
            "gpt-5-codex",
            "gpt-5.1-codex",
            "o3",
            "gpt-4.1",
            "gpt-4.1-mini"
        };

        /// <summary>
        /// Fallback models for Google Gemini CLI if discovery fails.
        /// </summary>
        public static readonly string[] DefaultGeminiModels =
        {
            "gemini-2.0-flash",
            "gemini-2.5-pro",
            "gemini-1.5-pro",
            "gemini-1.5-flash",
            "gemini-pro"
        };

        /// <summary>
        /// Fallback models for Cursor CLI if discovery fails.
        /// </summary>
        public static readonly string[] DefaultCursorModels =
        {
            "auto",
            "sonnet-4.5",
            "sonnet-4.5-thinking",
            "opus-4.5",
            "gpt-5.2",
            "gpt-5.1-codex",
            "grok"
        };

        /// <summary>
        /// Fallback models for OpenRouter CLI if discovery fails.
        /// Popular models across different providers via OpenRouter.
        /// </summary>
        public static readonly string[] DefaultOpenRouterModels =
        {
            "openrouter/auto",
            "anthropic/claude-sonnet-4",
            "anthropic/claude-3.5-sonnet",
            "openai/gpt-4o",
            "openai/gpt-4-turbo",
            "google/gemini-pro-1.5",
            "meta-llama/llama-3.1-70b-instruct"
        };

        /// <summary>
        /// Gets fallback default models for the specified provider.
        /// </summary>
        public static string[] GetDefaultModels(ProviderType provider)
        {
            return provider switch
            {
                ProviderType.Claude => DefaultClaudeModels,
                ProviderType.Codex => DefaultCodexModels,
                ProviderType.Gemini => DefaultGeminiModels,
                ProviderType.Cursor => DefaultCursorModels,
                ProviderType.OpenRouter => DefaultOpenRouterModels,
                _ => new string[0]
            };
        }

        /// <summary>
        /// Prompt to ask Claude CLI for available models.
        /// </summary>
        public const string ClaudeDiscoveryPrompt =
            "List available model aliases for Claude Code --model flag. Just model names, one per line, no explanation or code blocks.";

        /// <summary>
        /// Prompt to ask Codex CLI for available models.
        /// </summary>
        public const string CodexDiscoveryPrompt =
            "List available model names for OpenAI Codex CLI --model flag. Just model IDs, one per line, no explanation.";

        /// <summary>
        /// Prompt to ask Gemini CLI for available models.
        /// </summary>
        public const string GeminiDiscoveryPrompt =
            "List available model names for Gemini CLI --model flag. Just model IDs, one per line, no explanation.";

        /// <summary>
        /// Prompt to ask Cursor CLI for available models.
        /// </summary>
        public const string CursorDiscoveryPrompt =
            "List available model names for cursor-agent --model flag. Just model IDs, one per line, no explanation.";

        /// <summary>
        /// OpenRouter uses CLI command 'openrouter models' for discovery - no prompt needed.
        /// </summary>
        public const string OpenRouterDiscoveryPrompt = "";

        /// <summary>
        /// Gets the discovery prompt for the specified provider.
        /// </summary>
        public static string GetDiscoveryPrompt(ProviderType provider)
        {
            return provider switch
            {
                ProviderType.Claude => ClaudeDiscoveryPrompt,
                ProviderType.Codex => CodexDiscoveryPrompt,
                ProviderType.Gemini => GeminiDiscoveryPrompt,
                ProviderType.Cursor => CursorDiscoveryPrompt,
                ProviderType.OpenRouter => OpenRouterDiscoveryPrompt,
                _ => ""
            };
        }
    }
}
