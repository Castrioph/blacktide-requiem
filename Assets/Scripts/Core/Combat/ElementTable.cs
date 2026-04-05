using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Elemental advantage/disadvantage lookup.
    /// Pentagonal cycle: Polvora → Acero → Bestia → Maldicion → Tormenta → Polvora
    /// Dual pair: Luz ↔ Sombra (mutually strong)
    /// Neutral element always returns 1.0.
    ///
    /// See Damage & Stats Engine GDD §3.
    /// </summary>
    public static class ElementTable
    {
        public const float ADVANTAGE_MOD = 1.25f;
        public const float NEUTRAL_MOD = 1.00f;
        public const float DISADVANTAGE_MOD = 0.75f;

        /// <summary>
        /// Returns the element modifier for an attack.
        /// </summary>
        /// <param name="abilityElement">Offensive element of the ability.</param>
        /// <param name="targetElement">Defensive element of the target.</param>
        /// <returns>ElementMod: 0.75, 1.0, or 1.25.</returns>
        public static float GetElementMod(Element abilityElement, Element targetElement)
        {
            if (abilityElement == Element.Neutral || targetElement == Element.Neutral)
                return NEUTRAL_MOD;

            if (abilityElement == targetElement)
                return NEUTRAL_MOD;

            // Luz ↔ Sombra: mutually strong
            if ((abilityElement == Element.Luz && targetElement == Element.Sombra) ||
                (abilityElement == Element.Sombra && targetElement == Element.Luz))
                return ADVANTAGE_MOD;

            // Luz/Sombra vs pentagonal elements: neutral
            if (abilityElement == Element.Luz || abilityElement == Element.Sombra ||
                targetElement == Element.Luz || targetElement == Element.Sombra)
                return NEUTRAL_MOD;

            // Pentagonal cycle: Polvora → Acero → Bestia → Maldicion → Tormenta → Polvora
            Element strongAgainst = GetStrongAgainst(abilityElement);
            if (targetElement == strongAgainst)
                return ADVANTAGE_MOD;

            Element weakAgainst = GetWeakAgainst(abilityElement);
            if (targetElement == weakAgainst)
                return DISADVANTAGE_MOD;

            return NEUTRAL_MOD;
        }

        /// <summary>
        /// Returns the element this element is strong against in the pentagonal cycle.
        /// </summary>
        private static Element GetStrongAgainst(Element element)
        {
            return element switch
            {
                Element.Polvora => Element.Acero,
                Element.Acero => Element.Bestia,
                Element.Bestia => Element.Maldicion,
                Element.Maldicion => Element.Tormenta,
                Element.Tormenta => Element.Polvora,
                _ => Element.Neutral
            };
        }

        /// <summary>
        /// Returns the element this element is weak against in the pentagonal cycle.
        /// </summary>
        private static Element GetWeakAgainst(Element element)
        {
            return element switch
            {
                Element.Polvora => Element.Tormenta,
                Element.Tormenta => Element.Maldicion,
                Element.Maldicion => Element.Bestia,
                Element.Bestia => Element.Acero,
                Element.Acero => Element.Polvora,
                _ => Element.Neutral
            };
        }
    }
}
