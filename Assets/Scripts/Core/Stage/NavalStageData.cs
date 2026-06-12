using System;
using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Stage
{
    /// <summary>
    /// Stage de combate naval. Subclase de StageData para aparecer en el
    /// StageRegistry/StageSelect sin cambios en esa UI; el flujo detecta el
    /// tipo y carga la escena naval (GameFlowManager.LoadCombat).
    /// Las oleadas terrestres (Waves) quedan vacías; las navales van en
    /// NavalWaves. See Combate Naval GDD §7 y ADR-004.
    /// </summary>
    [CreateAssetMenu(fileName = "stage_naval_", menuName = "Blacktide/Naval Stage Data")]
    public class NavalStageData : StageData
    {
        [Header("Naval")]

        [Tooltip("Barco del jugador en esta misión (demo: sin sistema de flota)")]
        public ShipData PlayerShip;

        [Tooltip("Oleadas de barcos/criaturas enemigas")]
        public List<NavalWaveDefinition> NavalWaves = new();

        [Tooltip("Units cicladas en los RoleSlots de cada barco enemigo (criaturas sin slots las ignoran)")]
        public List<CharacterData> EnemyCrewPool = new();
    }

    [Serializable]
    public class NavalWaveDefinition
    {
        public List<ShipData> Ships = new();
    }
}
