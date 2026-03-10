using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using TurnBasedGame.Unit;
using TurnBasedGame.Skills;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.SpellCard;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Inventory / Bộ Sưu Tập screen.
    /// Grid/List view hiển thị OwnedUnits + OwnedSpells từ PlayerDataSO.
    /// Click card → hiện detail panel bên phải.
    /// Filter theo search text, Sort theo name/cost/hp.
    /// </summary>
    public class InventoryScreen : BaseScreen
    {
        [Header("Data")]
        [SerializeField] private PlayerDataSO _playerData;

        // ─── Tab State ───
        private enum CollectionTab { Units, Spells }
        private CollectionTab _activeTab = CollectionTab.Units;

        // ─── Sort ───
        private enum SortMode { Name, Cost, HP }
        private SortMode _currentSort = SortMode.Name;

        // ─── UI Refs ───
        private Button _btnBack;
        private Label _collectionCount;

        private Button _tabUnits;
        private Button _tabSpells;

        private TextField _searchField;
        private DropdownField _sortDropdown;

        private ScrollView _collectionScroll;
        private VisualElement _collectionGrid;
        private Label _emptyLabel;

        // Detail panels
        private VisualElement _detailPlaceholder;
        private UnitDetailPanel _unitDetail;
        private SpellDetailPanel _spellDetail;

        // Selection tracking
        private UnitMove _selectedUnit;
        private SpellCardData _selectedSpell;

        // ─── Lifecycle ───

        protected override void OnScreenShow()
        {
            QueryElements();
            SetupSortDropdown();
            InitDetailPanels();
            BindButtons();
            SubscribeDataEvents();
            ShowDetailPlaceholder();
            SwitchTab(CollectionTab.Units);
        }

        protected override void OnScreenHide()
        {
            UnbindButtons();
            UnsubscribeDataEvents();
            _selectedUnit = null;
            _selectedSpell = null;
        }

        public override bool OnBackPressed()
        {
            UIManager.GoBack();
            return false;
        }

        // ─── Query ───

        private void QueryElements()
        {
            _btnBack = Q<Button>("btn-back");
            _collectionCount = Q<Label>("collection-count");

            _tabUnits = Q<Button>("tab-units");
            _tabSpells = Q<Button>("tab-spells");

            _searchField = Q<TextField>("search-field");
            _sortDropdown = Q<DropdownField>("sort-dropdown");

            _collectionScroll = Q<ScrollView>("collection-scroll");
            _collectionGrid = Q("collection-grid");
            _emptyLabel = Q<Label>("empty-label");

            _detailPlaceholder = Q("detail-placeholder");
        }

        private void InitDetailPanels()
        {
            var unitRoot = Q("unit-detail");
            var spellRoot = Q("spell-detail");

            _unitDetail = unitRoot != null ? new UnitDetailPanel(unitRoot) : null;
            _spellDetail = spellRoot != null ? new SpellDetailPanel(spellRoot) : null;
        }

        private void SetupSortDropdown()
        {
            if (_sortDropdown == null) return;

            _sortDropdown.choices = new List<string> { "Tên", "Chi phí", "Máu" };
            _sortDropdown.index = 0;
            _sortDropdown.RegisterValueChangedCallback(OnSortChanged);
        }

        // ─── Binding ───

        private void BindButtons()
        {
            _btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);
            _tabUnits?.RegisterCallback<ClickEvent>(OnTabUnitsClicked);
            _tabSpells?.RegisterCallback<ClickEvent>(OnTabSpellsClicked);
            _searchField?.RegisterValueChangedCallback(OnSearchChanged);
        }

        private void UnbindButtons()
        {
            _btnBack?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _tabUnits?.UnregisterCallback<ClickEvent>(OnTabUnitsClicked);
            _tabSpells?.UnregisterCallback<ClickEvent>(OnTabSpellsClicked);
            _searchField?.UnregisterValueChangedCallback(OnSearchChanged);
            _sortDropdown?.UnregisterValueChangedCallback(OnSortChanged);
        }

        private void SubscribeDataEvents()
        {
            if (_playerData == null) return;
            _playerData.OnUnitAcquired += HandleUnitAcquired;
            _playerData.OnSpellAcquired += HandleSpellAcquired;
        }

        private void UnsubscribeDataEvents()
        {
            if (_playerData == null) return;
            _playerData.OnUnitAcquired -= HandleUnitAcquired;
            _playerData.OnSpellAcquired -= HandleSpellAcquired;
        }

        // ─── Tab Logic ───

        private void SwitchTab(CollectionTab tab)
        {
            _activeTab = tab;
            _tabUnits?.EnableInClassList("inv-tab--active", tab == CollectionTab.Units);
            _tabSpells?.EnableInClassList("inv-tab--active", tab == CollectionTab.Spells);

            // Update sort dropdown visibility (HP sort only for units)
            UpdateSortChoices();

            ShowDetailPlaceholder();
            _selectedUnit = null;
            _selectedSpell = null;
            RefreshGrid();
        }

        private void UpdateSortChoices()
        {
            if (_sortDropdown == null) return;

            if (_activeTab == CollectionTab.Units)
                _sortDropdown.choices = new List<string> { "Tên", "Chi phí", "Máu" };
            else
                _sortDropdown.choices = new List<string> { "Tên", "Cooldown", "Tầm bắn" };

            _sortDropdown.index = 0;
            _currentSort = SortMode.Name;
        }

        // ─── Grid Population ───

        private void RefreshGrid()
        {
            _collectionGrid?.Clear();
            if (_playerData == null) return;

            string filter = _searchField?.value?.Trim().ToLowerInvariant() ?? "";

            int count = _activeTab == CollectionTab.Units
                ? PopulateUnits(filter)
                : PopulateSpells(filter);

            ShowEmptyState(count == 0);
            UpdateCollectionCount(count);
        }

        private int PopulateUnits(string filter)
        {
            var units = GetSortedUnits(filter);
            int count = 0;

            foreach (var unit in units)
            {
                var card = new UnitCardElement();
                card.Bind(unit.UnitData);
                card.SetInDeck(_playerData.SelectedDeck.Contains(unit));
                card.SetSelected(unit == _selectedUnit);

                var captured = unit;
                card.RegisterCallback<ClickEvent>(_ => OnUnitCardClicked(captured, card));

                _collectionGrid?.Add(card);
                count++;
            }

            return count;
        }

        private int PopulateSpells(string filter)
        {
            var spells = GetSortedSpells(filter);
            int count = 0;

            foreach (var spell in spells)
            {
                var card = new SpellCardElement();
                card.Bind(spell);
                card.SetInDeck(_playerData.SelectedSpells.Contains(spell));
                card.SetSelected(spell == _selectedSpell);

                var captured = spell;
                card.RegisterCallback<ClickEvent>(_ => OnSpellCardClicked(captured, card));

                _collectionGrid?.Add(card);
                count++;
            }

            return count;
        }

        // ─── Sorting ───

        private List<UnitMove> GetSortedUnits(string filter)
        {
            var result = new List<UnitMove>();
            var owned = _playerData.OwnedUnits;

            for (int i = 0; i < owned.Count; i++)
            {
                var u = owned[i];
                if (u == null) continue;
                if (!string.IsNullOrEmpty(filter) && !u.UnitData.unitName.ToLowerInvariant().Contains(filter))
                    continue;
                result.Add(u);
            }

            result.Sort((a, b) => _currentSort switch
            {
                SortMode.Cost => a.UnitData.spawnCost.CompareTo(b.UnitData.spawnCost),
                SortMode.HP => b.UnitData.Health.CompareTo(a.UnitData.Health),
                _ => string.Compare(a.UnitData.unitName, b.UnitData.unitName, StringComparison.Ordinal)
            });

            return result;
        }

        private List<SpellCardData> GetSortedSpells(string filter)
        {
            var result = new List<SpellCardData>();
            var owned = _playerData.OwnedSpells;

            for (int i = 0; i < owned.Count; i++)
            {
                var s = owned[i];
                if (s == null) continue;
                if (!string.IsNullOrEmpty(filter) && !s.spellName.ToLowerInvariant().Contains(filter))
                    continue;
                result.Add(s);
            }

            result.Sort((a, b) => _currentSort switch
            {
                SortMode.Cost => a.mpCost.CompareTo(b.mpCost),    // "Chi phí" maps to mpCost for spells
                SortMode.HP => b.range.CompareTo(a.range),            // "Máu" maps to range for spells
                _ => string.Compare(a.spellName, b.spellName, StringComparison.Ordinal)
            });

            return result;
        }

        // ─── Card Click → Detail ───

        private void OnUnitCardClicked(UnitMove unit, UnitCardElement card)
        {
            _selectedUnit = unit;
            _selectedSpell = null;

            // Refresh grid to update selection visuals
            RefreshGrid();
            ShowUnitDetail(unit);
        }

        private void OnSpellCardClicked(SpellCardData spell, SpellCardElement card)
        {
            _selectedSpell = spell;
            _selectedUnit = null;

            RefreshGrid();
            ShowSpellDetail(spell);
        }

        // ─── Detail Display ───

        private void ShowDetailPlaceholder()
        {
            SetDisplay(_detailPlaceholder, true);
            _unitDetail?.Hide();
            _spellDetail?.Hide();
        }

        private void ShowUnitDetail(UnitMove unit)
        {
            SetDisplay(_detailPlaceholder, false);
            _spellDetail?.Hide();

            bool inDeck = _playerData.SelectedDeck.Contains(unit);
            _unitDetail?.Bind(unit.UnitData, inDeck);
            _unitDetail?.Show();
        }

        private void ShowSpellDetail(SpellCardData spell)
        {
            SetDisplay(_detailPlaceholder, false);
            _unitDetail?.Hide();

            bool inDeck = _playerData.SelectedSpells.Contains(spell);
            _spellDetail?.Bind(spell, inDeck);
            _spellDetail?.Show();
        }

        // ─── Helpers ───

        private void ShowEmptyState(bool show)
        {
            if (_emptyLabel != null)
                _emptyLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (_collectionScroll != null)
                _collectionScroll.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void UpdateCollectionCount(int count)
        {
            if (_collectionCount != null)
                _collectionCount.text = $"{count} vật phẩm";
        }

        private static void SetDisplay(VisualElement el, bool visible)
        {
            if (el != null)
                el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ─── Button Handlers ───

        private void OnBackClicked(ClickEvent _) => UIManager.GoBack();
        private void OnTabUnitsClicked(ClickEvent _) => SwitchTab(CollectionTab.Units);
        private void OnTabSpellsClicked(ClickEvent _) => SwitchTab(CollectionTab.Spells);
        private void OnSearchChanged(ChangeEvent<string> _) => RefreshGrid();

        private void OnSortChanged(ChangeEvent<string> evt)
        {
            _currentSort = (_sortDropdown?.index ?? 0) switch
            {
                1 => SortMode.Cost,
                2 => SortMode.HP,
                _ => SortMode.Name
            };
            RefreshGrid();
        }

        // ─── Data Event Handlers ───

        private void HandleUnitAcquired(UnitMove _)
        {
            if (_activeTab == CollectionTab.Units) RefreshGrid();
        }

        private void HandleSpellAcquired(SpellCardData _)
        {
            if (_activeTab == CollectionTab.Spells) RefreshGrid();
        }
    }
}
