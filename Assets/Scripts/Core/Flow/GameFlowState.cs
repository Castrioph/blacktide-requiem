namespace BlacktideRequiem.Core.Flow
{
    /// <summary>
    /// Represents the current state of the game flow / screen the player is on.
    /// </summary>
    public enum GameFlowState
    {
        None,
        MainMenu,
        StageSelect,
        TeamSelect,
        Combat,
        Results
    }
}
