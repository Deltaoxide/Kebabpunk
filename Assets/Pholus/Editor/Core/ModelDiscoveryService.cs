using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Providers;
using UnityEngine;

namespace Pholus.Editor.Core
{
    /// <summary>
    /// Service for discovering available models from CLI providers.
    /// Asks each CLI for available models by sending a discovery prompt.
    /// </summary>
    public static class ModelDiscoveryService
    {
        /// <summary>
        /// Discovers available models for the specified provider by asking the CLI.
        /// </summary>
        /// <param name="provider">The provider to discover models for.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>List of discovered model names, or empty list if discovery fails.</returns>
        public static async Task<List<string>> DiscoverModelsAsync(
            ProviderType provider,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // OpenRouter uses CLI command directly, no prompt needed
                if (provider == ProviderType.OpenRouter)
                {
                    var openRouterResponse = await DiscoverOpenRouterModelsAsync(cancellationToken);
                    if (!openRouterResponse.Success || string.IsNullOrEmpty(openRouterResponse.Output))
                    {
                        PholusLogger.LogWarning($"Model discovery failed for OpenRouter: {openRouterResponse.Error}");
                        return new List<string>();
                    }
                    var openRouterModels = ParseModelResponse(openRouterResponse.Output, provider);
                    PholusLogger.Log($"Discovered {openRouterModels.Count} models for OpenRouter");
                    return openRouterModels;
                }

                var prompt = ModelConfiguration.GetDiscoveryPrompt(provider);
                if (string.IsNullOrEmpty(prompt))
                {
                    PholusLogger.LogWarning($"No discovery prompt for provider: {provider}");
                    return new List<string>();
                }

                CLIResponse response;

                switch (provider)
                {
                    case ProviderType.Claude:
                        response = await DiscoverClaudeModelsAsync(prompt, cancellationToken);
                        break;
                    case ProviderType.Codex:
                        response = await DiscoverCodexModelsAsync(prompt, cancellationToken);
                        break;
                    case ProviderType.Gemini:
                        response = await DiscoverGeminiModelsAsync(prompt, cancellationToken);
                        break;
                    case ProviderType.Cursor:
                        response = await DiscoverCursorModelsAsync(prompt, cancellationToken);
                        break;
                    default:
                        return new List<string>();
                }

                if (!response.Success || string.IsNullOrEmpty(response.Output))
                {
                    PholusLogger.LogWarning($"Model discovery failed for {provider}: {response.Error}");
                    return new List<string>();
                }

                var models = ParseModelResponse(response.Output, provider);
                PholusLogger.Log($"Discovered {models.Count} models for {provider}: {string.Join(", ", models)}");

                return models;
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Model discovery error for {provider}: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Discovers Claude models using the Claude CLI.
        /// </summary>
        private static async Task<CLIResponse> DiscoverClaudeModelsAsync(
            string prompt,
            CancellationToken cancellationToken)
        {
            // Claude uses --print -p - to read from stdin
            // Don't use --output-format json for discovery as we want plain text
            return await ProcessRunner.RunWithStdinAsync(
                "claude",
                "--print -p -",
                prompt,
                timeoutMs: 60000, // 1 minute timeout for discovery
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Codex CLI has no way to list models programmatically.
        /// Returns empty - users should use CLI Default or add custom models.
        /// </summary>
        private static Task<CLIResponse> DiscoverCodexModelsAsync(
            string prompt,
            CancellationToken cancellationToken)
        {
            // No dynamic discovery available - return empty
            return Task.FromResult(CLIResponse.Successful("", TimeSpan.Zero));
        }

        /// <summary>
        /// Gemini CLI has no way to list models programmatically.
        /// Returns empty - users should use CLI Default or add custom models.
        /// </summary>
        private static Task<CLIResponse> DiscoverGeminiModelsAsync(
            string prompt,
            CancellationToken cancellationToken)
        {
            // No dynamic discovery available - return empty
            return Task.FromResult(CLIResponse.Successful("", TimeSpan.Zero));
        }

        /// <summary>
        /// Discovers Cursor models by sending an invalid model and parsing the error.
        /// The error message contains "Available models: model1, model2, ..."
        /// </summary>
        private static async Task<CLIResponse> DiscoverCursorModelsAsync(
            string prompt,
            CancellationToken cancellationToken)
        {
            // Trick: send invalid model to get list of available models from error message
            CLIResponse response;
            if (CursorProvider.IsWindowsPlatform)
            {
                response = await ProcessRunner.RunWSLWithStdinAsync(
                    "cursor-agent",
                    "-p --model invalid-model-for-discovery",
                    "hi",
                    timeoutMs: 30000,
                    cancellationToken: cancellationToken);
            }
            else
            {
                response = await ProcessRunner.RunWithStdinAsync(
                    "cursor-agent",
                    "-p --model invalid-model-for-discovery",
                    "hi",
                    timeoutMs: 30000,
                    cancellationToken: cancellationToken);
            }

            // Parse "Available models: model1, model2, ..." from error
            var error = response.Error ?? response.Output ?? "";
            var match = Regex.Match(error, @"Available models:\s*(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                // Return the model list as successful output for parsing
                var modelList = match.Groups[1].Value;
                // Convert comma-separated to newline-separated for standard parsing
                var models = modelList.Replace(", ", "\n").Replace(",", "\n");
                return CLIResponse.Successful(models, response.Duration);
            }

            // If no match, return original response
            return response;
        }

        /// <summary>
        /// Discovers OpenRouter models using the 'openrouter models' CLI command.
        /// Parses the table output to extract model IDs.
        /// </summary>
        private static async Task<CLIResponse> DiscoverOpenRouterModelsAsync(
            CancellationToken cancellationToken)
        {
            // Get API key from settings for the environment
            var envVars = new Dictionary<string, string>();
            var storedApiKey = PholusSettings.Instance.OpenRouterApiKey;
            if (!string.IsNullOrEmpty(storedApiKey))
            {
                envVars["OPENROUTER_API_KEY"] = storedApiKey;
            }

            // Use 'openrouter models' command to list available models
            // This prints a table in non-TTY mode
            var response = await ProcessRunner.RunAsync(
                "openrouter",
                "models",
                timeoutMs: 30000,
                cancellationToken: cancellationToken);

            if (!response.Success)
            {
                return response;
            }

            // Parse the table output - models are in format: provider/model-name
            var output = response.Output ?? "";
            var models = new System.Text.StringBuilder();

            foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();

                // Look for model IDs in format: provider/model-name
                var match = Regex.Match(trimmed, @"\b([a-z0-9\-]+/[a-z0-9\-\.:]+)\b", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    models.AppendLine(match.Groups[1].Value);
                }
            }

            return CLIResponse.Successful(models.ToString(), response.Duration);
        }

        /// <summary>
        /// Parses the CLI response to extract model names.
        /// </summary>
        private static List<string> ParseModelResponse(string response, ProviderType provider)
        {
            var models = new List<string>();

            // Split by newlines
            var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Skip lines that look like explanatory text
                if (IsExplanatoryText(trimmed))
                    continue;

                // Extract model name from the line
                var modelName = ExtractModelName(trimmed, provider);
                if (!string.IsNullOrEmpty(modelName) && !models.Contains(modelName))
                {
                    models.Add(modelName);
                }
            }

            return models;
        }

        /// <summary>
        /// Checks if a line is explanatory text rather than a model name.
        /// </summary>
        private static bool IsExplanatoryText(string line)
        {
            var lower = line.ToLowerInvariant();

            // Skip common explanatory phrases
            var skipPhrases = new[]
            {
                "based on",
                "available",
                "following",
                "here are",
                "you can use",
                "model name",
                "example",
                "note:",
                "the",
                "these",
                "also",
                "additionally",
                "full",
                "directly",
                "e.g.",
                "such as",
                "including"
            };

            // Skip if starts with these phrases
            foreach (var phrase in skipPhrases)
            {
                if (lower.StartsWith(phrase))
                    return true;
            }

            // Skip if contains certain patterns indicating explanatory text
            if (lower.Contains("can also") || lower.Contains("you can") || lower.Contains("api model"))
                return true;

            // Skip if it's too long to be a model name (likely a sentence)
            if (line.Length > 50 && line.Contains(" "))
                return true;

            // Skip markdown headers
            if (line.StartsWith("#") || line.StartsWith("*") || line.StartsWith("-") && line.Length > 30)
                return true;

            return false;
        }

        /// <summary>
        /// Extracts a model name from a line.
        /// </summary>
        private static string ExtractModelName(string line, ProviderType provider)
        {
            // Remove leading bullets, numbers, dashes
            var cleaned = Regex.Replace(line, @"^[\s\-\*\•\d\.]+", "").Trim();

            // Remove backticks (code formatting)
            cleaned = cleaned.Replace("`", "").Trim();

            // Remove trailing punctuation
            cleaned = cleaned.TrimEnd('.', ',', ':', ';');

            // If there's a colon or dash with description, take the first part
            var colonIndex = cleaned.IndexOf(':');
            if (colonIndex > 0 && colonIndex < 30)
            {
                cleaned = cleaned.Substring(0, colonIndex).Trim();
            }

            var dashIndex = cleaned.IndexOf(" - ");
            if (dashIndex > 0 && dashIndex < 30)
            {
                cleaned = cleaned.Substring(0, dashIndex).Trim();
            }

            // Validate model name format based on provider
            if (IsValidModelName(cleaned, provider))
            {
                return cleaned;
            }

            return null;
        }

        /// <summary>
        /// Validates if a string looks like a valid model name.
        /// </summary>
        private static bool IsValidModelName(string name, ProviderType provider)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Model names should be short
            if (name.Length < 2 || name.Length > 50)
                return false;

            // Model names shouldn't contain spaces (with few exceptions)
            if (name.Contains(" ") && !name.Contains("-"))
                return false;

            // Provider-specific validation
            switch (provider)
            {
                case ProviderType.Claude:
                    // Claude CLI only accepts short aliases, not full model IDs
                    return Regex.IsMatch(name, @"^(sonnet|opus|haiku|opusplan|sonnet1m|default)$", RegexOptions.IgnoreCase);

                case ProviderType.Codex:
                    // Codex models: gpt-*, o3, o4, etc.
                    return Regex.IsMatch(name, @"^(gpt[\w\-\.]+|o\d[\w\-]*|codex[\w\-]*)$", RegexOptions.IgnoreCase);

                case ProviderType.Gemini:
                    // Gemini models: gemini-*, etc.
                    return Regex.IsMatch(name, @"^(gemini[\w\-\.]*|pro|flash)$", RegexOptions.IgnoreCase);

                case ProviderType.Cursor:
                    // Cursor models: auto, composer-*, sonnet-*, opus-*, gpt-*, gemini-*, grok
                    return Regex.IsMatch(name, @"^(auto|composer[\w\-\.]*|sonnet[\w\-\.]*|opus[\w\-\.]*|gpt[\w\-\.]*|gemini[\w\-\.]*|grok)$", RegexOptions.IgnoreCase);

                case ProviderType.OpenRouter:
                    // OpenRouter models: provider/model-name format (e.g., anthropic/claude-3.5-sonnet)
                    return Regex.IsMatch(name, @"^[\w\-]+/[\w\-\.:]+$", RegexOptions.IgnoreCase);

                default:
                    // Generic: alphanumeric with hyphens/dots/underscores
                    return Regex.IsMatch(name, @"^[\w\-\.]+$");
            }
        }
    }
}
