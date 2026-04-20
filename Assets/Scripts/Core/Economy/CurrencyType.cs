namespace BlacktideRequiem.Core.Economy
{
    /// <summary>
    /// Abstract fungible currencies managed by <see cref="CurrencyWallet"/>.
    /// Inventory items (TIE/TIF tickets, materials) are NOT currencies and
    /// are owned by inventory systems. See design/gdd/currency-system.md.
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>Soft currency earned through gameplay. ID: DOB.</summary>
        Doblones,

        /// <summary>Hard premium currency. ID: GDC.</summary>
        GemasDeCalavera,
    }
}
