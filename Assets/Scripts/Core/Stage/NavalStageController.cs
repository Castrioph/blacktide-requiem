using System;
using System.Collections.Generic;
using BlacktideRequiem.Core.AI;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Stage
{
    /// <summary>
    /// Resultado de armar una batalla naval: config para CombatManager, el
    /// barco aliado ya tripulado y una AI propia por enemigo (los jefes
    /// llevan estado de fase — nunca compartir instancia, ADR-004/S4-05).
    /// </summary>
    public class NavalBattleSetup
    {
        public BattleConfig Config;
        public ShipCombatant AllyShip;
        public Dictionary<ICombatant, ICombatInput> EnemyInputs;
    }

    /// <summary>
    /// Construye batallas navales desde NavalStageData. Pure C# — testeable
    /// en EditMode. Equivalente naval de StageController.BuildBattleConfig.
    /// </summary>
    public static class NavalStageController
    {
        /// <summary>
        /// Arma la batalla: barco aliado tripulado por <paramref name="crew"/>
        /// (asignación en orden de RoleSlots, ciclada; el guest slot se omite —
        /// 2º capitán es S4-08), enemigos con crew del EnemyCrewPool del stage
        /// y una NavalEnemyAI por enemigo. Evalúa sinergias de toda crew viva.
        /// </summary>
        public static NavalBattleSetup BuildNavalBattle(
            NavalStageData stage, IReadOnlyList<CharacterData> crew)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (stage.PlayerShip == null)
                throw new ArgumentException("NavalStageData sin PlayerShip", nameof(stage));

            var ally = BuildShip(stage.PlayerShip, crew, isBoss: false);
            ally.EvaluateCrewSynergies();

            var enemyInputs = new Dictionary<ICombatant, ICombatInput>();
            var waves = new List<WaveConfig>();

            if (stage.NavalWaves != null)
            {
                foreach (var waveDef in stage.NavalWaves)
                {
                    if (waveDef?.Ships == null) continue;
                    var wave = new WaveConfig { Enemies = new List<InitiativeEntry>() };
                    int slot = 0;
                    foreach (var shipData in waveDef.Ships)
                    {
                        if (shipData == null) continue;

                        bool hasSlots = shipData.RoleSlots != null && shipData.RoleSlots.Count > 0;
                        var enemy = BuildShip(shipData,
                            hasSlots ? stage.EnemyCrewPool : null,
                            isBoss: shipData.Tier == EnemyTier.Jefe);
                        if (enemy.Crew.Count > 0)
                            enemy.EvaluateCrewSynergies();

                        wave.Enemies.Add(new InitiativeEntry(enemy, CombatTeam.Enemy, slot++));
                        enemyInputs[enemy] = NavalEnemyAI.FromShipData(shipData);
                    }
                    if (wave.Enemies.Count > 0)
                        waves.Add(wave);
                }
            }

            if (waves.Count == 0)
                throw new ArgumentException("NavalStageData sin oleadas navales", nameof(stage));

            var config = new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                {
                    new InitiativeEntry(ally, CombatTeam.Ally, 0)
                },
                Waves = waves,
                CaptainIndex = 0
            };

            return new NavalBattleSetup
            {
                Config = config,
                AllyShip = ally,
                EnemyInputs = enemyInputs
            };
        }

        /// <summary>
        /// Tripula un barco asignando units a sus RoleSlots en orden (cicladas
        /// si hay menos units que slots). Sin units → barco sin crew (criatura).
        /// </summary>
        public static ShipCombatant BuildShip(ShipData data,
            IReadOnlyList<CharacterData> units, bool isBoss)
        {
            Dictionary<int, CharacterData> crewBySlot = null;
            if (units != null && units.Count > 0 && data.RoleSlots != null)
            {
                crewBySlot = new Dictionary<int, CharacterData>();
                int i = 0;
                foreach (var slot in data.RoleSlots)
                {
                    if (slot.IsGuestSlot) continue; // guest = 2º capitán (S4-08)
                    crewBySlot[slot.SlotIndex] = units[i % units.Count];
                    i++;
                }
            }

            return new ShipCombatant(data, new ShipUpgradeState(), crewBySlot)
            {
                IsBoss = isBoss
            };
        }
    }
}
