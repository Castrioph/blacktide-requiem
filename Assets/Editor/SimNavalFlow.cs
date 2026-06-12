using UnityEngine;
using UnityEngine.UI;
using BlacktideRequiem.Core.Events;
using BlacktideRequiem.Core.Stage;
using BlacktideRequiem.Runtime.Flow;
using BlacktideRequiem.UI.StageSelect;

/// <summary>
/// Play-mode helpers para verificar el flujo naval S4-07 vía Coplay:
/// StageSelect (elegir stage naval) → Launch → [SimulateCombat existente] →
/// NavalCombat → victoria forzada → Results con rewards.
/// </summary>
public static class SimNavalFlow
{
    /// <summary>Selecciona la card del stage naval en StageSelect.</summary>
    public static string ClickNavalStage()
    {
        var entries = Object.FindObjectsByType<StageEntryUI>(FindObjectsSortMode.None);
        foreach (var entry in entries)
        {
            if (entry.BoundStage is not NavalStageData) continue;
            var btn = entry.GetComponentInChildren<Button>();
            if (btn == null) return "Card naval sin Button";
            btn.onClick.Invoke();
            return $"Seleccionado: {entry.BoundStage.DisplayName}";
        }
        return $"Sin card naval entre {entries.Length} entries";
    }

    /// <summary>Pulsa el botón Lanzar de StageSelect → TeamSelect.</summary>
    public static string ClickLaunch()
    {
        var controller = Object.FindFirstObjectByType<StageSelectController>();
        if (controller == null) return "StageSelectController no encontrado";

        // El botón Launch está serializado privado; lo buscamos por jerarquía
        foreach (var btn in controller.GetComponentsInChildren<Button>())
        {
            if (btn.name.ToLowerInvariant().Contains("launch") ||
                btn.name.ToLowerInvariant().Contains("lanzar"))
            {
                if (!btn.interactable) return $"{btn.name} disabled (¿stage sin seleccionar?)";
                btn.onClick.Invoke();
                return $"Pulsado {btn.name}";
            }
        }
        return "Botón Launch no encontrado";
    }

    /// <summary>
    /// Fuerza el fin de batalla en victoria y navega a Results — verifica
    /// payout de RewardDispatcher y la línea de recompensas sin jugar
    /// las 3 oleadas completas.
    /// </summary>
    public static string ForceVictoryAndResults()
    {
        var gfm = Object.FindFirstObjectByType<GameFlowManager>();
        if (gfm == null) return "GameFlowManager no encontrado";

        GameEvents.PublishBattleEnd(new BattleEndEvent
        {
            Result = BattleResult.Victory,
            RoundsElapsed = 7
        });
        gfm.LoadResults();
        return "Victoria forzada → Results";
    }
}
