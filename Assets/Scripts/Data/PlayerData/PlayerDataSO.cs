using System;
using System.Collections.Generic;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;
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
    public List<UnitController> AvalailableUnits = new();

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
    [SerializeField] private List<UnitController> _ownedUnits = new();
    [SerializeField] private List<SpellCardData> _ownedSpells = new();

    // ─── Deck (chọn cho trận đấu) ───

    [Header("Selected Deck")]
    [Tooltip("Units được chọn cho trận đấu tiếp theo (max 6)")]
    [SerializeField] private List<UnitController> _selectedDeck = new();
    [Tooltip("Spells được chọn cho trận đấu tiếp theo (max 4)")]
    [SerializeField] private List<SpellCardData> _selectedSpells = new();

   
    public const int MaxSpellCardSlots = 4;

    // ─── Constants ───

    public const int MaxDeckSize = 6;
    public const int MaxSpellSlots = 4;

    // ─── Events ───

    public event Action<int> OnGoldChanged;
    public event Action<int, int> OnXPChanged; // (currentXP, level)
    public event Action<List<UnitController>> OnDeckChanged;
    public event Action<List<SpellCardData>> OnSpellsChanged;
    public event Action<UnitController> OnUnitAcquired;
    public event Action<SpellCardData> OnSpellAcquired;

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

    public IReadOnlyList<UnitController> OwnedUnits => _ownedUnits;
    public IReadOnlyList<SpellCardData> OwnedSpells => _ownedSpells;
    public IReadOnlyList<UnitController> SelectedDeck => _selectedDeck;
    public IReadOnlyList<SpellCardData> SelectedSpells => _selectedSpells;

    // ─── Collection Methods ───

    public bool OwnsUnit(UnitController unit) => _ownedUnits.Contains(unit);
    public bool OwnsSpell(SpellCardData spell) => _ownedSpells.Contains(spell);

    public void AddUnit(UnitController unit)
    {
        if (unit == null || _ownedUnits.Contains(unit)) return;
        _ownedUnits.Add(unit);
        OnUnitAcquired?.Invoke(unit);
    }

    public void AddSpell(SpellCardData spell)
    {
        if (spell == null || _ownedSpells.Contains(spell)) return;
        _ownedSpells.Add(spell);
        OnSpellAcquired?.Invoke(spell);
    }

    // ─── Deck Methods ───

    public bool AddToDeck(UnitController unit)
    {
        if (unit == null || _selectedDeck.Count >= MaxDeckSize) return false;
        if (_selectedDeck.Contains(unit)) return false;
        if (!_ownedUnits.Contains(unit)) return false;

        _selectedDeck.Add(unit);
        OnDeckChanged?.Invoke(_selectedDeck);
        return true;
    }

    public bool RemoveFromDeck(UnitController unit)
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

    public bool AddSelectedSpell(SpellCardData spell)
    {
        if (spell == null || _selectedSpells.Count >= MaxSpellSlots) return false;
        if (_selectedSpells.Contains(spell)) return false;
        if (!_ownedSpells.Contains(spell)) return false;

        _selectedSpells.Add(spell);
        OnSpellsChanged?.Invoke(_selectedSpells);
        return true;
    }

    public bool RemoveSelectedSpell(SpellCardData spell)
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
