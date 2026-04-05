// =============================================================================
// S1-07: LAND COMBAT PROTOTYPE — Throwaway code
// =============================================================================
// Validates the core combat loop: turn order, actions, damage, victory/defeat.
//
// SETUP:
//   1. Create an empty scene (File > New Scene > Basic)
//   2. Create an empty GameObject (right-click Hierarchy > Create Empty)
//   3. Add Component > Prototypes > Combat Prototype v1
//   4. Press Play
//
// Uses OnGUI (IMGUI) for zero scene setup. NOT production UI.
// =============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Prototypes.CombatV1
{
    /// <summary>
    /// Prototype ability data. Production will use ScriptableObjects.
    /// </summary>
    public class ProtoAbility
    {
        public string Name;
        public float Power;
        public Element Element;
        public bool IsPhysical;
    }

    /// <summary>
    /// S1-07: Land Combat Prototype.
    /// Single self-contained MonoBehaviour. Attach to any GameObject and Play.
    /// </summary>
    [AddComponentMenu("Prototypes/Combat Prototype v1")]
    public class CombatPrototype : MonoBehaviour
    {
        // --- Constants (from GDDs) ---
        const float GUARD_REDUCTION = 0.50f;
        const float NORMAL_ATTACK_POWER = 1.0f;

        // --- Battle Data ---
        InitiativeBar _bar = new();
        List<InitiativeEntry> _allEntries = new();
        List<InitiativeEntry> _allyEntries = new();
        List<InitiativeEntry> _enemyEntries = new();
        Dictionary<CombatantState, ProtoAbility> _abilities = new();
        Dictionary<CombatantState, bool> _physicalType = new();
        HashSet<CombatantState> _guarding = new();

        // --- State Machine ---
        enum Phase { Starting, Playing, WaitAction, WaitTarget, EnemyTurn, Victory, Defeat }
        Phase _phase = Phase.Starting;
        InitiativeEntry _currentActor;

        enum ActionType { None, Attack, Ability, Guard, Pass }
        ActionType _chosenAction;
        CombatantState _chosenTarget;

        // --- Battle Log ---
        List<string> _logLines = new();
        Vector2 _logScroll;

        // --- IMGUI Styles (lazy init) ---
        GUIStyle _titleStyle, _labelStyle, _smallLabel, _logStyle, _buttonStyle;
        bool _stylesReady;

        // ====================================================================
        // LIFECYCLE
        // ====================================================================

        void Start()
        {
            SetupCombatants();
            StartCoroutine(BattleLoop());
        }

        // ====================================================================
        // SETUP — 3 allies vs 2 enemies with different elements/stats
        // ====================================================================

        void SetupCombatants()
        {
            // --- Allies ---
            var elena = MakeCombatant("elena", "Elena Storm", Element.Tormenta,
                hp: 120, mp: 50, atk: 45, def: 25, mst: 30, spr: 20, spd: 70, cri: 15);
            _abilities[elena] = new ProtoAbility
                { Name = "Rayo", Power = 1.5f, Element = Element.Tormenta, IsPhysical = false };
            _physicalType[elena] = true;

            var bones = MakeCombatant("bones", "Bones McCoy", Element.Maldicion,
                hp: 150, mp: 60, atk: 25, def: 40, mst: 35, spr: 35, spd: 50, cri: 10);
            _abilities[bones] = new ProtoAbility
                { Name = "Maldicion Oscura", Power = 1.3f, Element = Element.Maldicion, IsPhysical = false };
            _physicalType[bones] = false;

            var molly = MakeCombatant("molly", "Red Molly", Element.Polvora,
                hp: 90, mp: 40, atk: 55, def: 15, mst: 20, spr: 15, spd: 65, cri: 20);
            _abilities[molly] = new ProtoAbility
                { Name = "Canonazo", Power = 2.0f, Element = Element.Polvora, IsPhysical = true };
            _physicalType[molly] = true;

            _allyEntries.Add(new InitiativeEntry(elena, CombatTeam.Ally, 0));
            _allyEntries.Add(new InitiativeEntry(bones, CombatTeam.Ally, 1));
            _allyEntries.Add(new InitiativeEntry(molly, CombatTeam.Ally, 2));

            // --- Enemies ---
            var esqueleto = MakeCombatant("esqueleto", "Pirata Esqueleto", Element.Acero,
                hp: 100, mp: 30, atk: 35, def: 30, mst: 20, spr: 25, spd: 55, cri: 10);
            _abilities[esqueleto] = new ProtoAbility
                { Name = "Estocada", Power = 1.3f, Element = Element.Acero, IsPhysical = true };
            _physicalType[esqueleto] = true;

            var fantasma = MakeCombatant("fantasma", "Capitan Fantasma", Element.Sombra,
                hp: 180, mp: 50, atk: 40, def: 35, mst: 45, spr: 30, spd: 60, cri: 15);
            fantasma.IsBoss = true;
            _abilities[fantasma] = new ProtoAbility
                { Name = "Sombra Cortante", Power = 1.8f, Element = Element.Sombra, IsPhysical = false };
            _physicalType[fantasma] = false;

            _enemyEntries.Add(new InitiativeEntry(esqueleto, CombatTeam.Enemy, 0));
            _enemyEntries.Add(new InitiativeEntry(fantasma, CombatTeam.Enemy, 1));

            _allEntries.AddRange(_allyEntries);
            _allEntries.AddRange(_enemyEntries);
        }

        CombatantState MakeCombatant(string id, string displayName, Element element,
            float hp, float mp, float atk, float def, float mst, float spr, float spd, float cri)
        {
            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.Id = id;
            data.DisplayName = displayName;
            data.Element = element;
            data.BaseStats = new StatBlock
                { HP = hp, MP = mp, ATK = atk, DEF = def, MST = mst, SPR = spr, SPD = spd };
            data.SecondaryStats = new SecondaryStatBlock { CRI = cri, LCK = 0 };

            var stats = new StatBlock
                { HP = hp, MP = mp, ATK = atk, DEF = def, MST = mst, SPR = spr, SPD = spd };
            return new CombatantState(data, stats, 1);
        }

        // ====================================================================
        // BATTLE LOOP (Coroutine-driven)
        // ====================================================================

        IEnumerator BattleLoop()
        {
            yield return new WaitForSeconds(0.3f);
            Log("<b>=== BATTLE START ===</b>");
            Log("Allies: Elena Storm, Bones McCoy, Red Molly");
            Log("Enemies: Pirata Esqueleto, Capitan Fantasma");
            _phase = Phase.Playing;

            while (_phase != Phase.Victory && _phase != Phase.Defeat)
            {
                _bar.BeginRound(_allEntries);
                Log($"\n<b>--- Round {_bar.RoundNumber} ---</b>");
                LogTurnOrder();

                while (!_bar.IsRoundOver && _phase != Phase.Victory && _phase != Phase.Defeat)
                {
                    var entry = _bar.AdvanceTurn();
                    if (entry == null) break;

                    _currentActor = entry;

                    // Remove guard at start of this unit's turn
                    _guarding.Remove(entry.Combatant);

                    if (entry.Team == CombatTeam.Ally)
                        yield return StartCoroutine(PlayerTurn(entry));
                    else
                        yield return StartCoroutine(EnemyTurn(entry));

                    _bar.CompleteCurrentTurn();
                    yield return new WaitForSeconds(0.2f);
                }
            }

            _currentActor = null;
        }

        IEnumerator PlayerTurn(InitiativeEntry entry)
        {
            string name = entry.Combatant.Template.DisplayName;
            Log($"<color=#6699FF>>> {name}'s turn</color>");

            // Loop allows cancel from target selection back to action selection
            while (true)
            {
                _chosenAction = ActionType.None;
                _chosenTarget = null;
                _phase = Phase.WaitAction;

                while (_chosenAction == ActionType.None)
                    yield return null;

                // --- Guard ---
                if (_chosenAction == ActionType.Guard)
                {
                    _guarding.Add(entry.Combatant);
                    Log($"  {name} takes a defensive stance! <color=#00CCCC>[GUARD]</color>");
                    break;
                }

                // --- Pass ---
                if (_chosenAction == ActionType.Pass)
                {
                    Log($"  {name} passes.");
                    break;
                }

                // --- Need target selection ---
                _phase = Phase.WaitTarget;
                _chosenTarget = null;

                while (_chosenTarget == null && _phase == Phase.WaitTarget)
                    yield return null;

                if (_chosenTarget == null)
                    continue; // Cancelled — back to action selection

                bool useAbility = _chosenAction == ActionType.Ability;
                ResolveAttack(entry.Combatant, _chosenTarget, useAbility);
                break;
            }

            if (_phase != Phase.Victory && _phase != Phase.Defeat)
                _phase = Phase.Playing;
        }

        IEnumerator EnemyTurn(InitiativeEntry entry)
        {
            _phase = Phase.EnemyTurn;
            yield return new WaitForSeconds(0.4f);

            string name = entry.Combatant.Template.DisplayName;
            Log($"<color=#FF6666>>> {name}'s turn</color>");

            var aliveAllies = GetAlive(_allyEntries);
            if (aliveAllies.Count == 0) yield break;

            // Simple AI: 40% ability, 60% basic attack
            bool useAbility = Random.value < 0.4f;
            var target = aliveAllies[Random.Range(0, aliveAllies.Count)];

            ResolveAttack(entry.Combatant, target, useAbility);

            yield return new WaitForSeconds(0.3f);
            if (_phase != Phase.Victory && _phase != Phase.Defeat)
                _phase = Phase.Playing;
        }

        // ====================================================================
        // ACTION RESOLUTION
        // ====================================================================

        void ResolveAttack(CombatantState attacker, CombatantState target, bool useAbility)
        {
            string atkName = attacker.Template.DisplayName;
            string defName = target.Template.DisplayName;

            float power;
            Element element;
            bool isPhysical;
            string actionName;

            if (useAbility && _abilities.TryGetValue(attacker, out var ability))
            {
                power = ability.Power;
                element = ability.Element;
                isPhysical = ability.IsPhysical;
                actionName = ability.Name;
            }
            else
            {
                power = NORMAL_ATTACK_POWER;
                element = Element.Neutral;
                isPhysical = true;
                if (_physicalType.TryGetValue(attacker, out bool phys))
                    isPhysical = phys;
                actionName = "Attack";
            }

            // Get effective stats
            float effAtk = isPhysical
                ? attacker.GetEffectiveStat(StatType.ATK)
                : attacker.GetEffectiveStat(StatType.MST);
            float effDef = isPhysical
                ? target.GetEffectiveStat(StatType.DEF)
                : target.GetEffectiveStat(StatType.SPR);

            float cri = attacker.Template.SecondaryStats.CRI;

            var result = DamageCalculator.Calculate(
                effAtk, effDef, power, element, target.Template.Element,
                cri, isPhysical);

            if (result.IsMiss)
            {
                Log($"  {atkName} uses {actionName} on {defName} -- <color=#999999>MISS!</color>");
                return;
            }

            int damage = result.FinalDamage;

            // Guard reduction
            bool guarded = _guarding.Contains(target);
            if (guarded)
                damage = Mathf.Max(Mathf.FloorToInt(damage * GUARD_REDUCTION), 1);

            int actual = target.ApplyDamage(damage);

            // Build log line
            string elemTag = result.ElementMod > 1f ? " <color=#FFD700>EFFECTIVE!</color>"
                : result.ElementMod < 1f ? " <color=#888888>(resisted)</color>" : "";
            string critTag = result.IsCritical ? " <color=#FF4444>CRIT!</color>" : "";
            string guardTag = guarded ? " <color=#00CCCC>[GUARDED]</color>" : "";

            Log($"  {atkName} uses <b>{actionName}</b> on {defName} -> {actual} dmg{elemTag}{critTag}{guardTag}");

            // Death check
            if (target.IsKO)
            {
                Log($"  <color=#FF0000>{defName} has been defeated!</color>");
                _bar.RemoveDead(target);
                _guarding.Remove(target);
                CheckBattleEnd();
            }
        }

        // ====================================================================
        // VICTORY / DEFEAT
        // ====================================================================

        void CheckBattleEnd()
        {
            if (GetAlive(_enemyEntries).Count == 0)
            {
                _phase = Phase.Victory;
                Log("\n<color=#FFD700><b>*** VICTORY! ***</b></color>");
                Log("All enemies defeated!");
            }
            else if (GetAlive(_allyEntries).Count == 0)
            {
                _phase = Phase.Defeat;
                Log("\n<color=#FF0000><b>*** DEFEAT ***</b></color>");
                Log("All allies have fallen...");
            }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        List<CombatantState> GetAlive(List<InitiativeEntry> entries)
        {
            var result = new List<CombatantState>();
            foreach (var e in entries)
                if (!e.Combatant.IsKO)
                    result.Add(e.Combatant);
            return result;
        }

        void Log(string msg)
        {
            _logLines.Add(msg);
            _logScroll.y = float.MaxValue;
        }

        void LogTurnOrder()
        {
            var queued = _bar.GetQueuedEntries();
            var names = new List<string>();
            foreach (var e in queued)
            {
                string color = e.Team == CombatTeam.Ally ? "#6699FF" : "#FF6666";
                string boss = e.Combatant.IsBoss ? "*" : "";
                names.Add($"<color={color}>{boss}{e.Combatant.Template.DisplayName}</color>");
            }
            Log("Order: " + string.Join(" > ", names));
        }

        void Restart()
        {
            StopAllCoroutines();
            _allEntries.Clear();
            _allyEntries.Clear();
            _enemyEntries.Clear();
            _abilities.Clear();
            _physicalType.Clear();
            _guarding.Clear();
            _logLines.Clear();
            _bar = new InitiativeBar();
            _currentActor = null;
            _phase = Phase.Starting;
            SetupCombatants();
            StartCoroutine(BattleLoop());
        }

        string ElementColor(Element e) => e switch
        {
            Element.Polvora => "#FF6600",
            Element.Tormenta => "#00AAFF",
            Element.Maldicion => "#AA00FF",
            Element.Bestia => "#00CC00",
            Element.Acero => "#AAAACC",
            Element.Luz => "#FFFF00",
            Element.Sombra => "#8844AA",
            _ => "#CCCCCC"
        };

        // ====================================================================
        // IMGUI RENDERING
        // ====================================================================

        void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;
            _titleStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            _smallLabel = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
            _logStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = true };
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, richText = true };
        }

        void OnGUI()
        {
            EnsureStyles();

            float w = Screen.width;
            float h = Screen.height;
            float pad = 10f;

            // Dark background
            GUI.color = new Color(0.08f, 0.08f, 0.12f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Header
            GUILayout.BeginArea(new Rect(pad, pad, w - pad * 2, 30));
            GUILayout.Label($"<color=#FFD700>COMBAT PROTOTYPE</color>  —  Round {_bar.RoundNumber}", _titleStyle);
            GUILayout.EndArea();

            // Initiative bar
            DrawInitiativeBar(new Rect(pad, 45, w - pad * 2, 22));

            // Teams
            float teamY = 75;
            float teamH = h * 0.3f;
            DrawTeamPanel(new Rect(pad, teamY, w * 0.48f, teamH),
                _allyEntries, "ALLIES", "#6699FF");
            DrawTeamPanel(new Rect(w * 0.52f, teamY, w * 0.48f - pad, teamH),
                _enemyEntries, "ENEMIES", "#FF6666");

            // Battle log
            float logY = teamY + teamH + 8;
            float actionH = 110;
            float logH = h - logY - actionH - 16;
            DrawBattleLog(new Rect(pad, logY, w - pad * 2, logH));

            // Action panel
            DrawActionPanel(new Rect(pad, h - actionH - pad, w - pad * 2, actionH));
        }

        void DrawInitiativeBar(Rect area)
        {
            GUI.color = new Color(0.12f, 0.12f, 0.18f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(area.x + 6, area.y + 2, area.width - 12, area.height - 4));
            GUILayout.BeginHorizontal();

            GUILayout.Label("<color=#888888>Turn:</color> ", _smallLabel, GUILayout.ExpandWidth(false));

            var entries = _bar.GetQueuedEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                bool active = e.State == TurnState.Active;
                string color = active ? "#FFFF00"
                    : e.Team == CombatTeam.Ally ? "#6699FF" : "#FF6666";
                string arrow = active ? "> " : "";
                string boss = e.Combatant.IsBoss ? "*" : "";

                GUILayout.Label(
                    $"<color={color}>{arrow}{boss}{e.Combatant.Template.DisplayName}</color>",
                    _smallLabel, GUILayout.ExpandWidth(false));

                if (i < entries.Count - 1)
                    GUILayout.Label("<color=#555555> > </color>", _smallLabel, GUILayout.ExpandWidth(false));
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawTeamPanel(Rect area, List<InitiativeEntry> team, string title, string titleColor)
        {
            GUI.color = new Color(0.1f, 0.1f, 0.15f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(area.x + 8, area.y + 4, area.width - 16, area.height - 8));
            GUILayout.Label($"<color={titleColor}><b>{title}</b></color>", _labelStyle);
            GUILayout.Space(4);

            foreach (var entry in team)
            {
                var c = entry.Combatant;
                bool active = _currentActor != null && _currentActor.Combatant == c;
                bool dead = c.IsKO;
                bool guarding = _guarding.Contains(c);

                string marker = active ? "<color=#FFFF00>> </color>" : "  ";
                string nameColor = dead ? "#555555" : active ? "#FFFF00" : "#FFFFFF";
                string status = dead ? " <color=#FF0000>[KO]</color>"
                    : guarding ? " <color=#00CCCC>[GUARD]</color>" : "";
                string bossTag = c.IsBoss ? " <color=#FFD700>*</color>" : "";

                GUILayout.Label(
                    $"{marker}<color={nameColor}>{c.Template.DisplayName}</color>{bossTag}{status}",
                    _labelStyle);

                if (!dead)
                {
                    // HP bar (drawn manually)
                    Rect hpRect = GUILayoutUtility.GetRect(area.width - 40, 10);
                    DrawHPBar(hpRect, c.CurrentHP, c.MaxHP);

                    string elemColor = ElementColor(c.Template.Element);
                    GUILayout.Label(
                        $"    HP: {c.CurrentHP}/{c.MaxHP}  |  " +
                        $"<color={elemColor}>{c.Template.Element}</color>  |  SPD {c.BaseStats.SPD}",
                        _smallLabel);
                }

                GUILayout.Space(6);
            }

            GUILayout.EndArea();
        }

        void DrawHPBar(Rect rect, int current, int max)
        {
            float ratio = Mathf.Clamp01((float)current / max);
            float barX = rect.x + 24;
            float barW = Mathf.Min(rect.width - 48, 200);

            // Background
            GUI.color = new Color(0.2f, 0.2f, 0.25f);
            GUI.DrawTexture(new Rect(barX, rect.y, barW, rect.height), Texture2D.whiteTexture);

            // Fill
            Color barColor = ratio > 0.5f ? new Color(0.2f, 0.8f, 0.2f)
                : ratio > 0.25f ? new Color(0.9f, 0.8f, 0.1f)
                : new Color(0.9f, 0.2f, 0.2f);
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barX, rect.y, barW * ratio, rect.height), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        void DrawBattleLog(Rect area)
        {
            GUI.color = new Color(0.06f, 0.06f, 0.09f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;

            Rect inner = new Rect(area.x + 8, area.y + 4, area.width - 16, area.height - 8);

            string fullLog = string.Join("\n", _logLines);

            GUILayout.BeginArea(inner);
            _logScroll = GUILayout.BeginScrollView(_logScroll);
            GUILayout.Label(fullLog, _logStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawActionPanel(Rect area)
        {
            GUI.color = new Color(0.12f, 0.12f, 0.18f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(area.x + 8, area.y + 6, area.width - 16, area.height - 12));

            if (_phase == Phase.Victory || _phase == Phase.Defeat)
            {
                DrawEndScreen();
            }
            else if (_phase == Phase.WaitAction && _currentActor != null)
            {
                DrawActionButtons();
            }
            else if (_phase == Phase.WaitTarget)
            {
                DrawTargetSelection();
            }
            else
            {
                string msg = _phase == Phase.EnemyTurn ? "Enemy is thinking..." : "...";
                GUILayout.Label($"<color=#888888>{msg}</color>", _labelStyle);
            }

            GUILayout.EndArea();
        }

        void DrawActionButtons()
        {
            var actor = _currentActor.Combatant;
            string name = actor.Template.DisplayName;
            GUILayout.Label($"<color=#FFFF00>{name}</color> — Choose action:", _labelStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Attack", _buttonStyle, GUILayout.Height(50)))
                _chosenAction = ActionType.Attack;

            string abilityLabel = "Ability";
            if (_abilities.TryGetValue(actor, out var ab))
            {
                string eColor = ElementColor(ab.Element);
                abilityLabel = $"<color={eColor}>{ab.Name}</color>";
            }
            if (GUILayout.Button(abilityLabel, _buttonStyle, GUILayout.Height(50)))
                _chosenAction = ActionType.Ability;

            if (GUILayout.Button("Guard", _buttonStyle, GUILayout.Height(50)))
                _chosenAction = ActionType.Guard;

            if (GUILayout.Button("Pass", _buttonStyle, GUILayout.Height(50)))
                _chosenAction = ActionType.Pass;

            GUILayout.EndHorizontal();
        }

        void DrawTargetSelection()
        {
            string actionName = _chosenAction == ActionType.Ability
                && _abilities.TryGetValue(_currentActor.Combatant, out var ab)
                ? ab.Name : "Attack";

            GUILayout.Label($"<color=#FFFF00>{actionName}</color> — Select target:", _labelStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();

            var aliveEnemies = GetAlive(_enemyEntries);
            foreach (var enemy in aliveEnemies)
            {
                string eColor = ElementColor(enemy.Template.Element);
                string label = $"{enemy.Template.DisplayName}\n<color={eColor}>{enemy.Template.Element}</color> HP:{enemy.CurrentHP}/{enemy.MaxHP}";
                if (GUILayout.Button(label, _buttonStyle, GUILayout.Height(50), GUILayout.MinWidth(150)))
                    _chosenTarget = enemy;
            }

            GUILayout.Space(30);

            if (GUILayout.Button("Cancel", _buttonStyle, GUILayout.Height(50), GUILayout.Width(100)))
                _phase = Phase.WaitAction; // Coroutine detects this and loops back

            GUILayout.EndHorizontal();
        }

        void DrawEndScreen()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            string color = _phase == Phase.Victory ? "#FFD700" : "#FF4444";
            string msg = _phase == Phase.Victory ? "VICTORY!" : "DEFEAT";
            GUILayout.Label($"<color={color}><b>{msg}</b></color>", _titleStyle);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Restart Battle", _buttonStyle, GUILayout.Width(200), GUILayout.Height(40)))
                Restart();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
