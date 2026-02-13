using UnityEngine;
using UnityEngine.UIElements;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Main Menu screen — entry point của game.
    /// Buttons: Chiến Đấu, Cửa Hàng, Bộ Sưu Tập, Cài Đặt.
    /// Hiển thị player info (name, level, gold) từ PlayerDataSO.
    /// </summary>
    public class MainMenuScreen : BaseScreen
    {
        [Header("Data")]
        [SerializeField] private PlayerDataSO _playerData;

        // UI element refs
        private Label _playerNameLabel;
        private Label _playerLevelLabel;
        private Label _playerGoldLabel;
        private Button _btnBattle;
        private Button _btnShop;
        private Button _btnInventory;
        private Button _btnSettings;

        // ─── Lifecycle ───

        protected override void OnScreenShow()
        {
            QueryElements();
            BindButtons();
            RefreshPlayerInfo();
            SubscribeEvents();
        }

        protected override void OnScreenHide()
        {
            UnbindButtons();
            UnsubscribeEvents();
        }

        /// <summary>Main Menu là root — Back không pop.</summary>
        public override bool OnBackPressed() => false;

        // ─── UI Binding ───

        private void QueryElements()
        {
            _playerNameLabel = Q<Label>("player-name");
            _playerLevelLabel = Q<Label>("player-level");
            _playerGoldLabel = Q<Label>("player-gold");
            _btnBattle = Q<Button>("btn-battle");
            _btnShop = Q<Button>("btn-shop");
            _btnInventory = Q<Button>("btn-inventory");
            _btnSettings = Q<Button>("btn-settings");
        }

        private void BindButtons()
        {
            _btnBattle?.RegisterCallback<ClickEvent>(OnBattleClicked);
            _btnShop?.RegisterCallback<ClickEvent>(OnShopClicked);
            _btnInventory?.RegisterCallback<ClickEvent>(OnInventoryClicked);
            _btnSettings?.RegisterCallback<ClickEvent>(OnSettingsClicked);
        }

        private void UnbindButtons()
        {
            _btnBattle?.UnregisterCallback<ClickEvent>(OnBattleClicked);
            _btnShop?.UnregisterCallback<ClickEvent>(OnShopClicked);
            _btnInventory?.UnregisterCallback<ClickEvent>(OnInventoryClicked);
            _btnSettings?.UnregisterCallback<ClickEvent>(OnSettingsClicked);
        }

        private void SubscribeEvents()
        {
            if (_playerData != null)
                _playerData.OnGoldChanged += HandleGoldChanged;
        }

        private void UnsubscribeEvents()
        {
            if (_playerData != null)
                _playerData.OnGoldChanged -= HandleGoldChanged;
        }

        // ─── Data Display ───

        private void RefreshPlayerInfo()
        {
            if (_playerData == null) return;

            SetLabel(_playerNameLabel, _playerData.PlayerName);
            SetLabel(_playerLevelLabel, $"Lv. {_playerData.PlayerLevel}");
            SetLabel(_playerGoldLabel, FormatGold(_playerData.Gold));
        }

        private void HandleGoldChanged(int newGold)
        {
            SetLabel(_playerGoldLabel, FormatGold(newGold));
        }

        // ─── Button Handlers ───

        private void OnBattleClicked(ClickEvent _)
        {
            UIManager.ShowScreen<PrepareBattleScreen>();
        }

        private void OnShopClicked(ClickEvent _)
        {
            // TODO: Phase E — ShowScreen<ShopScreen>()
            Debug.Log("[MainMenu] → Shop (chưa triển khai)");
        }

        private void OnInventoryClicked(ClickEvent _)
        {
            UIManager.ShowScreen<InventoryScreen>();
        }

        private void OnSettingsClicked(ClickEvent _)
        {
            // TODO: Phase G — ShowPopup<SettingsPopup>()
            Debug.Log("[MainMenu] → Settings (chưa triển khai)");
        }

        // ─── Helpers ───

        private static void SetLabel(Label label, string text)
        {
            if (label != null) label.text = text;
        }

        private static string FormatGold(int gold) =>
            gold >= 1000 ? $"💰 {gold:N0}" : $"💰 {gold}";
    }
}
