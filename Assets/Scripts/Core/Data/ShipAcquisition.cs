namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// How a ship is acquired by the player.
    /// See Ship Data Model GDD §5.
    /// </summary>
    public enum ShipAcquisition
    {
        /// <summary>Given during story progression.</summary>
        Story,

        /// <summary>Crafted with materials from naval stages.</summary>
        Crafted,

        /// <summary>Future: pulled from ship gacha (full game only).</summary>
        Gacha
    }
}
