using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis;
using Pholus.Editor.Analysis.Interfaces;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Core;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Consensus
{
    /// <summary>
    /// Service for multi-provider consensus analysis.
    /// Runs analysis with multiple providers and has a director judge the results.
    /// </summary>
    public class ConsensusService
    {
        private readonly IProviderFactory _providerFactory;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IResponseParser _responseParser;

        public event Action<string> OnStatusUpdate;

        public ConsensusService(
            IProviderFactory providerFactory,
            IPromptBuilder promptBuilder,
            IResponseParser responseParser)
        {
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
        }

        /// <summary>
        /// Creates a ConsensusService with default dependencies.
        /// </summary>
        public static ConsensusService Create()
        {
            return new ConsensusService(
                PholusServices.Get<IProviderFactory>(),
                new PromptBuilder(),
                new ResponseParser());
        }

        /// <summary>
        /// Performs consensus analysis on a script.
        /// </summary>
        public async Task<ConsensusResult> AnalyzeWithConsensusAsync(
            string scriptPath,
            ConsensusSettings settings,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ConsensusResult
            {
                AnalyzersUsed = new List<ProviderType>(settings.Analyzers),
                DirectorUsed = settings.Director
            };

            try
            {
                var fileName = Path.GetFileName(scriptPath);
                PholusLogger.Log($"Consensus: Starting analysis with {settings.Analyzers.Count} analyzers, Director: {settings.Director}");

                // Read script content
                if (!File.Exists(scriptPath))
                {
                    throw new FileNotFoundException($"Script not found: {scriptPath}");
                }

                var scriptContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);

                if (string.IsNullOrWhiteSpace(scriptContent))
                {
                    result.FinalResult = AnalysisResult.Empty(scriptPath);
                    return result;
                }

                // Build the analysis prompt
                var context = PholusSettings.Instance.CreateAnalysisContext();
                var prompt = _promptBuilder is PromptBuilder pb
                    ? pb.BuildPromptWithFileName(scriptContent, fileName, context)
                    : _promptBuilder.BuildPrompt(scriptContent, context);

                // Step 1: Run all analyzers in parallel
                OnStatusUpdate?.Invoke("Running analyzers in parallel...");
                var analyzerResults = await RunAnalyzersAsync(prompt, settings.Analyzers, cancellationToken);
                result.ProviderResults = analyzerResults;

                // Check if we have enough results
                var successfulResults = analyzerResults.Where(r => r.Value != null).ToList();
                if (successfulResults.Count < 2)
                {
                    PholusLogger.LogWarning("Consensus: Less than 2 analyzers succeeded, falling back to single result");
                    result.IsPartialConsensus = true;

                    if (successfulResults.Count == 1)
                    {
                        result.FinalResult = successfulResults.First().Value;
                        result.FinalResult.ScriptPath = scriptPath;
                        return result;
                    }
                    else
                    {
                        throw new Exception("All analyzers failed");
                    }
                }

                // Step 2: Format results for director
                OnStatusUpdate?.Invoke("Preparing results for director review...");
                var directorPrompt = DirectorPromptBuilder.BuildReviewPrompt(scriptContent, analyzerResults);

                // Step 3: Send to director for judgment
                OnStatusUpdate?.Invoke($"Director ({settings.Director}) reviewing results...");
                var (verdict, directorUsage) = await GetDirectorVerdictAsync(directorPrompt, settings.Director, cancellationToken);
                result.Verdict = verdict;

                // Step 4: Apply verdict to create final result
                OnStatusUpdate?.Invoke("Applying director verdict...");
                result.FinalResult = ApplyVerdict(analyzerResults, verdict, scriptPath);

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.AnalyzedAt = DateTime.Now;

                // Aggregate total usage from all providers + director
                var allUsages = analyzerResults.Values
                    .Where(r => r?.Usage != null)
                    .Select(r => r.Usage)
                    .ToList();
                if (directorUsage != null)
                {
                    allUsages.Add(directorUsage);
                }
                if (allUsages.Count > 0)
                {
                    result.TotalUsage = new TokenUsage
                    {
                        InputTokens = allUsages.Sum(u => u.InputTokens),
                        OutputTokens = allUsages.Sum(u => u.OutputTokens),
                        CacheCreationTokens = allUsages.Sum(u => u.CacheCreationTokens),
                        CacheReadTokens = allUsages.Sum(u => u.CacheReadTokens),
                        IsActual = allUsages.All(u => u.IsActual)
                    };
                    result.FinalResult.Usage = result.TotalUsage;
                }

                PholusLogger.Log($"Consensus: Complete in {stopwatch.Elapsed.TotalSeconds:F1}s. " +
                         $"Valid: {verdict.ValidCount}, Dismissed: {verdict.DismissedCount}" +
                         (result.TotalUsage != null ? $", Tokens: {result.TotalUsage.TotalTokens:N0}" : ""));

                return result;
            }
            catch (OperationCanceledException)
            {
                PholusLogger.Log("Consensus: Analysis cancelled");
                throw;
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Consensus: Failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Runs all analyzer providers in parallel.
        /// </summary>
        private async Task<Dictionary<ProviderType, AnalysisResult>> RunAnalyzersAsync(
            string prompt,
            List<ProviderType> analyzers,
            CancellationToken cancellationToken)
        {
            var results = new Dictionary<ProviderType, AnalysisResult>();
            var tasks = new List<Task<(ProviderType Type, AnalysisResult Result, string Error)>>();

            foreach (var analyzerType in analyzers)
            {
                tasks.Add(RunSingleAnalyzerAsync(prompt, analyzerType, cancellationToken));
            }

            var taskResults = await Task.WhenAll(tasks);

            foreach (var (type, analysisResult, error) in taskResults)
            {
                if (analysisResult != null)
                {
                    results[type] = analysisResult;
                    PholusLogger.Log($"Consensus: {type} found {analysisResult.TotalIssueCount} issues");
                }
                else
                {
                    PholusLogger.LogError($"Consensus: PROVIDER FAILED: {type} - {error}");
                }
            }

            return results;
        }

        private async Task<(ProviderType Type, AnalysisResult Result, string Error)> RunSingleAnalyzerAsync(
            string prompt,
            ProviderType analyzerType,
            CancellationToken cancellationToken)
        {
            try
            {
                var provider = _providerFactory.CreateProvider(analyzerType);

                // Get selected model for this specific analyzer provider
                var selectedModel = PholusSettings.Instance.GetSelectedModel(analyzerType);
                var response = await provider.SendPromptAsync(prompt, selectedModel, cancellationToken);

                if (!response.Success)
                {
                    // Check if this is a stale model error - auto-clear and retry with default
                    if (!string.IsNullOrEmpty(selectedModel) && PholusSettings.Instance.HandleStaleModelError(response.Error, analyzerType, selectedModel))
                    {
                        // Get the default model for retry (some providers like Cursor need "auto")
                        var defaultModel = PholusSettings.GetDefaultModel(analyzerType);
                        PholusLogger.Log($"Retrying {analyzerType} with model: {defaultModel ?? "CLI Default"}...");
                        response = await provider.SendPromptAsync(prompt, defaultModel, cancellationToken);

                        if (!response.Success)
                        {
                            return (analyzerType, null, response.Error);
                        }
                    }
                    else
                    {
                        return (analyzerType, null, response.Error);
                    }
                }

                if (_responseParser.TryParse(response.Output, out var result, out var parseError))
                {
                    // Attach usage info from response
                    if (response.Usage != null)
                    {
                        result.Usage = response.Usage;
                        SessionUsageTracker.AddUsage(response.Usage);
                    }
                    return (analyzerType, result, null);
                }
                else
                {
                    return (analyzerType, null, $"Parse error: {parseError}");
                }
            }
            catch (Exception ex)
            {
                return (analyzerType, null, ex.Message);
            }
        }

        /// <summary>
        /// Sends combined results to the director for judgment.
        /// </summary>
        private async Task<(DirectorVerdict Verdict, TokenUsage Usage)> GetDirectorVerdictAsync(
            string directorPrompt,
            ProviderType directorType,
            CancellationToken cancellationToken)
        {
            var provider = _providerFactory.CreateProvider(directorType);

            // Get selected model for the director provider
            var selectedModel = PholusSettings.Instance.GetSelectedModel(directorType);
            var response = await provider.SendPromptAsync(directorPrompt, selectedModel, cancellationToken);

            if (!response.Success)
            {
                // Check if this is a stale model error - auto-clear and retry with default
                if (!string.IsNullOrEmpty(selectedModel) && PholusSettings.Instance.HandleStaleModelError(response.Error, directorType, selectedModel))
                {
                    var defaultModel = PholusSettings.GetDefaultModel(directorType);
                    PholusLogger.Log($"Retrying Director ({directorType}) with model: {defaultModel ?? "CLI Default"}...");
                    response = await provider.SendPromptAsync(directorPrompt, defaultModel, cancellationToken);

                    if (!response.Success)
                    {
                        throw new Exception($"Director failed: {response.Error}");
                    }
                }
                else
                {
                    throw new Exception($"Director failed: {response.Error}");
                }
            }

            // Track director usage
            if (response.Usage != null)
            {
                SessionUsageTracker.AddUsage(response.Usage);
            }

            // Parse the director's verdict
            var verdict = DirectorVerdictParser.Parse(response.Output);
            return (verdict, response.Usage);
        }

        /// <summary>
        /// Applies the director's verdict to create the final filtered result.
        /// </summary>
        private AnalysisResult ApplyVerdict(
            Dictionary<ProviderType, AnalysisResult> analyzerResults,
            DirectorVerdict verdict,
            string scriptPath)
        {
            var result = new AnalysisResult
            {
                ScriptPath = scriptPath,
                AnalyzedAt = DateTime.Now,
                DefiniteIssues = new List<PerformanceIssue>(),
                ContextualIssues = new List<PerformanceIssue>(),
                Suggestions = new List<PerformanceIssue>(),
                PositiveNotes = new List<string>(),
                Summary = verdict.Summary ?? "Consensus analysis complete."
            };

            // Calculate score based on validated issues only
            // Start at 100 and deduct based on validated issue severities
            var validatedIssues = verdict.Items.Where(i => i.IsValid).ToList();
            var score = 100;
            foreach (var item in validatedIssues)
            {
                switch (item.FinalSeverity)
                {
                    case IssueSeverity.Critical: score -= 15; break;
                    case IssueSeverity.High: score -= 10; break;
                    case IssueSeverity.Medium: score -= 5; break;
                    case IssueSeverity.Low: score -= 2; break;
                }
            }
            result.Score = Math.Max(0, score);
            PholusLogger.Log($"Consensus: Score: {result.Score} (deducted for {validatedIssues.Count} validated issues, {verdict.DismissedCount} dismissed)");

            // Add validated issues - categorize by Director's severity rating
            foreach (var item in verdict.Items.Where(i => i.IsValid))
            {
                var issue = CreateIssueFromVerdict(item, analyzerResults);

                // Categorize by severity (set by Director)
                // Critical/High = Definite, Medium = Contextual, Low = Suggestion
                PholusLogger.Log($"Consensus: Issue '{item.IssueTitle}': severity={issue.Severity}, providers={item.ProviderOpinions.Count}");

                switch (issue.Severity)
                {
                    case IssueSeverity.Critical:
                    case IssueSeverity.High:
                        issue.Category = IssueCategory.Definite;
                        result.DefiniteIssues.Add(issue);
                        break;
                    case IssueSeverity.Medium:
                        issue.Category = IssueCategory.Contextual;
                        result.ContextualIssues.Add(issue);
                        break;
                    case IssueSeverity.Low:
                    default:
                        issue.Category = IssueCategory.Suggestion;
                        result.Suggestions.Add(issue);
                        break;
                }
            }

            // Add dismissed issues for transparency
            foreach (var item in verdict.Items.Where(i => !i.IsValid))
            {
                var issue = CreateDismissedIssueFromVerdict(item);
                result.DismissedIssues.Add(issue);
                PholusLogger.Log($"Consensus: Dismissed: '{item.IssueTitle}' - {item.DirectorReasoning}");
            }

            // Sort all lists by severity (Critical > High > Medium > Low)
            result.DefiniteIssues = result.DefiniteIssues.OrderBy(i => i.Severity).ToList();
            result.ContextualIssues = result.ContextualIssues.OrderBy(i => i.Severity).ToList();
            result.Suggestions = result.Suggestions.OrderBy(i => i.Severity).ToList();

            // Collect unique positive notes from all providers
            var positiveNotes = new HashSet<string>();
            foreach (var providerResult in analyzerResults.Values.Where(r => r != null))
            {
                foreach (var note in providerResult.PositiveNotes)
                {
                    positiveNotes.Add(note);
                }
            }
            result.PositiveNotes = positiveNotes.ToList();

            return result;
        }

        private PerformanceIssue CreateIssueFromVerdict(
            VerdictItem verdictItem,
            Dictionary<ProviderType, AnalysisResult> analyzerResults)
        {
            // Find the best explanation, WhyIMightBeWrong, and Impact from the providers
            string bestExplanation = null;
            string bestWhyMightBeWrong = null;
            string bestImpact = null;
            string bestCodeSnippet = null;
            IssueSeverity severity = verdictItem.FinalSeverity ?? IssueSeverity.Medium;

            foreach (var (providerType, opinion) in verdictItem.ProviderOpinions)
            {
                if (opinion.FoundIssue && analyzerResults.TryGetValue(providerType, out var providerResult))
                {
                    // Find the matching issue in the provider's results
                    var allIssues = providerResult.DefiniteIssues
                        .Concat(providerResult.ContextualIssues)
                        .Concat(providerResult.Suggestions)
                        .ToList();

                    // Try matching by line first, then by title similarity
                    var matchingIssue = allIssues.FirstOrDefault(i => Math.Abs(i.Line - verdictItem.Line) <= 2);

                    if (matchingIssue == null)
                    {
                        // Try matching by title keywords
                        matchingIssue = allIssues.FirstOrDefault(i =>
                            i.Title?.Contains(verdictItem.IssueTitle ?? "", StringComparison.OrdinalIgnoreCase) == true ||
                            verdictItem.IssueTitle?.Contains(i.Title ?? "", StringComparison.OrdinalIgnoreCase) == true);
                    }

                    if (matchingIssue != null)
                    {
                        if (string.IsNullOrEmpty(bestExplanation) && !string.IsNullOrEmpty(matchingIssue.Explanation))
                            bestExplanation = matchingIssue.Explanation;
                        if (string.IsNullOrEmpty(bestWhyMightBeWrong) && !string.IsNullOrEmpty(matchingIssue.WhyIMightBeWrong))
                            bestWhyMightBeWrong = matchingIssue.WhyIMightBeWrong;
                        if (string.IsNullOrEmpty(bestImpact) && !string.IsNullOrEmpty(matchingIssue.Impact))
                            bestImpact = matchingIssue.Impact;
                        if (string.IsNullOrEmpty(bestCodeSnippet) && !string.IsNullOrEmpty(matchingIssue.CodeSnippet))
                            bestCodeSnippet = matchingIssue.CodeSnippet;
                    }
                }
            }

            // Build agreement string for title
            var foundBy = verdictItem.ProviderOpinions
                .Where(o => o.Value.FoundIssue)
                .Select(o => o.Key.ToString())
                .ToList();
            var agreementTag = $"[{foundBy.Count}/{verdictItem.ProviderOpinions.Count}]";

            // Generate fallback WhyIMightBeWrong if not available from providers
            if (string.IsNullOrEmpty(bestWhyMightBeWrong))
            {
                bestWhyMightBeWrong = GenerateWhyMightBeWrong(verdictItem, foundBy.Count);
            }

            // Calculate confidence based on provider agreement
            var totalProviders = verdictItem.ProviderOpinions.Count;
            var confidencePercent = totalProviders > 0
                ? (int)Math.Round((double)foundBy.Count / totalProviders * 100)
                : 0;

            return new PerformanceIssue
            {
                Title = $"{agreementTag} {verdictItem.IssueTitle}",
                Line = verdictItem.Line,
                Severity = severity,
                Explanation = bestExplanation ?? verdictItem.DirectorReasoning,
                WhyIMightBeWrong = bestWhyMightBeWrong,
                Impact = bestImpact,
                CodeSnippet = bestCodeSnippet,
                ConfidencePercent = confidencePercent,
                Confidence = confidencePercent >= 80 ? IssueConfidence.High
                           : confidencePercent >= 50 ? IssueConfidence.Medium
                           : IssueConfidence.Low
            };
        }

        private PerformanceIssue CreateDismissedIssueFromVerdict(VerdictItem verdictItem)
        {
            // Build provider list for title
            var foundBy = verdictItem.ProviderOpinions
                .Where(o => o.Value.FoundIssue)
                .Select(o => o.Key.ToString())
                .ToList();
            var agreementTag = $"[{foundBy.Count}/{verdictItem.ProviderOpinions.Count}]";

            return new PerformanceIssue
            {
                Title = $"{agreementTag} {verdictItem.IssueTitle}",
                Line = verdictItem.Line,
                Severity = verdictItem.FinalSeverity ?? IssueSeverity.Low,
                Explanation = verdictItem.DirectorReasoning ?? "Dismissed as false positive by Director.",
                Category = IssueCategory.Suggestion // Use suggestion category for styling
            };
        }

        private string GenerateWhyMightBeWrong(VerdictItem verdictItem, int agreementCount)
        {
            var title = verdictItem.IssueTitle?.ToLower() ?? "";
            var totalProviders = verdictItem.ProviderOpinions.Count;

            // Generate realistic "why I might be wrong" based on issue type
            if (title.Contains("getcomponent"))
            {
                return "If this method is called infrequently (e.g., only on specific events), caching may add unnecessary complexity.";
            }
            if (title.Contains("find") || title.Contains("findobj"))
            {
                return "If the scene is small or this runs rarely, the performance impact may be negligible.";
            }
            if (title.Contains("camera.main"))
            {
                return "Unity 2020.2+ caches Camera.main internally, reducing the performance penalty significantly.";
            }
            if (title.Contains("instantiate"))
            {
                return "If objects are created infrequently, object pooling may add unnecessary complexity for minimal gain.";
            }
            if (title.Contains("string") || title.Contains("concat"))
            {
                return "If this runs infrequently or the strings are short, the GC impact may be acceptable.";
            }
            if (title.Contains("linq"))
            {
                return "LINQ readability benefits may outweigh performance costs if this isn't in a hot path.";
            }
            if (title.Contains("list") || title.Contains("array") || title.Contains("alloc"))
            {
                return "If the collection size is small and this runs infrequently, pooling may be over-engineering.";
            }
            if (title.Contains("waitforseconds"))
            {
                return "If the coroutine runs infrequently, the small allocation may not justify caching complexity.";
            }
            if (title.Contains("physics") || title.Contains("raycast"))
            {
                return "If the scene has few colliders or this runs rarely, layer masks may not provide significant improvement.";
            }
            if (title.Contains("sendmessage") || title.Contains("broadcast"))
            {
                return "If called rarely and with few receivers, the reflection overhead may be acceptable for code simplicity.";
            }

            // Generic fallback based on agreement
            if (agreementCount == totalProviders && totalProviders > 1)
            {
                return "While multiple analyzers agree, the actual performance impact depends on how frequently this code executes in your specific use case.";
            }

            return "The severity of this issue depends on execution frequency and your target platform's performance constraints.";
        }
    }
}
