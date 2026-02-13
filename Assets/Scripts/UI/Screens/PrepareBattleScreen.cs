using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using TurnBasedGame.Skills;
using System.Linq;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Prepare Battle / Deck Builder screen.
    /// Layout 2 vùng: Collection (trái) — scroll list owned units/spells,
    /// Selected Deck (phải) — slots cho unit/spell đã chọn.
    /// Click card → thêm vào deck. Click slot → xóa khỏi deck.
    /// </summary>
    public class PrepareBattleScreen : BaseScreen
    {
        [Header("Data")]
        [SerializeField] private PlayerDataSO _playerData;

        [Header("Scene")]
        [SerializeField] private string _battleSceneName = "HUDScene";

        // ─── Tab State ───
        private enum CollectionTab { Units, Spells }
        private CollectionTab _activeTab = CollectionTab.Units;

        // ─── UI Refs ───
        private Button _btnBack;
        private Label _goldLabel;

        // Tabs
        private Button _tabUnits;
        private Button _tabSpells;

        // Collection
        private VisualElement _collectionList;
        private ScrollView _collectionScroll;
        private Label _emptyLabel;
        private TextField _searchField;

        // Deck
        private Label _deckUnitsTitle;
        private Label _deckSpellsTitle;
        private VisualElement _deckUnitsGrid;
        private VisualElement _deckSpellsGrid;
        private Label _totalCostLabel;
        private Label _validationLabel;

        // Actions
        private Button _btnClear;
        private Button _btnConfirm;

        // Slot cache
        private readonly List<DeckSlot> _unitSlots = new();
        private readonly List<DeckSlot> _spellSlots = new();

        // ─── Lifecycle ───

        protected override void OnScreenShow()
        {
            QueryElements();
            CreateDeckSlots();
            BindButtons();
            SubscribeDataEvents();
            SyncDeckFromData();
            RefreshGold();
            SwitchTab(CollectionTab.Units);
        }

        protected override void OnScreenHide()
        {
            UnbindButtons();
            UnsubscribeDataEvents();
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
            _goldLabel = Q<Label>("gold-label");

            _tabUnits = Q<Button>("tab-units");
            _tabSpells = Q<Button>("tab-spells");

            _collectionScroll = Q<ScrollView>("collection-scroll");
            _collectionList = Q("collection-list");
            _emptyLabel = Q<Label>("empty-label");
            _searchField = Q<TextField>("search-field");

            _deckUnitsTitle = Q<Label>("deck-units-title");
            _deckSpellsTitle = Q<Label>("deck-spells-title");
            _deckUnitsGrid = Q("deck-units-grid");
            _deckSpellsGrid = Q("deck-spells-grid");
            _totalCostLabel = Q<Label>("total-cost-label");
            _validationLabel = Q<Label>("validation-label");

            _btnClear = Q<Button>("btn-clear");
            _btnConfirm = Q<Button>("btn-confirm");
        }

        // ─── Deck Slots ───

        private void CreateDeckSlots()
        {
            _unitSlots.Clear();
            _spellSlots.Clear();
            _deckUnitsGrid?.Clear();
            _deckSpellsGrid?.Clear();

            for (int i = 0; i < PlayerDataSO.MaxDeckSize; i++)
            {
                var slot = new DeckSlot();
                int index = i;
                slot.RegisterCallback<ClickEvent>(_ => OnUnitSlotClicked(index));
                _unitSlots.Add(slot);
                _deckUnitsGrid?.Add(slot);
            }

            for (int i = 0; i < PlayerDataSO.MaxSpellSlots; i++)
            {
                var slot = new DeckSlot();
                int index = i;
                slot.RegisterCallback<ClickEvent>(_ => OnSpellSlotClicked(index));
                _spellSlots.Add(slot);
                _deckSpellsGrid?.Add(slot);
            }
        }

        // ─── Binding ───

        private void BindButtons()
        {
            _btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);
            _tabUnits?.RegisterCallback<ClickEvent>(OnTabUnitsClicked);
            _tabSpells?.RegisterCallback<ClickEvent>(OnTabSpellsClicked);
            _btnClear?.RegisterCallback<ClickEvent>(OnClearClicked);
            _btnConfirm?.RegisterCallback<ClickEvent>(OnConfirmClicked);
            _searchField?.RegisterValueChangedCallback(OnSearchChanged);
        }

        private void UnbindButtons()
        {
            _btnBack?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _tabUnits?.UnregisterCallback<ClickEvent>(OnTabUnitsClicked);
            _tabSpells?.UnregisterCallback<ClickEvent>(OnTabSpellsClicked);
            _btnClear?.UnregisterCallback<ClickEvent>(OnClearClicked);
            _btnConfirm?.UnregisterCallback<ClickEvent>(OnConfirmClicked);
            _searchField?.UnregisterValueChangedCallback(OnSearchChanged);
        }

        private void SubscribeDataEvents()
        {
            if (_playerData == null) return;
            _playerData.OnDeckChanged += HandleDeckChanged;
            _playerData.OnSpellsChanged += HandleSpellsChanged;
            _playerData.OnGoldChanged += HandleGoldChanged;
        }

        private void UnsubscribeDataEvents()
        {
            if (_playerData == null) return;
            _playerData.OnDeckChanged -= HandleDeckChanged;
            _playerData.OnSpellsChanged -= HandleSpellsChanged;
            _playerData.OnGoldChanged -= HandleGoldChanged;
        }

        // ─── Tab Logic ───

        private void SwitchTab(CollectionTab tab)
        {
            _activeTab = tab;
            _tabUnits?.EnableInClassList("prepare-tab--active", tab == CollectionTab.Units);
            _tabSpells?.EnableInClassList("prepare-tab--active", tab == CollectionTab.Spells);
            RefreshCollectionList();
        }

        // ─── Collection List ───

        private void RefreshCollectionList()
        {
            _collectionList?.Clear();
            if (_playerData == null) return;

            string filter = _searchField?.value?.Trim().ToLowerInvariant() ?? "";

            if (_activeTab == CollectionTab.Units)
                PopulateUnitCards(filter);
            else
                PopulateSpellCards(filter);
        }

        private void PopulateUnitCards(string filter)
        {
            var units = _playerData.OwnedUnits;
            int count = 0;

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null) continue;
                if (!string.IsNullOrEmpty(filter) && !unit.unitName.ToLowerInvariant().Contains(filter))
                    continue;

                var card = new UnitCardElement();
                card.Bind(unit);
                card.SetInDeck(_playerData.SelectedDeck.Contains(unit));

                var capturedUnit = unit;
                card.RegisterCallback<ClickEvent>(_ => OnUnitCardClicked(capturedUnit));

                _collectionList?.Add(card);
                count++;
            }

            ShowEmptyState(count == 0);
        }

        private void PopulateSpellCards(string filter)
        {
            var spells = _playerData.OwnedSpells;
            int count = 0;

            for (int i = 0; i < spells.Count; i++)
            {
                var spell = spells[i];
                if (spell == null) continue;
                if (!string.IsNullOrEmpty(filter) && !spell.SkillName.ToLowerInvariant().Contains(filter))
                    continue;

                var card = new SpellCardElement();
                card.Bind(spell);
                card.SetInDeck(_playerData.SelectedSpells.Contains(spell));

                var capturedSpell = spell;
                card.RegisterCallback<ClickEvent>(_ => OnSpellCardClicked(capturedSpell));

                _collectionList?.Add(card);
                count++;
            }

            ShowEmptyState(count == 0);
        }

        private void ShowEmptyState(bool show)
        {
            if (_emptyLabel != null)
                _emptyLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (_collectionScroll != null)
                _collectionScroll.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ─── Card Click Handlers ───

        private void OnUnitCardClicked(UnitData unit)
        {
            if (_playerData.SelectedDeck.Contains(unit))
            {
                _playerData.RemoveFromDeck(unit);
            }
            else
            {
                if (!_playerData.AddToDeck(unit))
                {
                    PopupManager.Instance?.ShowNotification(
                        $"Đội hình đã đầy ({PlayerDataSO.MaxDeckSize} binh lính)!");
                }
            }
        }

        private void OnSpellCardClicked(SkillBase spell)
        {
            if (_playerData.SelectedSpells.Contains(spell))
            {
                _playerData.RemoveSelectedSpell(spell);
            }
            else
            {
                if (!_playerData.AddSelectedSpell(spell))
                {
                    PopupManager.Instance?.ShowNotification(
                        $"Tối đa {PlayerDataSO.MaxSpellSlots} phép thuật!");
                }
            }
        }

        // ─── Slot Click Handlers ───

        private void OnUnitSlotClicked(int index)
        {
            if (index >= _unitSlots.Count) return;

            var slot = _unitSlots[index];
            if (slot.UnitData != null)
                _playerData.RemoveFromDeck(slot.UnitData);
        }

        private void OnSpellSlotClicked(int index)
        {
            if (index >= _spellSlots.Count) return;

            var slot = _spellSlots[index];
            if (slot.SpellData != null)
                _playerData.RemoveSelectedSpell(slot.SpellData);
        }

        // ─── Data Event Handlers ───

        private void HandleDeckChanged(List<UnitData> deck)
        {
            RefreshUnitSlots();
            RefreshCollectionList();
            UpdateDeckInfo();
        }

        private void HandleSpellsChanged(List<SkillBase> spells)
        {
            RefreshSpellSlots();
            RefreshCollectionList();
            UpdateDeckInfo();
        }

        private void HandleGoldChanged(int gold) => RefreshGold();

        // ─── Sync & Refresh ───

        private void SyncDeckFromData()
        {
            RefreshUnitSlots();
            RefreshSpellSlots();
            UpdateDeckInfo();
        }

        private void RefreshUnitSlots()
        {
            var deck = _playerData.SelectedDeck;

            for (int i = 0; i < _unitSlots.Count; i++)
            {
                if (i < deck.Count)
                    _unitSlots[i].BindUnit(deck[i]);
                else
                    _unitSlots[i].ClearSlot();
            }

            SetLabel(_deckUnitsTitle, $"Đội Hình ({deck.Count}/{PlayerDataSO.MaxDeckSize})");
        }

        private void RefreshSpellSlots()
        {
            var spells = _playerData.SelectedSpells;

            for (int i = 0; i < _spellSlots.Count; i++)
            {
                if (i < spells.Count)
                    _spellSlots[i].BindSpell(spells[i]);
                else
                    _spellSlots[i].ClearSlot();
            }

            SetLabel(_deckSpellsTitle, $"Phép ({spells.Count}/{PlayerDataSO.MaxSpellSlots})");
        }

        private void UpdateDeckInfo()
        {
            int cost = DeckValidator.CalculateTotalCost(_playerData.SelectedDeck);
            SetLabel(_totalCostLabel, $"Tổng chi phí: {cost}");

            var result = DeckValidator.Validate(_playerData.SelectedDeck, _playerData.SelectedSpells);
            SetLabel(_validationLabel, result.Message);

            _btnConfirm?.SetEnabled(result.IsValid);
        }

        private void RefreshGold()
        {
            if (_playerData != null)
                SetLabel(_goldLabel, $"💰 {_playerData.Gold}");
        }

        // ─── Button Handlers ───

        private void OnBackClicked(ClickEvent _) => UIManager.GoBack();

        private void OnTabUnitsClicked(ClickEvent _) => SwitchTab(CollectionTab.Units);

        private void OnTabSpellsClicked(ClickEvent _) => SwitchTab(CollectionTab.Spells);

        private void OnSearchChanged(ChangeEvent<string> _) => RefreshCollectionList();

        private void OnClearClicked(ClickEvent _)
        {
            PopupManager.Instance?.ShowConfirmDialog(
                "Xóa Đội Hình",
                "Xóa tất cả binh lính và phép thuật đã chọn?",
                onConfirm: () =>
                {
                    _playerData.ClearDeck();
                    _playerData.ClearSelectedSpells();
                },
                onCancel: null);
        }

        private void OnConfirmClicked(ClickEvent _)
        {
            var result = DeckValidator.Validate(_playerData.SelectedDeck, _playerData.SelectedSpells);

            if (!result.IsValid)
            {
                PopupManager.Instance?.ShowNotification(result.Message);
                return;
            }

            PopupManager.Instance?.ShowConfirmDialog(
                "Xác Nhận Chiến Đấu",
                $"Vào trận với {_playerData.SelectedDeck.Count} binh lính" +
                $" và {_playerData.SelectedSpells.Count} phép thuật?",
                onConfirm: StartBattle,
                onCancel: null,
                confirmText: "Chiến!",
                cancelText: "Chưa");
        }

        private void StartBattle()
        {
            Debug.Log($"[PrepareBattle] Loading battle scene: {_battleSceneName}");
            SceneLoader.Instance?.LoadSceneAsync(_battleSceneName);
        }

        // ─── Helpers ───

        private static void SetLabel(Label label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
