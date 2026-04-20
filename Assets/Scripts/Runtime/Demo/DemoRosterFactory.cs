using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Runtime.Demo
{
    /// <summary>
    /// Builds in-memory demo roster ScriptableObjects: 9 abilities + 3 characters.
    /// Used by the Editor asset generator (persists to .asset files) and by
    /// EditMode tests (asserts over the same data).
    /// Keep this file free of UnityEditor references.
    /// </summary>
    public static class DemoRosterFactory
    {
        public const string StormBoltId        = "storm_bolt";
        public const string ChainLightningId   = "chain_lightning";
        public const string CurseOfSparksId    = "curse_of_sparks";
        public const string PowderBlastId      = "powder_blast";
        public const string ScatterShotId      = "scatter_shot";
        public const string SearingShotId      = "searing_shot";
        public const string HealingTideId      = "healing_tide";
        public const string MendingCurrentId   = "mending_current";
        public const string LullabyId          = "lullaby";

        public const string ElenaId   = "elena_tempestad";
        public const string KaelId    = "kael_polvora";
        public const string MirraId   = "mirra_mareamadre";

        /// <summary>
        /// Builds all 9 demo AbilityData instances, keyed by ability Id.
        /// Each call returns fresh ScriptableObject instances.
        /// </summary>
        public static Dictionary<string, AbilityData> BuildAbilities()
        {
            var map = new Dictionary<string, AbilityData>(9);

            // --- Elena (Tormenta) ---
            map[StormBoltId] = MakeAbility(StormBoltId, "Rayo de Tormenta",
                "Descarga eléctrica sobre un enemigo.",
                power: 1.4f, Element.Tormenta, isPhysical: false,
                TargetType.SingleEnemy, AbilityCategory.Damage,
                mpCost: 8, cooldown: 0);

            map[ChainLightningId] = MakeAbility(ChainLightningId, "Cadena de Rayos",
                "Salto eléctrico que alcanza a todos los enemigos.",
                power: 1.05f, Element.Tormenta, isPhysical: false,
                TargetType.AoeEnemy, AbilityCategory.Damage,
                mpCost: 20, cooldown: 2);

            map[CurseOfSparksId] = MakeAbility(CurseOfSparksId, "Maldición de Chispas",
                "Descarga menor que sella las habilidades del objetivo.",
                power: 0.6f, Element.Tormenta, isPhysical: false,
                TargetType.SingleEnemy, AbilityCategory.Debuff,
                mpCost: 10, cooldown: 3,
                secondaries: new[]
                {
                    new AbilitySecondaryEffect
                    {
                        Effect = StatusEffect.Silencio,
                        Probability = 0.75f,
                        Duration = 2,
                        Param = 0f
                    }
                });

            // --- Kael (Pólvora) ---
            map[PowderBlastId] = MakeAbility(PowderBlastId, "Andanada de Pólvora",
                "Disparo cargado de pólvora concentrada.",
                power: 1.6f, Element.Polvora, isPhysical: true,
                TargetType.SingleEnemy, AbilityCategory.Damage,
                mpCost: 10, cooldown: 2);

            map[ScatterShotId] = MakeAbility(ScatterShotId, "Metralla Dispersa",
                "Ráfaga de perdigones que golpea a todos los enemigos.",
                power: 1.15f, Element.Polvora, isPhysical: true,
                TargetType.AoeEnemy, AbilityCategory.Damage,
                mpCost: 15, cooldown: 3);

            map[SearingShotId] = MakeAbility(SearingShotId, "Disparo Ardiente",
                "Bala incendiaria que prende al objetivo.",
                power: 1.2f, Element.Polvora, isPhysical: true,
                TargetType.SingleEnemy, AbilityCategory.Damage,
                mpCost: 12, cooldown: 2,
                secondaries: new[]
                {
                    new AbilitySecondaryEffect
                    {
                        Effect = StatusEffect.Quemadura,
                        Probability = 0.6f,
                        Duration = 3,
                        Param = 0.05f
                    }
                });

            // --- Mirra (Neutral) ---
            map[HealingTideId] = MakeAbility(HealingTideId, "Marea Sanadora",
                "Ola de agua bendita que restaura HP a un aliado.",
                power: 1.2f, Element.Neutral, isPhysical: false,
                TargetType.SingleAlly, AbilityCategory.Heal,
                mpCost: 12, cooldown: 1,
                healPower: 1.2f);

            map[MendingCurrentId] = MakeAbility(MendingCurrentId, "Corriente Reparadora",
                "Corriente sagrada que cura a todo el grupo.",
                power: 0.8f, Element.Neutral, isPhysical: false,
                TargetType.AllyAoe, AbilityCategory.Heal,
                mpCost: 22, cooldown: 2,
                healPower: 0.8f);

            map[LullabyId] = MakeAbility(LullabyId, "Canción de Cuna",
                "Melodía que duerme al enemigo mientras causa daño leve.",
                power: 0.3f, Element.Neutral, isPhysical: false,
                TargetType.SingleEnemy, AbilityCategory.Debuff,
                mpCost: 14, cooldown: 3,
                secondaries: new[]
                {
                    new AbilitySecondaryEffect
                    {
                        Effect = StatusEffect.Sueno,
                        Probability = 0.8f,
                        Duration = 2,
                        Param = 0f
                    }
                });

            return map;
        }

        /// <summary>
        /// Builds the 3 demo CharacterData instances referencing the given abilities.
        /// Keyed by character Id.
        /// </summary>
        public static Dictionary<string, CharacterData> BuildCharacters(
            Dictionary<string, AbilityData> abilities)
        {
            var map = new Dictionary<string, CharacterData>(3);

            map[ElenaId] = MakeCharacter(ElenaId, "Elena",
                "Maga de tormenta que desata rayos y sellos arcanos.",
                Element.Tormenta,
                hp: 320, mp: 110, atk: 45, def: 30, mst: 70, spr: 42, spd: 75,
                cri: 7f, lck: 6f,
                new[]
                {
                    abilities[StormBoltId],
                    abilities[ChainLightningId],
                    abilities[CurseOfSparksId]
                });

            map[KaelId] = MakeCharacter(KaelId, "Kael",
                "Artillero terrestre de cañón corto y temple duro.",
                Element.Polvora,
                hp: 400, mp: 60, atk: 72, def: 46, mst: 28, spr: 26, spd: 55,
                cri: 8f, lck: 4f,
                new[]
                {
                    abilities[PowderBlastId],
                    abilities[ScatterShotId],
                    abilities[SearingShotId]
                });

            map[MirraId] = MakeCharacter(MirraId, "Mirra",
                "Sanadora ritual que canaliza el mar en bendiciones y cantos.",
                Element.Neutral,
                hp: 280, mp: 130, atk: 35, def: 25, mst: 58, spr: 52, spd: 65,
                cri: 4f, lck: 8f,
                new[]
                {
                    abilities[HealingTideId],
                    abilities[MendingCurrentId],
                    abilities[LullabyId]
                });

            return map;
        }

        private static AbilityData MakeAbility(
            string id, string displayName, string description,
            float power, Element element, bool isPhysical,
            TargetType targetType, AbilityCategory category,
            int mpCost, int cooldown,
            float healPower = 0f,
            AbilitySecondaryEffect[] secondaries = null)
        {
            var a = ScriptableObject.CreateInstance<AbilityData>();
            a.Id = id;
            a.DisplayName = displayName;
            a.Description = description;
            a.AbilityPower = power;
            a.Element = element;
            a.IsPhysical = isPhysical;
            a.TargetType = targetType;
            a.Category = category;
            a.MPCost = mpCost;
            a.Cooldown = cooldown;
            a.HealPower = healPower;
            a.SecondaryEffects = new List<AbilitySecondaryEffect>(
                secondaries ?? System.Array.Empty<AbilitySecondaryEffect>());
            return a;
        }

        private static CharacterData MakeCharacter(
            string id, string displayName, string description,
            Element element,
            float hp, float mp, float atk, float def, float mst, float spr, float spd,
            float cri, float lck,
            AbilityData[] abilities)
        {
            var c = ScriptableObject.CreateInstance<CharacterData>();
            c.Id = id;
            c.DisplayName = displayName;
            c.Description = description;
            c.Element = element;
            c.BaseStats = new StatBlock
            {
                HP = hp, MP = mp, ATK = atk, DEF = def,
                MST = mst, SPR = spr, SPD = spd
            };
            c.SecondaryStats = new SecondaryStatBlock { CRI = cri, LCK = lck };
            c.LandAbilities = new List<AbilityEntry>();
            foreach (var ability in abilities)
            {
                c.LandAbilities.Add(new AbilityEntry
                {
                    Ability = ability,
                    UnlockLevel = 1,
                    Source = AbilitySource.Learned,
                    CanLimitBreak = false,
                    LBConditionParam = -1f,
                    LBConditionTarget = string.Empty
                });
            }
            c.SeaAbilities = new List<AbilityEntry>();
            c.Traits = new List<UnitTraitEntry>();
            return c;
        }
    }
}
