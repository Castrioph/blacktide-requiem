namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Identifies which naval stat a modifier targets.
    /// Maps to ShipStatBlock indices.
    /// See Ship Data Model GDD §1.
    /// </summary>
    public enum ShipStatType
    {
        HHP,
        FPW,
        HDF,
        MST,
        MP,
        RSL,
        SPD
    }
}
