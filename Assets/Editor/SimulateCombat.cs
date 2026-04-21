using UnityEngine;
using UnityEditor;
using BlacktideRequiem.Runtime.Flow;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Team;

public class SimulateCombat
{
    public static void Execute()
    {
        var gfm = Object.FindFirstObjectByType<GameFlowManager>();
        if (gfm == null) { Debug.LogError("[Sim] GFM not found"); return; }

        var c1 = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/elena_tempestad.asset");
        var c2 = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/kael_polvora.asset");
        var c3 = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/mirra_mareamadre.asset");

        if (c1 == null || c2 == null || c3 == null)
        { Debug.LogError("[Sim] Character assets not found"); return; }

        var team = new TeamComposition(new[] { c1, c2, c3 });
        team.SelectCharacter(0, c1);
        team.SelectCharacter(1, c2);
        team.SelectCharacter(2, c3);

        gfm.SelectedTeam = team;
        Debug.Log($"[Sim] Team set, loading Combat");
        gfm.LoadCombat();
    }
}
