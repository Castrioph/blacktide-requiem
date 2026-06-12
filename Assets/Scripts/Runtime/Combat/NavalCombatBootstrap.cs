using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.AI;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.UI.Combat.Naval;

namespace BlacktideRequiem.Runtime.Combat
{
    /// <summary>
    /// Standalone bootstrap for the S4-06 naval combat test scene. Builds the
    /// allied ShipCombatant with crew, the enemy waves (one NavalEnemyAI per
    /// enemy — bosses keep phase state), wires the NavalCombatHUD and starts
    /// the battle through CombatRunner with a NavalTurnResolver.
    /// S4-07 replaces this with the StageSelect → naval stage flow.
    /// </summary>
    public class NavalCombatBootstrap : MonoBehaviour
    {
        [Header("Allied ship")]
        [SerializeField] private ShipData _allyShip;
        [Tooltip("Units assigned to the allied ship's role slots, in slot order")]
        [SerializeField] private List<CharacterData> _allyCrew = new();

        [Header("Enemy waves (one entry per wave)")]
        [SerializeField] private List<NavalWaveEntry> _waves = new();

        [Tooltip("Units cycled into every enemy ship's role slots (creatures ignore)")]
        [SerializeField] private List<CharacterData> _enemyCrewPool = new();

        [Header("Scene refs")]
        [SerializeField] private CombatRunner _runner;
        [SerializeField] private NavalCombatHUD _hud;

        [System.Serializable]
        public class NavalWaveEntry
        {
            public List<ShipData> Ships = new();
        }

        private void Start()
        {
            if (_allyShip == null || _runner == null || _hud == null)
            {
                Debug.LogError("[NavalBootstrap] Faltan referencias (allyShip/runner/hud)");
                return;
            }

            // --- Allied ship ---
            var ally = BuildShip(_allyShip, _allyCrew, isBoss: false);
            ally.EvaluateCrewSynergies();

            // --- Waves + per-enemy AI ---
            var enemyInputs = new Dictionary<ICombatant, ICombatInput>();
            var waves = new List<WaveConfig>();

            foreach (var waveEntry in _waves)
            {
                var wave = new WaveConfig { Enemies = new List<InitiativeEntry>() };
                int slot = 0;
                foreach (var shipData in waveEntry.Ships)
                {
                    if (shipData == null) continue;
                    bool isBoss = shipData.Tier == EnemyTier.Jefe;
                    var crew = shipData.RoleSlots != null && shipData.RoleSlots.Count > 0
                        ? _enemyCrewPool : null;
                    var enemy = BuildShip(shipData, crew, isBoss);
                    if (enemy.Crew.Count > 0)
                        enemy.EvaluateCrewSynergies();

                    wave.Enemies.Add(new InitiativeEntry(enemy, CombatTeam.Enemy, slot++));
                    enemyInputs[enemy] = NavalEnemyAI.FromShipData(shipData);
                }
                if (wave.Enemies.Count > 0)
                    waves.Add(wave);
            }

            if (waves.Count == 0)
            {
                Debug.LogError("[NavalBootstrap] Sin oleadas configuradas");
                return;
            }

            var config = new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                {
                    new InitiativeEntry(ally, CombatTeam.Ally, 0)
                },
                Waves = waves,
                CaptainIndex = 0
            };

            // --- Input + HUD wiring ---
            var playerInput = new NavalPlayerCombatInput();
            _hud.Bind(playerInput);

            _runner.StartBattle(config, playerInput,
                resolver: new NavalTurnResolver(),
                enemyInputSelector: c => enemyInputs.TryGetValue(c, out var ai) ? ai : null);

            var firstWaveEnemies = new List<ICombatant>();
            foreach (var entry in waves[0].Enemies)
                firstWaveEnemies.Add(entry.Combatant);
            _hud.BuildBattlefield(ally, firstWaveEnemies);
        }

        /// <summary>
        /// Builds a ShipCombatant assigning the given units to its role slots
        /// in slot order (cycled if there are fewer units than slots).
        /// </summary>
        private static ShipCombatant BuildShip(ShipData data, List<CharacterData> units, bool isBoss)
        {
            Dictionary<int, CharacterData> crewBySlot = null;
            if (units != null && units.Count > 0 && data.RoleSlots != null)
            {
                crewBySlot = new Dictionary<int, CharacterData>();
                int i = 0;
                foreach (var slot in data.RoleSlots)
                {
                    if (slot.IsGuestSlot) continue; // guest as 2nd captain is S4-08
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
