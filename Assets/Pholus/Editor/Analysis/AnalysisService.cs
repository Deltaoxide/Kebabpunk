using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Interfaces;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Core;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Analysis
{
    /// <summary>
    /// Orchestrates script analysis workflow.
    /// Depends on abstractions (Dependency Inversion Principle).
    /// </summary>
    public class AnalysisService : IAnalysisService
    {
        private readonly ICLIProvider _provider;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IResponseParser _responseParser;

        public event Action<string> OnAnalysisStarted;
        public event Action<AnalysisResult> OnAnalysisCompleted;
        public event Action<string, Exception> OnAnalysisFailed;

        public AnalysisService(
            ICLIProvider provider,
            IPromptBuilder promptBuilder,
            IResponseParser responseParser)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
        }

        /// <summary>
        /// Creates an AnalysisService with dependencies from the service locator.
        /// </summary>
        public static AnalysisService Create()
        {
            var provider = PholusServices.Get<ICLIProvider>();
            var promptBuilder = new PromptBuilder();
            var responseParser = new ResponseParser();

            return new AnalysisService(provider, promptBuilder, responseParser);
        }

        public Task<AnalysisResult> AnalyzeScriptAsync(
            string scriptPath,
            CancellationToken cancellationToken = default)
        {
            var context = PholusSettings.Instance.CreateAnalysisContext();
            return AnalyzeScriptAsync(scriptPath, context, cancellationToken);
        }

        public async Task<AnalysisResult> AnalyzeScriptAsync(
            string scriptPath,
            AnalysisContext context,
            CancellationToken cancellationToken = default)
        {
            var fileName = Path.GetFileName(scriptPath);

            try
            {
                OnAnalysisStarted?.Invoke(scriptPath);
                PholusLogger.Log($"Analyzing: {fileName}");

                // Read script content
                if (!File.Exists(scriptPath))
                {
                    throw new FileNotFoundException($"Script not found: {scriptPath}");
                }

                var scriptContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);

                if (string.IsNullOrWhiteSpace(scriptContent))
                {
                    return AnalysisResult.Empty(scriptPath);
                }

                // Build prompt
                var prompt = _promptBuilder is PromptBuilder pb
                    ? pb.BuildPromptWithFileName(scriptContent, fileName, context)
                    : _promptBuilder.BuildPrompt(scriptContent, context);

                // Run analysis
                var result = await RunSingleAnalysis(prompt, cancellationToken);

                result.ScriptPath = scriptPath;
                result.AnalyzedAt = DateTime.Now;

                PholusLogger.Log($"Analysis complete. Score: {result.Score}, Issues: {result.TotalIssueCount}");

                OnAnalysisCompleted?.Invoke(result);
                return result;
            }
            catch (OperationCanceledException)
            {
                PholusLogger.Log("Analysis cancelled");
                throw;
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Analysis failed: {ex.Message}");
                OnAnalysisFailed?.Invoke(scriptPath, ex);
                throw;
            }
        }

        private async Task<AnalysisResult> RunSingleAnalysis(string prompt, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            // Get selected model for the provider
            var selectedModel = PholusSettings.Instance.GetSelectedModel(_provider.ProviderType);
            var response = await _provider.SendPromptAsync(prompt, selectedModel, cancellationToken);

            if (!response.Success)
            {
                // Check if this is a stale model error - auto-clear and retry with default
                if (!string.IsNullOrEmpty(selectedModel) && PholusSettings.Instance.HandleStaleModelError(response.Error, _provider.ProviderType, selectedModel))
                {
                    var defaultModel = PholusSettings.GetDefaultModel(_provider.ProviderType);
                    PholusLogger.Log($"Retrying {_provider.ProviderType} with model: {defaultModel ?? "CLI Default"}...");
                    response = await _provider.SendPromptAsync(prompt, defaultModel, cancellationToken);

                    if (!response.Success)
                    {
                        throw new Exception($"AI request failed: {response.Error}");
                    }
                }
                else
                {
                    throw new Exception($"AI request failed: {response.Error}");
                }
            }

            stopwatch.Stop();

            PholusLogger.Log($"Response received in {response.Duration.TotalSeconds:F1}s");

            if (!_responseParser.TryParse(response.Output, out var result, out var parseError))
            {
                PholusLogger.LogWarning($"Parse error: {parseError}");
                PholusLogger.LogWarning($"Raw response: {response.Output}");
                throw new Exception($"Failed to parse AI response: {parseError}");
            }

            // Attach usage info if available
            if (response.Usage != null)
            {
                result.Usage = response.Usage;
                SessionUsageTracker.AddUsage(response.Usage);
            }

            return result;
        }

        public async Task<ProjectScanResult> ScanProjectAsync(
            IEnumerable<string> scriptPaths,
            IProgress<ScanProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            var paths = new List<string>(scriptPaths);
            var result = new ProjectScanResult
            {
                Results = new List<AnalysisResult>(),
                ScannedAt = startTime
            };

            var context = PholusSettings.Instance.CreateAnalysisContext();

            for (var i = 0; i < paths.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var path = paths[i];
                var fileName = Path.GetFileName(path);

                progress?.Report(new ScanProgress
                {
                    CurrentFile = fileName,
                    CurrentIndex = i + 1,
                    TotalFiles = paths.Count
                });

                try
                {
                    var analysisResult = await AnalyzeScriptAsync(path, context, cancellationToken);
                    result.Results.Add(analysisResult);
                }
                catch (Exception ex)
                {
                    PholusLogger.LogWarning($"Failed to analyze {fileName}: {ex.Message}");
                    result.Results.Add(AnalysisResult.Error(path, ex.Message));
                }

                // Small delay between requests to avoid rate limiting
                if (i < paths.Count - 1)
                {
                    await Task.Delay(500, cancellationToken);
                }
            }

            result.Duration = DateTime.Now - startTime;

            PholusLogger.Log($"Project scan complete. " +
                     $"Analyzed: {result.ScriptsAnalyzed}, " +
                     $"With issues: {result.ScriptsWithIssues}, " +
                     $"Total issues: {result.TotalIssues}");

            return result;
        }
    }
}
