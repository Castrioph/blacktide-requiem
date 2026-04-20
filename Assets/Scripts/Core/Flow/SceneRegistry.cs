namespace BlacktideRequiem.Core.Flow
{
    /// <summary>
    /// Central registry of scene names used by the game flow system.
    /// Keeps scene name strings in one place to avoid magic strings.
    /// </summary>
    public static class SceneRegistry
    {
        public const string MainMenu = "MainMenu";
        public const string Combat = "CombatDemo";
        public const string Results = "Results";
    }
}
