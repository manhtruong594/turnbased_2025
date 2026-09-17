using System;
using System.Collections.Generic;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Runtime UI Toolkit view for HUDScene. The old Canvas stays serialized as a rollback path,
    /// while its renderers and raycasters are disabled when this view is available.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleHUDToolkit : MonoBehaviour
    {
        private sealed class PlayerBinding
        {
            public Action EndTurn;
            public Action RollDice1;
            public Action RollDice2;
        }

        public static BattleHUDToolkit Instance { get; private set; }
        public static bool IsAvailable => Instance != null && Instance._root != null;

        private readonly Dictionary<PlayerID, PlayerBinding> _bindings = new();
        private readonly Dictionary<PlayerID, IReadOnlyList<UnitController>> _spawnUnits = new();
        private readonly Dictionary<PlayerID, IReadOnlyList<SpellCardData>> _spellHands = new();

        private UIDocument _document;
        private PanelSettings _runtimePanelSettings;
        private VisualElement _root;
        private VisualElement _sidePanel;
        private Label _sideTitle;
        private ScrollView _sideList;
        private VisualElement _skillPanel;
        private VisualElement _skillList;
        private Label _selectedUnitName;
        private Button _finishActionButton;
        private Button _undoActionButton;
        private Button _summonButton;
        private Button _spellButton;
        private Button _dice1Button;
        private Button _dice2Button;
        private Button _endTurnButton;
        private Label _turnTimerLabel;
        private VisualElement _confirmOverlay;
        private VisualElement _endgameOverlay;
        private PlayerID? _activePlayer;
        private bool _showingSpells;
        private int _displayedTurnSeconds = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (!string.Equals(scene.name, "HUDScene", StringComparison.Ordinal)) return;
            if (PlayerPrefs.GetInt("BattleHUD.UseUIToolkit", 1) == 0) return;
            if (FindFirstObjectByType<BattleHUDToolkit>() != null) return;

            var source = UnityEngine.Resources.Load<VisualTreeAsset>("UI/BattleHUD");
            if (source == null)
            {
                Debug.LogError("[BattleHUDToolkit] Missing Resources/UI/BattleHUD.uxml.");
                return;
            }

            var panelTemplate = UnityEngine.Resources.Load<PanelSettings>("UI/BattleHUDPanelSettings");
            if (panelTemplate == null)
            {
                Debug.LogError("[BattleHUDToolkit] Missing Resources/UI/BattleHUDPanelSettings.asset.");
                return;
            }

            var host = new GameObject("BattleHUD_UI Toolkit");
            SceneManager.MoveGameObjectToScene(host, scene);
            var document = host.AddComponent<UIDocument>();
            document.enabled = false;
            var settings = Instantiate(panelTemplate);
            settings.name = "BattleHUD Runtime Panel Settings";
            document.panelSettings = settings;
            document.visualTreeAsset = source;
            document.sortingOrder = 100;
            document.enabled = true;
            host.AddComponent<BattleHUDToolkit>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _document = GetComponent<UIDocument>();
            _runtimePanelSettings = _document != null ? _document.panelSettings : null;
            _root = _document != null ? _document.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("[BattleHUDToolkit] UIDocument has no rootVisualElement.");
                enabled = false;
                return;
            }

            QueryElements();
            BindStaticButtons();
            DisableLegacyScreenCanvases();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnbindStaticButtons();
            if (_runtimePanelSettings != null) Destroy(_runtimePanelSettings);
        }

        private void Update()
        {
            if (_turnTimerLabel == null) return;
            int seconds = TurnManager.Instance != null
                ? Mathf.CeilToInt(Mathf.Max(0f, TurnManager.Instance.Timer))
                : 0;
            if (seconds == _displayedTurnSeconds) return;
            _displayedTurnSeconds = seconds;
            _turnTimerLabel.text = seconds.ToString("00");
        }

        private void QueryElements()
        {
            _sidePanel = _root.Q("side-panel");
            _sideTitle = _root.Q<Label>("side-title");
            _sideList = _root.Q<ScrollView>("side-list");
            _skillPanel = _root.Q("skill-panel");
            _skillList = _root.Q("skill-list");
            _selectedUnitName = _root.Q<Label>("unit-name");
            _finishActionButton = _root.Q<Button>("finish-action");
            _undoActionButton = _root.Q<Button>("undo-action");
            _summonButton = _root.Q<Button>("summon-button");
            _spellButton = _root.Q<Button>("spell-button");
            _dice1Button = _root.Q<Button>("dice-1-button");
            _dice2Button = _root.Q<Button>("dice-2-button");
            _endTurnButton = _root.Q<Button>("end-turn-button");
            _turnTimerLabel = _root.Q<Label>("turn-timer");
            _confirmOverlay = _root.Q("confirm-overlay");
            _endgameOverlay = _root.Q("endgame-overlay");
        }

        private void BindStaticButtons()
        {
            _summonButton.clicked += ToggleSpawnPanel;
            _spellButton.clicked += ToggleSpellPanel;
            _dice1Button.clicked += RollDice1;
            _dice2Button.clicked += RollDice2;
            _endTurnButton.clicked += EndTurn;
        }

        private void UnbindStaticButtons()
        {
            if (_summonButton != null) _summonButton.clicked -= ToggleSpawnPanel;
            if (_spellButton != null) _spellButton.clicked -= ToggleSpellPanel;
            if (_dice1Button != null) _dice1Button.clicked -= RollDice1;
            if (_dice2Button != null) _dice2Button.clicked -= RollDice2;
            if (_endTurnButton != null) _endTurnButton.clicked -= EndTurn;
        }

        private void DisableLegacyScreenCanvases()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas == null || canvas.gameObject.scene != gameObject.scene ||
                    canvas.renderMode == RenderMode.WorldSpace) continue;
                canvas.enabled = false;
                var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = false;
            }
        }

        public void BindPlayer(PlayerID player, Action endTurn)
        {
            var binding = GetBinding(player);
            binding.EndTurn = endTurn;
        }

        public void BindDice(PlayerID player, Action rollDice1, Action rollDice2)
        {
            var binding = GetBinding(player);
            binding.RollDice1 = rollDice1;
            binding.RollDice2 = rollDice2;
        }

        public void SetPlayerName(PlayerID player, string playerName)
        {
            var label = _root.Q<Label>($"player-{PlayerNumber(player)}-name");
            if (label != null) label.text = playerName;
        }

        public void SetTurnActive(PlayerID player, bool active)
        {
            var turn = _root.Q<Label>($"player-{PlayerNumber(player)}-turn");
            turn?.EnableInClassList("hidden", !active);
            if (active) _activePlayer = player;
            else if (_activePlayer == player) _activePlayer = null;

            SetActionButtonsEnabled(active && _activePlayer == player);
            if (!active)
            {
                HideSidePanel();
                HideUnitActions();
            }
        }

        public void SetMP(PlayerID player, int current, int maximum)
        {
            var bar = _root.Q<ProgressBar>($"player-{PlayerNumber(player)}-mp");
            if (bar == null) return;
            bar.lowValue = 0;
            bar.highValue = Mathf.Max(1, maximum);
            bar.value = current;
            bar.title = $"{current}/{maximum} MP";
            RefreshCurrentList();
        }

        public void SetDice(PlayerID player, int index, string value)
        {
            if (_activePlayer != player) return;
            var button = index == 1 ? _dice1Button : _dice2Button;
            if (button != null) button.text = $"Xúc xắc {index}: {value}";
        }

        public void SetDiceEnabled(PlayerID player, int index, bool enabled)
        {
            if (_activePlayer != player) return;
            (index == 1 ? _dice1Button : _dice2Button)?.SetEnabled(enabled);
        }

        public void SetSpawnUnits(PlayerID player, IReadOnlyList<UnitController> units)
        {
            _spawnUnits[player] = units;
            if (_activePlayer == player && !_showingSpells && IsSidePanelVisible()) BuildSpawnList(player);
        }

        public void SetSpellHand(PlayerID player, IReadOnlyList<SpellCardData> spells)
        {
            _spellHands[player] = spells;
            if (_activePlayer == player && _showingSpells && IsSidePanelVisible()) BuildSpellList(player);
        }

        public void ShowUnitActions(UnitController unit, IReadOnlyList<TurnBasedGame.Skills.ISkill> skills,
            Action<TurnBasedGame.Skills.ISkill> onSkill, Action finishAction)
        {
            if (_skillPanel == null || _skillList == null || unit == null) return;
            _skillList.Clear();
            _selectedUnitName.text = unit.UnitData != null ? unit.UnitData.unitName : unit.name;
            foreach (var skill in skills)
            {
                var captured = skill;
                var button = new Button(() => onSkill?.Invoke(captured))
                {
                    text = skill.CurrentCooldown > 0
                        ? $"{skill.SkillName} — hồi {skill.CurrentCooldown}/{skill.Cooldown}"
                        : skill.SkillName
                };
                button.AddToClassList("hud-button");
                button.AddToClassList("skill-button");
                button.SetEnabled(skill.CurrentCooldown <= 0);
                _skillList.Add(button);
            }

            _finishActionButton.clicked -= _finishAction;
            _finishAction = finishAction;
            _finishActionButton.clicked += _finishAction;
            _undoActionButton.clicked -= _undoAction;
            _undoAction = () => LocalMatchAuthority.SubmitHumanUnitAction(unit, true);
            _undoActionButton.clicked += _undoAction;
            _undoActionButton.SetEnabled(unit.IsMoveDone());
            _skillPanel.RemoveFromClassList("hidden");
        }

        private Action _finishAction;
        private Action _undoAction;

        public void HideUnitActions()
        {
            if (_skillPanel == null) return;
            _skillPanel.AddToClassList("hidden");
            if (_finishActionButton != null && _finishAction != null) _finishActionButton.clicked -= _finishAction;
            if (_undoActionButton != null && _undoAction != null) _undoActionButton.clicked -= _undoAction;
            _finishAction = null;
            _undoAction = null;
            _skillList?.Clear();
        }

        public void ShowSpellConfirmation(SpellCardData card, Action confirm, Action cancel)
        {
            if (_confirmOverlay == null || card == null) return;
            _confirmOverlay.Q<Label>("confirm-name").text = card.spellName;
            _confirmOverlay.Q<Label>("confirm-description").text = card.description;
            _confirmOverlay.Q<Label>("confirm-cost").text = $"{card.mpCost} MP";
            var icon = _confirmOverlay.Q("confirm-icon");
            if (card.icon != null) icon.style.backgroundImage = new StyleBackground(card.icon);
            else icon.style.backgroundImage = StyleKeyword.None;
            var accept = _confirmOverlay.Q<Button>("confirm-accept");
            var reject = _confirmOverlay.Q<Button>("confirm-cancel");
            accept.clicked += Confirm;
            reject.clicked += Cancel;
            _confirmOverlay.RemoveFromClassList("hidden");

            void Confirm()
            {
                accept.clicked -= Confirm;
                reject.clicked -= Cancel;
                _confirmOverlay.AddToClassList("hidden");
                confirm?.Invoke();
            }

            void Cancel()
            {
                accept.clicked -= Confirm;
                reject.clicked -= Cancel;
                _confirmOverlay.AddToClassList("hidden");
                cancel?.Invoke();
            }
        }

        public void ShowEndgame(string result, string subtitle, Action restart, Action mainMenu)
        {
            if (_endgameOverlay == null) return;
            _endgameOverlay.Q<Label>("endgame-result").text = result;
            _endgameOverlay.Q<Label>("endgame-subtitle").text = subtitle;
            var restartButton = _endgameOverlay.Q<Button>("restart-button");
            var menuButton = _endgameOverlay.Q<Button>("main-menu-button");
            restartButton.clicked += Restart;
            menuButton.clicked += MainMenu;
            _endgameOverlay.RemoveFromClassList("hidden");

            void Restart()
            {
                restartButton.clicked -= Restart;
                menuButton.clicked -= MainMenu;
                restart?.Invoke();
            }

            void MainMenu()
            {
                restartButton.clicked -= Restart;
                menuButton.clicked -= MainMenu;
                mainMenu?.Invoke();
            }
        }

        public static bool IsPointerOverHUD(Vector2 screenPosition)
        {
            if (!IsAvailable || Instance._root.panel == null) return false;
            var panelPosition = RuntimePanelUtils.ScreenToPanel(Instance._root.panel, screenPosition);
            return Instance._root.panel.Pick(panelPosition) != null;
        }

        private void ToggleSpawnPanel()
        {
            if (!_activePlayer.HasValue) return;
            if (IsSidePanelVisible() && !_showingSpells) { HideSidePanel(); return; }
            _showingSpells = false;
            _sideTitle.text = "TRIỆU HỒI";
            _sidePanel.RemoveFromClassList("hidden");
            BuildSpawnList(_activePlayer.Value);
        }

        private void ToggleSpellPanel()
        {
            if (!_activePlayer.HasValue) return;
            if (IsSidePanelVisible() && _showingSpells) { HideSidePanel(); return; }
            _showingSpells = true;
            _sideTitle.text = "PHÉP THUẬT";
            _sidePanel.RemoveFromClassList("hidden");
            BuildSpellList(_activePlayer.Value);
        }

        private void BuildSpawnList(PlayerID player)
        {
            _sideList.Clear();
            if (!_spawnUnits.TryGetValue(player, out var units) || units == null) return;
            foreach (var unit in units)
            {
                if (unit == null || unit.UnitData == null) continue;
                var captured = unit;
                bool canSpawn = MPManager.Instance != null && TurnManager.Instance != null
                    && MPManager.Instance.HasEnoughMP(player, unit.UnitData.spawnCost)
                    && UnitSpawner.Instance != null && UnitSpawner.Instance.GetAvailableSpawnPoints(player).Count > 0
                    && TurnBasedGame.Multiplayer.MatchGameplayBootstrap.CanControl(player);
                _sideList.Add(CreateCardRow(unit.UnitData.unitName, $"{unit.UnitData.spawnCost} MP", null,
                    unit.UnitData.icon, canSpawn, () => LocalMatchAuthority.SubmitHumanSpawn(captured)));
            }
        }

        private void BuildSpellList(PlayerID player)
        {
            _sideList.Clear();
            var hand = SpellCardManager.Instance?.GetHand(player);
            if (hand == null && !_spellHands.TryGetValue(player, out hand)) return;
            foreach (var card in hand)
            {
                if (card == null) continue;
                var captured = card;
                bool canUse = SpellCardManager.Instance != null && SpellCardManager.Instance.CanUseCard(card, player);
                _sideList.Add(CreateCardRow(card.spellName, $"{card.mpCost} MP", card.description,
                    card.icon, canUse, () => SpellCardManager.Instance?.SelectCard(captured, player)));
            }
        }

        private static VisualElement CreateCardRow(string title, string cost, string description, Sprite icon,
            bool enabled, Action clicked)
        {
            var row = new Button(clicked);
            row.AddToClassList("card-row");
            row.SetEnabled(enabled);
            var image = new UnityEngine.UIElements.Image { sprite = icon };
            image.AddToClassList("card-icon");
            row.Add(image);
            var text = new VisualElement();
            text.AddToClassList("card-text");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("card-name");
            text.Add(titleLabel);
            if (!string.IsNullOrEmpty(description))
            {
                var descriptionLabel = new Label(description);
                descriptionLabel.AddToClassList("card-description");
                text.Add(descriptionLabel);
            }
            row.Add(text);
            var costLabel = new Label(cost);
            costLabel.AddToClassList("card-cost");
            row.Add(costLabel);
            return row;
        }

        private void RefreshCurrentList()
        {
            if (!_activePlayer.HasValue || !IsSidePanelVisible()) return;
            if (_showingSpells) BuildSpellList(_activePlayer.Value);
            else BuildSpawnList(_activePlayer.Value);
        }

        private void HideSidePanel()
        {
            _sidePanel?.AddToClassList("hidden");
            _sideList?.Clear();
        }

        private bool IsSidePanelVisible() => _sidePanel != null && !_sidePanel.ClassListContains("hidden");

        private void RollDice1() { if (TryGetActiveBinding(out var binding)) binding.RollDice1?.Invoke(); }
        private void RollDice2() { if (TryGetActiveBinding(out var binding)) binding.RollDice2?.Invoke(); }
        private void EndTurn() { if (TryGetActiveBinding(out var binding)) binding.EndTurn?.Invoke(); }

        private void SetActionButtonsEnabled(bool enabled)
        {
            _summonButton?.SetEnabled(enabled);
            _spellButton?.SetEnabled(enabled);
            _dice1Button?.SetEnabled(enabled);
            _dice2Button?.SetEnabled(enabled);
            _endTurnButton?.SetEnabled(enabled);
        }

        private bool TryGetActiveBinding(out PlayerBinding binding)
        {
            binding = null;
            return _activePlayer.HasValue && _bindings.TryGetValue(_activePlayer.Value, out binding);
        }

        private PlayerBinding GetBinding(PlayerID player)
        {
            if (!_bindings.TryGetValue(player, out var binding))
            {
                binding = new PlayerBinding();
                _bindings.Add(player, binding);
            }
            return binding;
        }

        private static int PlayerNumber(PlayerID player) => player == PlayerID.Player1 ? 1 : 2;
    }
}
