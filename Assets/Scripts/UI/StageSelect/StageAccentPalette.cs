using UnityEngine;

namespace BlacktideRequiem.UI.StageSelect
{
    /// <summary>
    /// Per-stage accent colors (visual identity markers). UI-only lookup,
    /// keyed by StageData.Id prefix. See docs/art/ui-s311-visual-design.md §1.2.
    /// </summary>
    public static class StageAccentPalette
    {
        public readonly struct StageAccent
        {
            public readonly Color Primary;
            public readonly Color Secondary;

            public StageAccent(Color primary, Color secondary)
            {
                Primary = primary;
                Secondary = secondary;
            }
        }

        // Corsair Blue / Sea Foam
        private static readonly StageAccent Bahia = new StageAccent(
            new Color(0.118f, 0.533f, 0.898f), new Color(0.310f, 0.765f, 0.969f));

        // Voodoo Violet / Pale Violet
        private static readonly StageAccent Muelle = new StageAccent(
            new Color(0.416f, 0.106f, 0.604f), new Color(0.808f, 0.576f, 0.847f));

        // Temple Ember / Ember Orange
        private static readonly StageAccent Templo = new StageAccent(
            new Color(0.749f, 0.212f, 0.047f), new Color(1f, 0.541f, 0.396f));

        // Gold fallback for stages without a defined identity
        private static readonly StageAccent Fallback = new StageAccent(
            new Color(0.831f, 0.627f, 0.090f), new Color(0.949f, 0.780f, 0.251f));

        public static StageAccent Get(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return Fallback;
            if (stageId.StartsWith("stage_001")) return Bahia;
            if (stageId.StartsWith("stage_002")) return Muelle;
            if (stageId.StartsWith("stage_003")) return Templo;
            return Fallback;
        }
    }
}
