using System;
using System.Collections.Generic;
using System.Linq;
using Pholus.Editor.Analysis.Models;
using UnityEngine;

namespace Pholus.Editor.Consensus
{
    /// <summary>
    /// Result of a consensus analysis across multiple providers.
    /// </summary>
    [Serializable]
    public class ConsensusResult
    {
        /// <summary>
        /// The final filtered analysis result.
        /// </summary>
        public AnalysisResult FinalResult;

        // Serializable backing storage for ProviderResults (Unity can't serialize Dictionary)
        [SerializeField]
        private List<ProviderType> _providerResultKeys = new List<ProviderType>();
        [SerializeField]
        private List<AnalysisResult> _providerResultValues = new List<AnalysisResult>();

        [NonSerialized]
        private Dictionary<ProviderType, AnalysisResult> _providerResults;

        /// <summary>
        /// Raw results from each analyzer provider.
        /// </summary>
        public Dictionary<ProviderType, AnalysisResult> ProviderResults
        {
            get
            {
                if (_providerResults == null)
                {
                    _providerResults = new Dictionary<ProviderType, AnalysisResult>();
                    for (int i = 0; i < _providerResultKeys.Count && i < _providerResultValues.Count; i++)
                    {
                        _providerResults[_providerResultKeys[i]] = _providerResultValues[i];
                    }
                }
                return _providerResults;
            }
            set
            {
                _providerResults = value ?? new Dictionary<ProviderType, AnalysisResult>();
                _providerResultKeys = _providerResults.Keys.ToList();
                _providerResultValues = _providerResults.Values.ToList();
            }
        }

        /// <summary>
        /// The director's verdict on all issues.
        /// </summary>
        public DirectorVerdict Verdict;

        /// <summary>
        /// Which providers were used as analyzers.
        /// </summary>
        public List<ProviderType> AnalyzersUsed = new List<ProviderType>();

        /// <summary>
        /// Which provider was used as the director.
        /// </summary>
        public ProviderType DirectorUsed;

        /// <summary>
        /// When the consensus analysis was performed.
        /// </summary>
        public DateTime AnalyzedAt;

        /// <summary>
        /// Total time taken for the consensus analysis.
        /// </summary>
        public TimeSpan Duration;

        /// <summary>
        /// Whether the consensus was partial (some providers failed).
        /// </summary>
        public bool IsPartialConsensus;

        // Serializable backing storage for FailedProviders (Unity can't serialize Dictionary)
        [SerializeField]
        private List<ProviderType> _failedProviderKeys = new List<ProviderType>();
        [SerializeField]
        private List<string> _failedProviderValues = new List<string>();

        [NonSerialized]
        private Dictionary<ProviderType, string> _failedProviders;

        /// <summary>
        /// Error messages from failed providers.
        /// </summary>
        public Dictionary<ProviderType, string> FailedProviders
        {
            get
            {
                if (_failedProviders == null)
                {
                    _failedProviders = new Dictionary<ProviderType, string>();
                    for (int i = 0; i < _failedProviderKeys.Count && i < _failedProviderValues.Count; i++)
                    {
                        _failedProviders[_failedProviderKeys[i]] = _failedProviderValues[i];
                    }
                }
                return _failedProviders;
            }
            set
            {
                _failedProviders = value ?? new Dictionary<ProviderType, string>();
                _failedProviderKeys = _failedProviders.Keys.ToList();
                _failedProviderValues = _failedProviders.Values.ToList();
            }
        }

        /// <summary>
        /// Combined token usage from all providers + director.
        /// </summary>
        public TokenUsage TotalUsage;
    }
}
