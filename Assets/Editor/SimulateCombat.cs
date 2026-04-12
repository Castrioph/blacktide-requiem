using UnityEngine;
using BlacktideRequiem.UI.Combat;
using BlacktideRequiem.Runtime.Combat;

public static class SimulateCombat
{
    public static string Execute()
    {
        if (!Application.isPlaying)
            return "ERROR: Must be in Play mode";

        var runnerGo = GameObject.Find("CombatSystem");
        if (runnerGo == null) return "ERROR: CombatSystem GO not found";

        var runner = runnerGo.GetComponent<CombatRunner>();
        if (runner == null) return "ERROR: CombatRunner component not found";

        var result = $"Runner found. Manager={runner.Manager != null}. ";

        if (runner.Manager == null)
            return result + "Manager is null — battle may not have started";

        var phase = runner.Manager.Phase;
        result += $"Phase={phase}. ";

        var hudGo = GameObject.Find("CombatUI");
        var hud = hudGo?.GetComponent<CombatHUDCanvas>();
        if (hud == null) return result + "ERROR: CombatHUDCanvas not found";

        var field = typeof(CombatHUDCanvas).GetField("_playerInput",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerInput = field?.GetValue(hud) as PlayerCombatInput;
        if (playerInput == null) return result + "ERROR: PlayerCombatInput null";

        result += $"IsWaiting={playerInput.IsWaitingForInput}. ";

        if (!playerInput.IsWaitingForInput)
            return result + "Not waiting for input — enemy turn or transition";

        // Find first alive enemy
        BlacktideRequiem.Core.Combat.CombatantState target = null;
        foreach (var enemy in runner.Manager.Enemies)
        {
            if (!enemy.IsKO) { target = enemy; break; }
        }
        if (target == null) return result + "No alive enemies";

        var hpBefore = target.CurrentHP;
        playerInput.SubmitAttack(target);

        return result + $"Attacked {target.Template.DisplayName}! HP: {hpBefore} -> {target.CurrentHP}";
    }
}
