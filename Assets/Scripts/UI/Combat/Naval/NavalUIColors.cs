using UnityEngine;

namespace BlacktideRequiem.UI.Combat.Naval
{
    /// <summary>
    /// Canonical color constants for the naval combat HUD.
    /// Source of truth: docs/art/ui-s406-naval-visual-design.md color token table
    /// and .claude/docs/coplay-unity-lessons.md §4.
    /// All values are in Unity linear Color space (as used by Image.color).
    /// </summary>
    internal static class NavalUIColors
    {
        // --- Backgrounds ---
        public static readonly Color BgDark        = new Color(0.08f, 0.06f, 0.14f);   // #140F24
        public static readonly Color PanelDark     = new Color(0.12f, 0.09f, 0.18f);   // #1F172E
        public static readonly Color HeaderBase    = new Color(0.10f, 0.05f, 0.00f);   // #1A0D00
        public static readonly Color HintBarBg     = new Color(0.05f, 0.04f, 0.08f);   // #0D0A14

        // --- Gold family ---
        public static readonly Color Gold          = new Color(0.83f, 0.63f, 0.09f);   // #D4A017
        public static readonly Color GoldBright    = new Color(1.00f, 0.84f, 0.00f);   // #FFD700
        public static readonly Color GoldMid       = new Color(0.91f, 0.71f, 0.13f);   // #E8B420
        public static readonly Color GoldDark      = new Color(0.72f, 0.53f, 0.06f);   // #B8880F

        // --- Text ---
        public static readonly Color Cream         = new Color(0.93f, 0.85f, 0.64f);   // #EDD9A3
        public static readonly Color CreamMuted    = new Color(0.96f, 0.90f, 0.78f);   // #F5E6C8

        // --- Wood (buttons, panels) ---
        public static readonly Color WoodBase      = new Color(0.24f, 0.16f, 0.06f, 0.902f);  // #3D2810 a230
        public static readonly Color WoodLight     = new Color(0.29f, 0.19f, 0.09f);   // #4A3018
        public static readonly Color WoodBorder    = new Color(0.36f, 0.24f, 0.12f);   // #5C3D1E

        // --- Buttons ---
        public static readonly Color BtnDisabledBg = new Color(0.24f, 0.19f, 0.13f);   // #3D3020
        public static readonly Color DisabledLabel = new Color(0.63f, 0.50f, 0.25f, 0.71f);  // #A08040 a180

        // --- HP / resource bars ---
        public static readonly Color HpHigh        = new Color(0.30f, 0.69f, 0.31f);   // #4CAF50
        public static readonly Color HpMid         = new Color(1.00f, 0.79f, 0.16f);   // #FFCA28
        public static readonly Color HpLow         = new Color(0.94f, 0.33f, 0.31f);   // #EF5350
        public static readonly Color MpBlue        = new Color(0.12f, 0.53f, 0.90f);   // #1E88E5
        public static readonly Color MpBlueDark    = new Color(0.05f, 0.28f, 0.63f);   // #0D47A1
        public static readonly Color LbGold        = new Color(0.83f, 0.63f, 0.09f);   // #D4A017 = Gold
        public static readonly Color LbEmpty       = new Color(0.16f, 0.13f, 0.06f);   // #2A2010

        // --- HP bar backgrounds (context-sensitive) ---
        public static readonly Color HpBgNormal    = new Color(0.10f, 0.16f, 0.10f, 0.86f);  // #1A2A1A
        public static readonly Color HpBgCritical  = new Color(0.16f, 0.06f, 0.06f, 0.86f);  // #2A1010
        public static readonly Color MpBarBg       = new Color(0.05f, 0.10f, 0.16f, 0.86f);  // #0D1A2A

        // --- Targeting ---
        public static readonly Color TargetGreen   = new Color(0.30f, 0.69f, 0.31f);   // #4CAF50
        public static readonly Color TargetGold    = new Color(1.00f, 0.84f, 0.00f);   // #FFD700 = GoldBright

        // --- DoT tints ---
        public static readonly Color DotBurn       = new Color(1.00f, 0.44f, 0.00f);   // #FF6F00
        public static readonly Color DotPoison     = new Color(0.49f, 0.70f, 0.26f);   // #7CB342
        public static readonly Color DotBleed      = new Color(0.78f, 0.16f, 0.16f);   // #C62828
        public static readonly Color DotSilence    = new Color(0.48f, 0.12f, 0.64f);   // #7B1FA2
        public static readonly Color DotBuff       = new Color(0.08f, 0.40f, 0.75f);   // #1565C0

        // --- Maneuver shield ---
        public static readonly Color ManeuverBlue  = new Color(0.16f, 0.71f, 0.96f);   // #29B6F6

        // --- Crew chip ---
        public static readonly Color ChipDeadOverlay = new Color(0.10f, 0.10f, 0.10f, 0.55f); // #1A1A1A a140

        // --- Slots (crew panel allied) ---
        public static readonly Color SlotFilled    = new Color(0.16f, 0.12f, 0.06f);   // #2A1E10
        public static readonly Color SlotBorderEmpty = new Color(0.23f, 0.16f, 0.31f); // #3A2A50

        // --- Log ---
        public static readonly Color LogDamageDealt  = new Color(0.30f, 0.69f, 0.31f); // HpHigh
        public static readonly Color LogDamageReceived = new Color(0.94f, 0.33f, 0.31f); // HpLow
        public static readonly Color LogCrewDeath    = new Color(0.94f, 0.33f, 0.31f); // HpLow

        // --- Battle result ---
        public static readonly Color VictoryGold   = new Color(0.83f, 0.63f, 0.09f);   // Gold
        public static readonly Color DefeatRed      = new Color(0.94f, 0.33f, 0.31f);   // HpLow

        // --- Misc ---
        public static readonly Color Shadow         = new Color(0.02f, 0.05f, 0.08f);   // #050D14
        public static readonly Color VoodooViolet   = new Color(0.42f, 0.11f, 0.60f);   // #6A1B9A

        // ====================================================================
        // HELPERS
        // ====================================================================

        /// <summary>Returns the HP bar fill color for a given normalized HP ratio.</summary>
        public static Color HpBarColor(float ratio)
        {
            if (ratio <= 0.25f) return HpLow;
            if (ratio <= 0.50f) return HpMid;
            return HpHigh;
        }

        /// <summary>Returns the HP bar background color for a given normalized HP ratio.</summary>
        public static Color HpBgColor(float ratio)
        {
            return ratio <= 0.25f ? HpBgCritical : HpBgNormal;
        }

        /// <summary>
        /// Returns the role color tint for crew chip icons.
        /// Matches §8 of the visual design spec.
        /// </summary>
        public static Color RoleColor(BlacktideRequiem.Core.Data.NavalRole role)
        {
            return role switch
            {
                BlacktideRequiem.Core.Data.NavalRole.Capitan       => Gold,
                BlacktideRequiem.Core.Data.NavalRole.Intendente    => Cream,
                BlacktideRequiem.Core.Data.NavalRole.Artillero     => HpLow,
                BlacktideRequiem.Core.Data.NavalRole.Navegante     => MpBlue,
                BlacktideRequiem.Core.Data.NavalRole.Carpintero    => new Color(0.55f, 0.43f, 0.39f), // #8D6E63
                BlacktideRequiem.Core.Data.NavalRole.Cirujano      => HpHigh,
                BlacktideRequiem.Core.Data.NavalRole.Contramaestre => new Color(1.00f, 0.70f, 0.00f), // #FFB300
                _ => Cream
            };
        }

        /// <summary>
        /// Returns the tint color for a DoT status effect icon.
        /// </summary>
        public static Color DotColor(BlacktideRequiem.Core.Data.StatusEffect effect)
        {
            return effect switch
            {
                BlacktideRequiem.Core.Data.StatusEffect.Quemadura => DotBurn,
                BlacktideRequiem.Core.Data.StatusEffect.Veneno    => DotPoison,
                BlacktideRequiem.Core.Data.StatusEffect.Sangrado  => DotBleed,
                BlacktideRequiem.Core.Data.StatusEffect.Silencio  => DotSilence,
                _ => DotBuff
            };
        }

        /// <summary>Returns the role's canonical 2-letter ID string for chip labels.</summary>
        public static string RoleLabel(BlacktideRequiem.Core.Data.NavalRole role)
        {
            return role switch
            {
                BlacktideRequiem.Core.Data.NavalRole.Capitan       => "CA",
                BlacktideRequiem.Core.Data.NavalRole.Intendente    => "IN",
                BlacktideRequiem.Core.Data.NavalRole.Artillero     => "AR",
                BlacktideRequiem.Core.Data.NavalRole.Navegante     => "NA",
                BlacktideRequiem.Core.Data.NavalRole.Carpintero    => "CP",  // Not "CA" — art-director §20 note 1
                BlacktideRequiem.Core.Data.NavalRole.Cirujano      => "CI",
                BlacktideRequiem.Core.Data.NavalRole.Contramaestre => "CO",
                _ => "??"
            };
        }
    }
}
