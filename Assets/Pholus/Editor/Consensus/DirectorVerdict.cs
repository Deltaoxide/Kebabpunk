using System;
using System.Collections.Generic;
using System.Linq;
using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Consensus
{
    /// <summary>
    /// The director's verdict after reviewing all analyzer results.
    /// </summary>
    [Serializable]
    public class DirectorVerdict
    {
        /// <summary>
        /// Individual verdict items for each issue.
        /// </summary>
        public List<VerdictItem> Items = new List<VerdictItem>();

        /// <summary>
        /// Overall summary from the director.
        /// </summary>
        public string Summary;

        /// <summary>
        /// How many issues were validated.
        /// </summary>
        public int ValidCount => Items?.FindAll(i => i.IsValid).Count ?? 0;

        /// <summary>
        /// How many issues were dismissed.
        /// </summary>
        public int DismissedCount => Items?.FindAll(i => !i.IsValid).Count ?? 0;
    }

    /// <summary>
    /// A single issue verdict from the director.
    /// </summary>
    [Serializable]
    public class VerdictItem
    {
        /// <summary>
        /// Title of the issue.
        /// </summary>
        public string IssueTitle;

        /// <summary>
        /// Line number in the code.
        /// </summary>
        public int Line;

        /// <summary>
        /// Whether the director validated this issue.
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// Final severity assigned by director.
        /// </summary>
        public IssueSeverity? FinalSeverity;

        /// <summary>
        /// Director's reasoning for the verdict.
        /// </summary>
        public string DirectorReasoning;

        // Serializable backing storage for ProviderOpinions (Unity can't serialize Dictionary)
        [UnityEngine.SerializeField]
        private List<ProviderType> _opinionKeys = new List<ProviderType>();
        [UnityEngine.SerializeField]
        private List<ProviderOpinion> _opinionValues = new List<ProviderOpinion>();

        // Non-serialized runtime dictionary (rebuilt from lists)
        [NonSerialized]
        private Dictionary<ProviderType, ProviderOpinion> _providerOpinions;

        /// <summary>
        /// What each provider said about this issue (the "debate").
        /// </summary>
        public Dictionary<ProviderType, ProviderOpinion> ProviderOpinions
        {
            get
            {
                if (_providerOpinions == null)
                {
                    _providerOpinions = new Dictionary<ProviderType, ProviderOpinion>();
                    for (int i = 0; i < _opinionKeys.Count && i < _opinionValues.Count; i++)
                    {
                        _providerOpinions[_opinionKeys[i]] = _opinionValues[i];
                    }
                }
                return _providerOpinions;
            }
            set
            {
                _providerOpinions = value ?? new Dictionary<ProviderType, ProviderOpinion>();
                _opinionKeys = _providerOpinions.Keys.ToList();
                _opinionValues = _providerOpinions.Values.ToList();
            }
        }
    }

    /// <summary>
    /// A single provider's opinion on an issue.
    /// </summary>
    [Serializable]
    public class ProviderOpinion
    {
        /// <summary>
        /// Whether this provider found the issue.
        /// </summary>
        public bool FoundIssue;

        /// <summary>
        /// What the provider said about the issue.
        /// </summary>
        public string Opinion;

        /// <summary>
        /// Severity rating from this provider (null if not found).
        /// </summary>
        public IssueSeverity? Severity;

        /// <summary>
        /// Creates an opinion for when provider found the issue.
        /// </summary>
        public static ProviderOpinion Found(string opinion, IssueSeverity severity)
        {
            return new ProviderOpinion
            {
                FoundIssue = true,
                Opinion = opinion,
                Severity = severity
            };
        }

        /// <summary>
        /// Creates an opinion for when provider did not find the issue.
        /// </summary>
        public static ProviderOpinion NotFound()
        {
            return new ProviderOpinion
            {
                FoundIssue = false,
                Opinion = null,
                Severity = null
            };
        }
    }
}
