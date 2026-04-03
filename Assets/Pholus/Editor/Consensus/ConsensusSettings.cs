using System;
using System.Collections.Generic;
using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Consensus
{
    /// <summary>
    /// Settings for multi-provider consensus analysis.
    /// </summary>
    [Serializable]
    public class ConsensusSettings
    {
        /// <summary>
        /// Whether consensus mode is enabled.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Providers to use as analyzers.
        /// </summary>
        public List<ProviderType> Analyzers = new List<ProviderType>();

        /// <summary>
        /// Provider to use as the director/judge.
        /// </summary>
        public ProviderType Director = ProviderType.Claude;

        /// <summary>
        /// Whether to show provider breakdown in results.
        /// </summary>
        public bool ShowProviderBreakdown = true;

        /// <summary>
        /// Creates settings from the current settings.
        /// </summary>
        public static ConsensusSettings FromSettings(Core.PholusSettings settings)
        {
            return new ConsensusSettings
            {
                Enabled = settings.EnableConsensusMode,
                Analyzers = settings.ConsensusAnalyzers,
                Director = settings.ConsensusDirector,
                ShowProviderBreakdown = settings.ShowProviderBreakdown
            };
        }
    }
}
