using System;
using System.Collections.Generic;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Stage
{
    public static class StageController
    {
        public static BattleConfig BuildBattleConfig(StageData stage, IReadOnlyList<CharacterData> allies)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (allies == null) throw new ArgumentNullException(nameof(allies));

            var allyEntries = new List<InitiativeEntry>(allies.Count);
            for (int i = 0; i < allies.Count; i++)
            {
                var state = new CombatantState(allies[i], allies[i].BaseStats, 1);
                allyEntries.Add(new InitiativeEntry(state, CombatTeam.Ally, i));
            }

            var waves = new List<WaveConfig>(stage.Waves.Count);
            foreach (var waveDef in stage.Waves)
            {
                var enemies = new List<InitiativeEntry>(waveDef.Enemies.Count);
                foreach (var slot in waveDef.Enemies)
                {
                    var state = new CombatantState(slot.Enemy, slot.Enemy.BaseStats, 1);
                    enemies.Add(new InitiativeEntry(state, CombatTeam.Enemy, slot.SlotIndex));
                }
                waves.Add(new WaveConfig
                {
                    Enemies = enemies,
                    EnemyCaptainIndex = waveDef.EnemyCaptainIndex
                });
            }

            return new BattleConfig { Allies = allyEntries, Waves = waves };
        }
    }
}
