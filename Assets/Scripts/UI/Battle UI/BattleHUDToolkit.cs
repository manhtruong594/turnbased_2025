using System;
using System.Collections.Generic;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;
using UnityEngine.UIElements;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Runtime UI Toolkit view for HUDScene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleHUDToolkit : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _skillDescriptionHoldSeconds = 0.5f;

        private sealed class PlayerBinding
        {
            public Action EndTurn;
            public Action RollDice1;
            public Action RollDice2;
        }

        private sealed class PlayerFrameView
        {
            public Label Name;
            public Label Turn;
            public ProgressBar MP;
        }

        public static BattleHUDToolkit Instance { get; private set; }
        public static bool IsAvailable => Instance != null && Instance._root != null;

        private readonly Dictionary<PlayerID, PlayerBinding> _bindings = new();
        private readonly Dictionary<PlayerID, PlayerFrameView> _playerFrames = new();
        private readonly Dictionary<PlayerID, IReadOnlyList<UnitController>> _spawnUnits = new();
        private readonly Dictionary<PlayerID, IReadOnlyList<SpellCardData>> _spellHands = new();

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _localFrameSlot;
        private VisualElement _opponentFrameSlot;
        private VisualElement _sidePanel;
        private Label _sideTitle;
        private ScrollView _sideList;
        private VisualElement _skillPanel;
        private VisualElement _skillList;
        private VisualElement _skillDescriptionPanel;
        private Label _skillDescriptionName;
        private Label _skillDescriptionText;
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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _document = GetComponent<UIDocument>();
            _root = _document != null ? _document.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("[BattleHUDToolkit] UIDocument has no rootVisualElement.");
                enabled = false;
                return;
            }

            QueryElements();
            BindStaticButtons();
        }

        private void Start()
        {
            BuildPlayerFrames();
            SubscribeToPresentationEvents();
            RefreshFrameState();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnsubscribeFromPresentationEvents();
            UnbindStaticButtons();
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
            _localFrameSlot = _root.Q("local-frame-slot");
            _opponentFrameSlot = _root.Q("opponent-frame-slot");
            _sidePanel = _root.Q("side-panel");
            _sideTitle = _root.Q<Label>("side-title");
            _sideList = _root.Q<ScrollView>("side-list");
            _skillPanel = _root.Q("skill-panel");
            _skillList = _root.Q("skill-list");
            _skillDescriptionPanel = _root.Q("skill-description-panel");
            _skillDescriptionName = _root.Q<Label>("skill-description-name");
            _skillDescriptionText = _root.Q<Label>("skill-description-text");
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

        private void BuildPlayerFrames()
        {
            if (_localFrameSlot == null || _opponentFrameSlot == null)
            {
                Debug.LogError("[BattleHUDToolkit] Missing player frame slots in BattleHUD.uxml.");
                return;
            }

            _playerFrames.Clear();
            _localFrameSlot.Clear();
            _opponentFrameSlot.Clear();

            PlayerID localPlayer = MatchContext.LocalPlayer;
            PlayerID opponent = MatchContext.OpponentOf(localPlayer);
            CreatePlayerFrame(_localFrameSlot, localPlayer, localPlayer.ToString(), false, false);
            CreatePlayerFrame(_opponentFrameSlot, opponent,
                MatchContext.IsVersusAI ? "AI Opponent" : opponent.ToString(), true, MatchContext.IsVersusAI);
        }

        private void CreatePlayerFrame(VisualElement slot, PlayerID player, string displayName,
            bool alignRight, bool isAI)
        {
            var frame = new VisualElement { name = alignRight ? "opponent-frame" : "local-player-frame" };
            frame.AddToClassList("player-frame");
            if (alignRight) frame.AddToClassList("player-frame--right");
            if (isAI) frame.AddToClassList("player-frame--ai");

            var header = new VisualElement();
            header.AddToClassList("player-header");
            if (alignRight) header.AddToClassList("player-header--right");

            var name = new Label(displayName);
            name.AddToClassList("player-name");
            var turn = new Label("ĐẾN LƯỢT");
            turn.AddToClassList("turn-label");
            turn.AddToClassList("hidden");

            if (alignRight)
            {
                header.Add(turn);
                header.Add(name);
            }
            else
            {
                header.Add(name);
                header.Add(turn);
            }

            var mp = new ProgressBar
            {
                lowValue = 0,
                highValue = 1,
                value = 0,
                title = "0/0 MP"
            };
            mp.AddToClassList("mp-bar");

            frame.Add(header);
            frame.Add(mp);
            slot.Add(frame);
            _playerFrames[player] = new PlayerFrameView { Name = name, Turn = turn, MP = mp };
        }

        private void SubscribeToPresentationEvents()
        {
            if (GameMediator.Instance == null) return;
            GameMediator.Instance.OnPlayerTurnStarted += HandlePlayerTurnStarted;
            GameMediator.Instance.OnPlayerTurnEnded += HandlePlayerTurnEnded;
            GameMediator.Instance.OnMPChanged += SetMP;
            GameMediator.Instance.OnReplicaApplied += RefreshFrameState;
        }

        private void UnsubscribeFromPresentationEvents()
        {
            if (GameMediator.Instance == null) return;
            GameMediator.Instance.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
            GameMediator.Instance.OnPlayerTurnEnded -= HandlePlayerTurnEnded;
            GameMediator.Instance.OnMPChanged -= SetMP;
            GameMediator.Instance.OnReplicaApplied -= RefreshFrameState;
        }

        private void HandlePlayerTurnStarted(PlayerID player)
        {
            foreach (var entry in _playerFrames)
                entry.Value.Turn.EnableInClassList("hidden", entry.Key != player);
        }

        private void HandlePlayerTurnEnded(PlayerID player)
        {
            if (_playerFrames.TryGetValue(player, out var frame))
                frame.Turn.AddToClassList("hidden");
        }

        private void RefreshFrameState()
        {
            if (MPManager.Instance != null)
            {
                foreach (var player in _playerFrames.Keys)
                    SetMP(player, MPManager.Instance.GetCurrentMP(player), MPManager.Instance.MaxMP);
            }

            var turn = TurnManager.Instance;
            if (turn == null || turn.CurrentState == TurnState.Initialization || turn.CurrentState == TurnState.GameEnd)
            {
                foreach (var frame in _playerFrames.Values)
                    frame.Turn.AddToClassList("hidden");
                return;
            }

            HandlePlayerTurnStarted(turn.CurrentPlayer);
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
            if (_playerFrames.TryGetValue(player, out var frame)) frame.Name.text = playerName;
        }

        public void SetTurnActive(PlayerID player, bool active)
        {
            if (_playerFrames.TryGetValue(player, out var frame))
                frame.Turn.EnableInClassList("hidden", !active);
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
            if (!_playerFrames.TryGetValue(player, out var frame)) return;
            var bar = frame.MP;
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
                bool suppressClick = false;
                bool canUse = skill.CurrentCooldown <= 0;
                var captured = skill;
                var button = new Button(() =>
                {
                    if (!suppressClick)
                    {
                        HideSkillDescription();
                        if (canUse) onSkill?.Invoke(captured);
                    }
                    suppressClick = false;
                })
                {
                    text = skill.CurrentCooldown > 0
                        ? $"{skill.SkillName} — hồi {skill.CurrentCooldown}/{skill.Cooldown}"
                        : skill.SkillName
                };
                button.AddToClassList("hud-button");
                button.AddToClassList("skill-button");
                button.text = string.Empty;
                button.EnableInClassList("skill-button--disabled", !canUse);
                if (skill.Icon != null)
                {
                    var icon = new UnityEngine.UIElements.Image
                    {
                        sprite = skill.Icon,
                        scaleMode = ScaleMode.ScaleToFit,
                        pickingMode = PickingMode.Ignore
                    };
                    icon.style.width = 72;
                    icon.style.height = 72;
                    button.Insert(0, icon);
                }

                IVisualElementScheduledItem longPress = null;
                button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    suppressClick = false;
                    longPress?.Pause();
                    longPress = button.schedule.Execute(() =>
                    {
                        suppressClick = true;
                        ShowSkillDescription(captured);
                    }).StartingIn(Mathf.RoundToInt(_skillDescriptionHoldSeconds * 1000f));
                }, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerUpEvent>(_ =>
                {
                    longPress?.Pause();
                    HideSkillDescription();
                }, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    longPress?.Pause();
                    HideSkillDescription();
                });
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
            HideSkillDescription();
        }

        private void ShowSkillDescription(TurnBasedGame.Skills.ISkill skill)
        {
            if (_skillDescriptionPanel == null || skill == null) return;
            _skillDescriptionName.text = skill.SkillName;
            _skillDescriptionText.text = skill.Description;
            _skillPanel.BringToFront();
            _skillDescriptionPanel.BringToFront();
            _skillDescriptionPanel.RemoveFromClassList("hidden");
        }

        private void HideSkillDescription()
        {
            _skillDescriptionPanel?.AddToClassList("hidden");
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

    }
}
