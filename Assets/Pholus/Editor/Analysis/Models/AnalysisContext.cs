using System;

namespace Pholus.Editor.Analysis.Models
{
    /// <summary>
    /// Context settings for analysis that affect detection thresholds and output.
    /// </summary>
    [Serializable]
    public class AnalysisContext
    {
        /// <summary>
        /// Target platform determines strictness of thresholds.
        /// </summary>
        public TargetPlatform Platform { get; set; } = TargetPlatform.Mobile;

        /// <summary>
        /// Whether to include "why I might be wrong" explanations.
        /// </summary>
        public bool ShowWhyMightBeWrong { get; set; } = true;

        /// <summary>
        /// Whether to group issues by certainty category.
        /// </summary>
        public bool GroupByCertainty { get; set; } = true;

        public static AnalysisContext Default => new AnalysisContext();

        public static AnalysisContext ForMobile => new AnalysisContext
        {
            Platform = TargetPlatform.Mobile
        };

        public static AnalysisContext ForPC => new AnalysisContext
        {
            Platform = TargetPlatform.PCConsole
        };

        public static AnalysisContext ForVR => new AnalysisContext
        {
            Platform = TargetPlatform.VR
        };
    }
}
