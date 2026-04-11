using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using TurnBasedGame.Unit;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.SpellCard;

/// <summary>
/// Editor tool cho phép test nhanh gameplay trong Play mode:
/// - Spawn unit (Player 1, đọc từ PlayerDataSO.SelectedDeck)
/// - Apply buff/debuff lên unit đang chọn
/// - Chỉnh HP / MP nhanh
/// - Force end turn
/// </summary>
public class GameplayTestTool : EditorWindow
{
    // ── Tab ──
    private enum Tab { Spawn, Buff, Stats, Actions }
    private Tab _currentTab = Tab.Spawn;
    private static readonly string[] TabNames = { "Spawn", "Buff/Debuff", "Stats", "Actions" };

    // ── Spawn ──
    private PlayerDataSO _playerData;
    private int _selectedUnitIndex;
    private static readonly string AssetPath = "Assets/Scripts/Data/PlayerData/PlayerDataSO.asset";

    // ── Buff ──
    private StatusEffectType _selectedEffect = StatusEffectType.DamageBuff;
    private int _effectValue = 20;
    private int _effectDuration = 3;
    private PlayerID _effectSource = PlayerID.Player1;

    // ── Stats ──
    private int _healAmount = 50;
    private int _damageAmount = 30;
    private int _mpAmount = 5;

    // ── Scroll ──
    private Vector2 _scrollPos;

    // ── Styles (lazy init) ──
    private GUIStyle _headerStyle;
    private GUIStyle _sectionStyle;
    private bool _stylesInitialized;

    [MenuItem("Horus/Gameplay Test Tool %&t")]
    public static void ShowWindow()
    {
        var window = GetWindow<GameplayTestTool>("Gameplay Test");
        window.minSize = new Vector2(320, 400);
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        _sectionStyle = new GUIStyle("box") { padding = new RectOffset(8, 8, 6, 6) };
        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();

        _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, TabNames);
        EditorGUILayout.Space(4);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Tool chỉ hoạt động trong Play Mode.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        switch (_currentTab)
        {
            case Tab.Spawn:   DrawSpawnTab();   break;
            case Tab.Buff:    DrawBuffTab();    break;
            case Tab.Stats:   DrawStatsTab();   break;
            case Tab.Actions: DrawActionsTab(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    #region ── Spawn Tab ──

    private void DrawSpawnTab()
    {
        EditorGUILayout.LabelField("Spawn Unit  (Player 1)", _headerStyle);

        // Auto-load PlayerDataSO
        if (_playerData == null)
            _playerData = AssetDatabase.LoadAssetAtPath<PlayerDataSO>(AssetPath);

        EditorGUILayout.BeginVertical(_sectionStyle);
        _playerData = (PlayerDataSO)EditorGUILayout.ObjectField("Player Data SO", _playerData, typeof(PlayerDataSO), false);

        if (_playerData == null)
        {
            EditorGUILayout.HelpBox("Không tìm thấy PlayerDataSO. Kéo asset vào ô trên.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            DrawUnitList();
            return;
        }

        var deck = _playerData.SelectedDeck;
        if (deck == null || deck.Count == 0)
        {
            EditorGUILayout.HelpBox("SelectedDeck trống. Thêm unit vào deck trong PlayerDataSO.", MessageType.Info);
            EditorGUILayout.EndVertical();
            DrawUnitList();
            return;
        }

        // Dropdown chọn unit từ deck
        var names = deck.Select(u => u != null ? u.name : "(null)").ToArray();
        _selectedUnitIndex = Mathf.Clamp(_selectedUnitIndex, 0, deck.Count - 1);
        _selectedUnitIndex = EditorGUILayout.Popup("Unit (từ Deck)", _selectedUnitIndex, names);

        var selectedPrefab = deck[_selectedUnitIndex];
        if (selectedPrefab != null && selectedPrefab.UnitData != null)
            EditorGUILayout.LabelField($"  Cost: {selectedPrefab.UnitData.spawnCost} MP", EditorStyles.miniLabel);

        if (GUILayout.Button("Spawn → Player 1", GUILayout.Height(28)))
            DoSpawn(selectedPrefab);

        EditorGUILayout.EndVertical();

        // Spawn all buttons
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(_sectionStyle);
        EditorGUILayout.LabelField("Spawn toàn bộ deck (P1)", EditorStyles.boldLabel);
        if (GUILayout.Button("Spawn All", GUILayout.Height(24)))
        {
            foreach (var u in deck)
                if (u != null) DoSpawn(u);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
        DrawUnitList();
    }

    private void DoSpawn(UnitController prefab)
    {
        if (prefab == null) return;
        var spawner = UnitSpawner.Instance;
        if (spawner == null) { Debug.LogWarning("[TestTool] UnitSpawner not found."); return; }

        bool ok = spawner.SpawnUnit(prefab, PlayerID.Player1);
        if (!ok) Debug.LogWarning($"[TestTool] Spawn {prefab.name} failed — no available spawn point or MP.");
    }

    private void DrawUnitList()
    {
        var spawner = UnitSpawner.Instance;
        if (spawner == null) return;

        foreach (PlayerID pid in Enum.GetValues(typeof(PlayerID)))
        {
            var units = spawner.GetPlayerUnits(pid);
            if (units == null || units.Count == 0) continue;

            EditorGUILayout.LabelField($"{pid} Units ({units.Count})", EditorStyles.boldLabel);
            foreach (var u in units)
            {
                if (u == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {u.UnitData.unitName}  HP:{u.GetCurrentHealth()}", GUILayout.Width(200));
                if (GUILayout.Button("Select", GUILayout.Width(55)))
                    Selection.activeGameObject = u.gameObject;
                if (GUILayout.Button("Kill", GUILayout.Width(40)))
                    u.TakeDamage(9999);
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    #endregion

    #region ── Buff / Debuff Tab ──

    private void DrawBuffTab()
    {
        EditorGUILayout.LabelField("Apply Buff / Debuff", _headerStyle);

        var target = GetSelectedUnit();
        if (target == null)
        {
            EditorGUILayout.HelpBox("Chọn một Unit trong Hierarchy hoặc Scene để apply effect.", MessageType.Info);
        }

        EditorGUILayout.BeginVertical(_sectionStyle);
        _selectedEffect = (StatusEffectType)EditorGUILayout.EnumPopup("Effect Type", _selectedEffect);
        _effectValue = EditorGUILayout.IntSlider("Value", _effectValue, 1, 200);
        _effectDuration = EditorGUILayout.IntSlider("Duration (turns)", _effectDuration, 1, 10);
        _effectSource = (PlayerID)EditorGUILayout.EnumPopup("Source Player", _effectSource);

        GUI.enabled = target != null && _selectedEffect != StatusEffectType.None;
        if (GUILayout.Button($"Apply {_selectedEffect}", GUILayout.Height(28)))
            DoApplyEffect(target);
        GUI.enabled = true;
        EditorGUILayout.EndVertical();

        // Quick buttons
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Quick Apply", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (QuickBtn("Shield 20", target)) QuickApply(target, StatusEffectType.Shield, 20, 2);
        if (QuickBtn("DmgBuff 15", target)) QuickApply(target, StatusEffectType.DamageBuff, 15, 3);
        if (QuickBtn("Root 2t", target)) QuickApply(target, StatusEffectType.Root, 0, 2);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (QuickBtn("Burn 10", target)) QuickApply(target, StatusEffectType.Burn, 10, 3);
        if (QuickBtn("Poison 5", target)) QuickApply(target, StatusEffectType.Poison, 5, 3);
        if (QuickBtn("Stun 1t", target)) QuickApply(target, StatusEffectType.Stun, 0, 1);
        EditorGUILayout.EndHorizontal();

        // Active effects on selected unit
        DrawActiveEffects(target);
    }

    private bool QuickBtn(string label, UnitController target)
    {
        GUI.enabled = target != null;
        bool clicked = GUILayout.Button(label, GUILayout.Height(24));
        GUI.enabled = true;
        return clicked;
    }

    private void QuickApply(UnitController target, StatusEffectType type, int value, int duration)
    {
        if (target == null) return;
        var effect = new ActiveStatusEffect(type, value, duration, _effectSource);
        target.BuffHandler.AddEffect(effect);
    }

    private void DoApplyEffect(UnitController target)
    {
        if (target == null) return;
        var effect = new ActiveStatusEffect(_selectedEffect, _effectValue, _effectDuration, _effectSource);
        target.BuffHandler.AddEffect(effect);
        Debug.Log($"[TestTool] Applied {_selectedEffect} (val:{_effectValue}, dur:{_effectDuration}) to {target.name}");
    }

    private void DrawActiveEffects(UnitController target)
    {
        if (target == null || target.BuffHandler == null) return;

        var effects = target.BuffHandler.ActiveEffects;
        if (effects.Count == 0) return;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"Active Effects on {target.UnitData.unitName}", EditorStyles.boldLabel);

        foreach (var e in effects)
        {
            EditorGUILayout.BeginHorizontal();
            var color = e.Type.IsBuff() ? Color.green : Color.red;
            var prevColor = GUI.contentColor;
            GUI.contentColor = color;
            EditorGUILayout.LabelField($"  {e.Type}  val:{e.Value}  turns:{e.RemainingTurns}", GUILayout.Width(250));
            GUI.contentColor = prevColor;
            if (GUILayout.Button("✕", GUILayout.Width(24)))
                target.BuffHandler.RemoveByType(e.Type);
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Clear All Effects"))
            target.BuffHandler.ClearAll();
    }

    #endregion

    #region ── Stats Tab ──

    private void DrawStatsTab()
    {
        EditorGUILayout.LabelField("Modify Stats", _headerStyle);

        var target = GetSelectedUnit();
        if (target == null)
        {
            EditorGUILayout.HelpBox("Chọn một Unit để chỉnh stats.", MessageType.Info);
        }

        // HP section
        EditorGUILayout.BeginVertical(_sectionStyle);
        EditorGUILayout.LabelField("Health", EditorStyles.boldLabel);
        if (target != null)
            EditorGUILayout.LabelField($"  Current: {target.GetCurrentHealth()} / {target.UnitData.Health}");

        _healAmount = EditorGUILayout.IntSlider("Amount", _healAmount, 1, 500);
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = target != null;
        if (GUILayout.Button($"Heal +{_healAmount}"))
            target?.Heal(_healAmount);
        if (GUILayout.Button($"Damage -{_damageAmount}"))
            target?.TakeDamage(_damageAmount);
        if (GUILayout.Button("Full Heal"))
            target?.Heal(9999);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        _damageAmount = EditorGUILayout.IntSlider("Damage", _damageAmount, 1, 500);
        EditorGUILayout.EndVertical();

        // MP section
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(_sectionStyle);
        EditorGUILayout.LabelField("Mana Points", EditorStyles.boldLabel);
        DrawMPSection(PlayerID.Player1);
        DrawMPSection(PlayerID.Player2);
        EditorGUILayout.EndVertical();
    }

    private void DrawMPSection(PlayerID player)
    {
        var mpMgr = MPManager.Instance;
        if (mpMgr == null) return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"  {player}: {mpMgr.GetCurrentMP(player)} / {mpMgr.MaxMP}", GUILayout.Width(160));
        if (GUILayout.Button("+5", GUILayout.Width(35)))
            mpMgr.AddMP(player, 5);
        if (GUILayout.Button("+Max", GUILayout.Width(45)))
            mpMgr.AddMP(player, 999);
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region ── Actions Tab ──

    private void DrawActionsTab()
    {
        EditorGUILayout.LabelField("Game Actions", _headerStyle);

        // Turn control
        EditorGUILayout.BeginVertical(_sectionStyle);
        EditorGUILayout.LabelField("Turn Control", EditorStyles.boldLabel);

        var mediator = GameMediator.Instance;
        if (mediator != null)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("End Current Turn", GUILayout.Height(28)))
            {
                var turnMgr = TurnManager.Instance;
                if (turnMgr != null) turnMgr.EndCurrentTurn ();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        // Kill all
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(_sectionStyle);
        EditorGUILayout.LabelField("Destroy Units", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Kill All P1 Units"))
            KillAllUnits(PlayerID.Player1);
        if (GUILayout.Button("Kill All P2 Units"))
            KillAllUnits(PlayerID.Player2);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // Quick info
        EditorGUILayout.Space(4);
        DrawGameInfo();
    }

    private void KillAllUnits(PlayerID player)
    {
        var spawner = UnitSpawner.Instance;
        if (spawner == null) return;
        var units = spawner.GetPlayerUnits(player);
        if (units == null) return;
        foreach (var u in units.ToList())
        {
            if (u != null && !u.IsDead())
                u.TakeDamage(9999);
        }
    }

    private void DrawGameInfo()
    {
        EditorGUILayout.BeginVertical(_sectionStyle);
        EditorGUILayout.LabelField("Game Info", EditorStyles.boldLabel);

        var spawner = UnitSpawner.Instance;
        if (spawner != null)
        {
            int p1 = spawner.GetPlayerUnits(PlayerID.Player1)?.Count(u => u != null && !u.IsDead()) ?? 0;
            int p2 = spawner.GetPlayerUnits(PlayerID.Player2)?.Count(u => u != null && !u.IsDead()) ?? 0;
            EditorGUILayout.LabelField($"  P1 alive: {p1}  |  P2 alive: {p2}");
        }

        var mpMgr = MPManager.Instance;
        if (mpMgr != null)
        {
            EditorGUILayout.LabelField($"  P1 MP: {mpMgr.GetCurrentMP(PlayerID.Player1)}  |  P2 MP: {mpMgr.GetCurrentMP(PlayerID.Player2)}");
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region ── Helpers ──

    private UnitController GetSelectedUnit()
    {
        if (Selection.activeGameObject == null) return null;
        return Selection.activeGameObject.GetComponent<UnitController>();
    }

    private void OnSelectionChange() => Repaint();
    private void OnInspectorUpdate() => Repaint();

    #endregion
}
