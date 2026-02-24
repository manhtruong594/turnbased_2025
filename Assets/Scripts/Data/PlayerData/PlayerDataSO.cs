using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Skills;
using TurnBasedGame.Unit;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ dữ liệu player: profile, tài nguyên, bộ sưu tập, deck.
/// Dùng làm single source of truth cho UI (Shop, Inventory, Prepare Battle).
/// Events cho phép UI reactive binding.
/// </summary>
[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "TurnBased/PlayerDataSO", order = 1)]
public class PlayerDataSO : ScriptableObject
{
    // ─── Backward Compatibility (battle system hiện tại) ───

    [Header("Battle Units (Legacy)")]
    public List<UnitMove> AvalailableUnits = new();

    // ─── Profile ───

    [Header("Player Profile")]
    [SerializeField] private string _playerName = "Summoner";
    [SerializeField] private int _playerLevel = 1;
    [SerializeField] private int _xp;

    // ─── Economy ───

    [Header("Economy")]
    [SerializeField] private int _gold = 100;

    // ─── Collection (tất cả đã mở khóa) ───

    [Header("Owned Collection")]
    [SerializeField] private List<UnitMove> _ownedUnits = new();
    [SerializeField] private List<SkillBase> _ownedSpells = new();

    // ─── Deck (chọn cho trận đấu) ───

    [Header("Selected Deck")]
    [Tooltip("Units được chọn cho trận đấu tiếp theo (max 6)")]
    [SerializeField] private List<UnitMove> _selectedDeck = new();
    [Tooltip("Spells được chọn cho trận đấu tiếp theo (max 4)")]
    [SerializeField] private List<SkillBase> _selectedSpells = new();

    // ─── Constants ───

    public const int MaxDeckSize = 6;
    public const int MaxSpellSlots = 4;

    // ─── Events ───

    public event Action<int> OnGoldChanged;
    public event Action<int, int> OnXPChanged; // (currentXP, level)
    public event Action<List<UnitMove>> OnDeckChanged;
    public event Action<List<SkillBase>> OnSpellsChanged;
    public event Action<UnitMove> OnUnitAcquired;
    public event Action<SkillBase> OnSpellAcquired;

    // ─── Properties ───

    public string PlayerName
    {
        get => _playerName;
        set => _playerName = value;
    }

    public int PlayerLevel => _playerLevel;

    public int XP
    {
        get => _xp;
        set
        {
            _xp = value;
            OnXPChanged?.Invoke(_xp, _playerLevel);
        }
    }

    public int Gold
    {
        get => _gold;
        set
        {
            _gold = Mathf.Max(0, value);
            OnGoldChanged?.Invoke(_gold);
        }
    }

    public IReadOnlyList<UnitMove> OwnedUnits => _ownedUnits;
    public IReadOnlyList<SkillBase> OwnedSpells => _ownedSpells;
    public IReadOnlyList<UnitMove> SelectedDeck => _selectedDeck;
    public IReadOnlyList<SkillBase> SelectedSpells => _selectedSpells;

    // ─── Collection Methods ───

    public bool OwnsUnit(UnitMove unit) => _ownedUnits.Contains(unit);
    public bool OwnsSpell(SkillBase spell) => _ownedSpells.Contains(spell);

    public void AddUnit(UnitMove unit)
    {
        if (unit == null || _ownedUnits.Contains(unit)) return;
        _ownedUnits.Add(unit);
        OnUnitAcquired?.Invoke(unit);
    }

    public void AddSpell(SkillBase spell)
    {
        if (spell == null || _ownedSpells.Contains(spell)) return;
        _ownedSpells.Add(spell);
        OnSpellAcquired?.Invoke(spell);
    }

    // ─── Deck Methods ───

    public bool AddToDeck(UnitMove unit)
    {
        if (unit == null || _selectedDeck.Count >= MaxDeckSize) return false;
        if (_selectedDeck.Contains(unit)) return false;
        if (!_ownedUnits.Contains(unit)) return false;

        _selectedDeck.Add(unit);
        OnDeckChanged?.Invoke(_selectedDeck);
        return true;
    }

    public bool RemoveFromDeck(UnitMove unit)
    {
        if (!_selectedDeck.Remove(unit)) return false;
        OnDeckChanged?.Invoke(_selectedDeck);
        return true;
    }

    public void ClearDeck()
    {
        _selectedDeck.Clear();
        OnDeckChanged?.Invoke(_selectedDeck);
    }

    public bool AddSelectedSpell(SkillBase spell)
    {
        if (spell == null || _selectedSpells.Count >= MaxSpellSlots) return false;
        if (_selectedSpells.Contains(spell)) return false;
        if (!_ownedSpells.Contains(spell)) return false;

        _selectedSpells.Add(spell);
        OnSpellsChanged?.Invoke(_selectedSpells);
        return true;
    }

    public bool RemoveSelectedSpell(SkillBase spell)
    {
        if (!_selectedSpells.Remove(spell)) return false;
        OnSpellsChanged?.Invoke(_selectedSpells);
        return true;
    }

    public void ClearSelectedSpells()
    {
        _selectedSpells.Clear();
        OnSpellsChanged?.Invoke(_selectedSpells);
    }

    // ─── Validation ───

    /// <summary>Deck có đủ unit tối thiểu để vào trận.</summary>
    public bool IsDeckValid(int minUnits = 1) => _selectedDeck.Count >= minUnits;

    /// <summary>Có đủ gold để mua không.</summary>
    public bool CanAfford(int cost) => _gold >= cost;
}
