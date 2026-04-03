using System;
using System.Text.RegularExpressions;
using Pholus.Editor.Core;

namespace Pholus.Editor.Analysis.Models
{
    /// <summary>
    /// Tracks token usage and cost for AI provider calls.
    /// </summary>
    [Serializable]
    public class TokenUsage
    {
        /// <summary>
        /// Input tokens (prompt).
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// Output tokens (response).
        /// </summary>
        public int OutputTokens { get; set; }

        /// <summary>
        /// Cache creation tokens (for providers that support caching).
        /// </summary>
        public int CacheCreationTokens { get; set; }

        /// <summary>
        /// Cache read tokens (for providers that support caching).
        /// </summary>
        public int CacheReadTokens { get; set; }

        /// <summary>
        /// Total tokens (input + output only, excludes cache overhead).
        /// </summary>
        public int TotalTokens => InputTokens + OutputTokens;

        /// <summary>
        /// Total tokens including cache (full API usage).
        /// </summary>
        public int TotalTokensWithCache => InputTokens + OutputTokens + CacheCreationTokens + CacheReadTokens;

        /// <summary>
        /// Provider used for this request.
        /// </summary>
        public ProviderType Provider { get; set; }

        /// <summary>
        /// Whether this is actual usage (from provider) or estimated.
        /// </summary>
        public bool IsActual { get; set; }

        /// <summary>
        /// Parses token usage from Claude CLI JSON output.
        /// </summary>
        public static TokenUsage ParseFromClaudeJson(string json, ProviderType provider = ProviderType.Claude)
        {
            var usage = new TokenUsage { Provider = provider, IsActual = true };

            try
            {
                // Parse usage object
                var inputMatch = Regex.Match(json, @"""input_tokens""\s*:\s*(\d+)");
                if (inputMatch.Success && int.TryParse(inputMatch.Groups[1].Value, out var input))
                {
                    usage.InputTokens = input;
                }

                var outputMatch = Regex.Match(json, @"""output_tokens""\s*:\s*(\d+)");
                if (outputMatch.Success && int.TryParse(outputMatch.Groups[1].Value, out var output))
                {
                    usage.OutputTokens = output;
                }

                var cacheCreateMatch = Regex.Match(json, @"""cache_creation_input_tokens""\s*:\s*(\d+)");
                if (cacheCreateMatch.Success && int.TryParse(cacheCreateMatch.Groups[1].Value, out var cacheCreate))
                {
                    usage.CacheCreationTokens = cacheCreate;
                }

                var cacheReadMatch = Regex.Match(json, @"""cache_read_input_tokens""\s*:\s*(\d+)");
                if (cacheReadMatch.Success && int.TryParse(cacheReadMatch.Groups[1].Value, out var cacheRead))
                {
                    usage.CacheReadTokens = cacheRead;
                }
            }
            catch
            {
                // If parsing fails, return empty usage
                usage.IsActual = false;
            }

            return usage;
        }

        /// <summary>
        /// Formats the token count for display.
        /// </summary>
        public string ToDisplayString()
        {
            return $"{TotalTokens:N0} tokens";
        }
    }

    /// <summary>
    /// Tracks cumulative token usage across a session.
    /// Persists cache state across recompiles using EditorPrefs.
    /// </summary>
    public static class SessionUsageTracker
    {
        private static TokenUsage _sessionTotal = new TokenUsage();
        private static int _requestCount;
        private static DateTime _lastCacheHit = DateTime.MinValue;
        private static int _cachedTokens;
        private const int CacheTTLSeconds = 300; // 5 minutes

        // EditorPrefs keys
        private static readonly string PrefKeyInputTokens = $"{PholusSettings.PrefsKeyPrefix}Session_InputTokens";
        private static readonly string PrefKeyOutputTokens = $"{PholusSettings.PrefsKeyPrefix}Session_OutputTokens";
        private static readonly string PrefKeyRequestCount = $"{PholusSettings.PrefsKeyPrefix}Session_RequestCount";
        private static readonly string PrefKeyCachedTokens = $"{PholusSettings.PrefsKeyPrefix}Session_CachedTokens";
        private static readonly string PrefKeyLastCacheHit = $"{PholusSettings.PrefsKeyPrefix}Session_LastCacheHit";

        private static bool _initialized;

        /// <summary>
        /// Current session's total usage.
        /// </summary>
        public static TokenUsage SessionTotal { get { EnsureInitialized(); return _sessionTotal; } }

        /// <summary>
        /// Number of requests in this session.
        /// </summary>
        public static int RequestCount { get { EnsureInitialized(); return _requestCount; } }

        /// <summary>
        /// Tokens currently in cache.
        /// </summary>
        public static int CachedTokens { get { EnsureInitialized(); return _cachedTokens; } }

        /// <summary>
        /// Whether cache is currently valid.
        /// </summary>
        public static bool IsCacheValid { get { EnsureInitialized(); return _cachedTokens > 0 && CacheSecondsRemaining > 0; } }

        /// <summary>
        /// Seconds remaining until cache expires.
        /// </summary>
        public static int CacheSecondsRemaining
        {
            get
            {
                EnsureInitialized();
                if (_lastCacheHit == DateTime.MinValue) return 0;
                var elapsed = (DateTime.Now - _lastCacheHit).TotalSeconds;
                var remaining = CacheTTLSeconds - (int)elapsed;
                return remaining > 0 ? remaining : 0;
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            LoadFromPrefs();
        }

        private static void LoadFromPrefs()
        {
#if UNITY_EDITOR
            _sessionTotal.InputTokens = UnityEditor.EditorPrefs.GetInt(PrefKeyInputTokens, 0);
            _sessionTotal.OutputTokens = UnityEditor.EditorPrefs.GetInt(PrefKeyOutputTokens, 0);
            _requestCount = UnityEditor.EditorPrefs.GetInt(PrefKeyRequestCount, 0);
            _cachedTokens = UnityEditor.EditorPrefs.GetInt(PrefKeyCachedTokens, 0);

            var lastHitStr = UnityEditor.EditorPrefs.GetString(PrefKeyLastCacheHit, "");
            if (!string.IsNullOrEmpty(lastHitStr) && DateTime.TryParse(lastHitStr, out var parsed))
            {
                _lastCacheHit = parsed;
            }
#endif
        }

        private static void SaveToPrefs()
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetInt(PrefKeyInputTokens, _sessionTotal.InputTokens);
            UnityEditor.EditorPrefs.SetInt(PrefKeyOutputTokens, _sessionTotal.OutputTokens);
            UnityEditor.EditorPrefs.SetInt(PrefKeyRequestCount, _requestCount);
            UnityEditor.EditorPrefs.SetInt(PrefKeyCachedTokens, _cachedTokens);
            UnityEditor.EditorPrefs.SetString(PrefKeyLastCacheHit, _lastCacheHit.ToString("O"));
#endif
        }

        /// <summary>
        /// Adds usage from a request to the session total.
        /// </summary>
        public static void AddUsage(TokenUsage usage)
        {
            EnsureInitialized();
            if (usage == null) return;
            _sessionTotal.InputTokens += usage.InputTokens;
            _sessionTotal.OutputTokens += usage.OutputTokens;
            _sessionTotal.CacheCreationTokens += usage.CacheCreationTokens;
            _sessionTotal.CacheReadTokens += usage.CacheReadTokens;
            _requestCount++;

            // Track cache state
            if (usage.CacheCreationTokens > 0)
            {
                _cachedTokens = usage.CacheCreationTokens;
                _lastCacheHit = DateTime.Now;
            }
            else if (usage.CacheReadTokens > 0)
            {
                _cachedTokens = usage.CacheReadTokens;
                _lastCacheHit = DateTime.Now;
            }

            SaveToPrefs();
        }

        /// <summary>
        /// Resets the session totals.
        /// </summary>
        public static void Reset()
        {
            _sessionTotal = new TokenUsage();
            _requestCount = 0;
            _lastCacheHit = DateTime.MinValue;
            _cachedTokens = 0;
            _initialized = true;

#if UNITY_EDITOR
            UnityEditor.EditorPrefs.DeleteKey(PrefKeyInputTokens);
            UnityEditor.EditorPrefs.DeleteKey(PrefKeyOutputTokens);
            UnityEditor.EditorPrefs.DeleteKey(PrefKeyRequestCount);
            UnityEditor.EditorPrefs.DeleteKey(PrefKeyCachedTokens);
            UnityEditor.EditorPrefs.DeleteKey(PrefKeyLastCacheHit);
#endif
        }

        /// <summary>
        /// Gets a display string for session totals.
        /// </summary>
        public static string GetSessionSummary()
        {
            EnsureInitialized();
            if (_requestCount == 0)
                return "No requests yet";
            return $"Session: {_requestCount} requests, {_sessionTotal.TotalTokens:N0} tokens";
        }
    }
}
