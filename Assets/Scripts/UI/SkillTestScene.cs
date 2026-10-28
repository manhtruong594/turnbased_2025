using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Multiplayer;
using TurnBasedGame.Resources;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TurnBasedGame.UI
{
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SkillTestScene : MonoBehaviour
    {
        public const string ScenePath = "Assets/Scenes/SkillTest.unity";
        private const string StyleSheetResourcePath = "UI/SkillTest";

        private enum TestTab
        {
            Combat = 0,
            Animation = 1,
            Sandbox = 2
        }

        private enum StatusType
        {
            Info,
            Success,
            Warning,
            Error
        }

        private static int characterIndex;
        private static int targetIndex;
        private static bool ignoreSkillLimits = true;

        private readonly List<MatchContentCatalog.Entry> characters = new();
        private readonly List<SkillBase> ownedSkills = new();
        private readonly List<string> eventLogs = new();
        private readonly StringBuilder statsBuilder = new(128);

        private UnitController caster;
        private UnitAnimator animator;
        private Camera worldCamera;
        private SkillBase selectedSkill;
        private bool placingTarget;
        private bool reloading;
        private bool isCollapsed;
        private TestTab currentTab = TestTab.Combat;

        // UI Toolkit Elements
        private VisualElement panel;
        private VisualElement headerContainer;
        private VisualElement bodyContainer;
        private Label titleLabel;
        private Button collapseBtn;
        private Label statusLabel;
        private VisualElement statusBanner;
        private Button tabCombatBtn;
        private Button tabAnimBtn;
        private Button tabSandboxBtn;
        private ScrollView contentScroll;
        private ProgressBar hpBar;
        private ProgressBar mpBar;
        private Label debuffBadge;
        private Label footerCoordLabel;
        private VisualElement eventLogContainer;

        private string validationMessage;
        private int lastRecordedHp = -1;
        private int lastRecordedMp = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSelection()
        {
            characterIndex = 0;
            targetIndex = 0;
            ignoreSkillLimits = true;
        }

        private void Awake()
        {
            BattleLaunchContext.Clear();
            MatchContext.ConfigureLocalPvP();
            TurnBasedGame.ObjectPool.ObjectPoolManager.Instance?.ClearAllPools();
        }

        private IEnumerator Start()
        {
            BuildPanel();
            SetStatus("Đang tải danh mục nhân vật...", StatusType.Info);

            var catalog = UnityEngine.Resources.Load<MatchContentCatalog>(MatchContentCatalog.ResourceName);
            if (catalog == null)
            {
                SetStatus("Thiếu MatchContentCatalog trong Resources.", StatusType.Error);
                yield break;
            }

            foreach (var entry in catalog.Entries)
            {
                if (entry != null && entry.UnitPrefab != null)
                    characters.Add(entry);
            }

            if (characters.Count == 0)
            {
                SetStatus("MatchContentCatalog chưa có character prefab hợp lệ.", StatusType.Error);
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + 10f;
            while (GameMediator.Instance == null || !GameMediator.Instance.IsInitialized ||
                   TurnManager.Instance.CurrentState == TurnState.Initialization)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    SetStatus("Khởi tạo trận đấu thất bại. Kiểm tra Unity Console.", StatusType.Error);
                    yield break;
                }
                yield return null;
            }

            TurnManager.Instance.enabled = false;
            AreaPathManager.Instance.enabled = false;
            worldCamera = Camera.main;

            characterIndex = Mathf.Clamp(characterIndex, 0, characters.Count - 1);
            targetIndex = Mathf.Clamp(targetIndex, 0, characters.Count - 1);

            SpawnCaster(characterIndex);

            // Xây dựng nội dung Tab ban đầu
            RefreshCurrentTab();
            SetStatus("Sẵn sàng. Chọn kỹ năng hoặc đặt target lên bàn cờ.", StatusType.Info);

            // Bắt đầu nhịp polling cập nhật HP/MP và trạng thái mượt mà
            panel.schedule.Execute(UpdateStats).Every(200);
        }

        #region UI Construction & Styling

        private void BuildPanel()
        {
            var uiDoc = GetComponent<UIDocument>();
            var root = uiDoc.rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;

            // Nạp StyleSheet UI Toolkit
            var styleSheet = UnityEngine.Resources.Load<StyleSheet>(StyleSheetResourcePath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            panel = new VisualElement { name = "skill-test-root" };
            panel.AddToClassList("st-root");
            root.Add(panel);

            // ── Header Bar ──
            headerContainer = new VisualElement();
            headerContainer.AddToClassList("st-header");

            titleLabel = new Label("SKILL TEST BENCH");
            titleLabel.AddToClassList("st-title");
            headerContainer.Add(titleLabel);

            var headerActions = new VisualElement();
            headerActions.AddToClassList("st-header-actions");

            var healFullBtn = new Button(QuickFullHealAndMP) { text = "Heal" };
            healFullBtn.AddToClassList("st-mini-btn");
            healFullBtn.tooltip = "Hồi đầy HP và MP cho Caster";
            headerActions.Add(healFullBtn);

            var resetCdBtn = new Button(QuickResetCooldowns) { text = "CD" };
            resetCdBtn.AddToClassList("st-mini-btn");
            resetCdBtn.tooltip = "Xóa toàn bộ Cooldown skill của Caster";
            headerActions.Add(resetCdBtn);

            var reloadBtn = new Button(Reload) { text = "Reload" };
            reloadBtn.AddToClassList("st-mini-btn");
            reloadBtn.AddToClassList("st-mini-btn--danger");
            reloadBtn.tooltip = "Tải lại toàn bộ scene";
            headerActions.Add(reloadBtn);

            collapseBtn = new Button(ToggleCollapse) { text = "—" };
            collapseBtn.AddToClassList("st-mini-btn");
            collapseBtn.tooltip = "Thu gọn / Mở rộng panel";
            headerActions.Add(collapseBtn);

            headerContainer.Add(headerActions);
            panel.Add(headerContainer);

            // ── Body Container (ẩn khi collapse) ──
            bodyContainer = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
            panel.Add(bodyContainer);

            // ── Status Banner ──
            statusBanner = new VisualElement();
            statusBanner.AddToClassList("st-status-banner");
            statusBanner.AddToClassList("st-status--info");

            statusLabel = new Label("Đang khởi tạo...");
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            statusBanner.Add(statusLabel);
            bodyContainer.Add(statusBanner);

            // ── Caster Quick Status (HP & MP Bars) ──
            var statusSection = new VisualElement();
            statusSection.AddToClassList("st-section");

            var hpRow = new VisualElement();
            hpRow.AddToClassList("st-bar-container");
            var hpLabelRow = new VisualElement();
            hpLabelRow.AddToClassList("st-bar-label-row");
            hpLabelRow.Add(new Label("HP"));
            debuffBadge = new Label("SLOW") { style = { display = DisplayStyle.None, color = new Color(1f, 0.6f, 0.2f), unityFontStyleAndWeight = FontStyle.Bold } };
            hpLabelRow.Add(debuffBadge);
            hpRow.Add(hpLabelRow);

            hpBar = new ProgressBar();
            hpBar.AddToClassList("st-progress-bar");
            hpBar.AddToClassList("st-hp-bar");
            hpRow.Add(hpBar);
            statusSection.Add(hpRow);

            var mpRow = new VisualElement();
            mpRow.AddToClassList("st-bar-container");
            var mpLabelRow = new VisualElement();
            mpLabelRow.AddToClassList("st-bar-label-row");
            mpLabelRow.Add(new Label("MP"));
            mpRow.Add(mpLabelRow);

            mpBar = new ProgressBar();
            mpBar.AddToClassList("st-progress-bar");
            mpBar.AddToClassList("st-mp-bar");
            mpRow.Add(mpBar);
            statusSection.Add(mpRow);

            bodyContainer.Add(statusSection);

            // ── Tab Bar ──
            var tabBar = new VisualElement();
            tabBar.AddToClassList("st-tab-bar");

            tabCombatBtn = new Button(() => SwitchTab(TestTab.Combat)) { text = "⚔ Combat" };
            tabCombatBtn.AddToClassList("st-tab-btn");
            tabBar.Add(tabCombatBtn);

            tabAnimBtn = new Button(() => SwitchTab(TestTab.Animation)) { text = "🎬 Animation" };
            tabAnimBtn.AddToClassList("st-tab-btn");
            tabBar.Add(tabAnimBtn);

            tabSandboxBtn = new Button(() => SwitchTab(TestTab.Sandbox)) { text = "🛠 Sandbox" };
            tabSandboxBtn.AddToClassList("st-tab-btn");
            tabBar.Add(tabSandboxBtn);

            bodyContainer.Add(tabBar);

            // ── Content ScrollView ──
            contentScroll = new ScrollView { name = "st-content-scroll" };
            contentScroll.AddToClassList("st-scroll");
            bodyContainer.Add(contentScroll);

            // ── Footer ──
            var footer = new VisualElement();
            footer.AddToClassList("st-footer");
            footerCoordLabel = new Label("Tile: —");
            footer.Add(footerCoordLabel);
            footer.Add(new Label("Esc: Hủy chọn"));
            bodyContainer.Add(footer);
        }

        private void ToggleCollapse()
        {
            isCollapsed = !isCollapsed;
            if (isCollapsed)
            {
                panel.AddToClassList("st-root--collapsed");
                bodyContainer.style.display = DisplayStyle.None;
                collapseBtn.text = "+";
            }
            else
            {
                panel.RemoveFromClassList("st-root--collapsed");
                bodyContainer.style.display = DisplayStyle.Flex;
                collapseBtn.text = "—";
            }
        }

        private void SwitchTab(TestTab tab)
        {
            currentTab = tab;
            tabCombatBtn.RemoveFromClassList("st-tab-btn--active");
            tabAnimBtn.RemoveFromClassList("st-tab-btn--active");
            tabSandboxBtn.RemoveFromClassList("st-tab-btn--active");

            switch (tab)
            {
                case TestTab.Combat:
                    tabCombatBtn.AddToClassList("st-tab-btn--active");
                    break;
                case TestTab.Animation:
                    tabAnimBtn.AddToClassList("st-tab-btn--active");
                    break;
                case TestTab.Sandbox:
                    tabSandboxBtn.AddToClassList("st-tab-btn--active");
                    break;
            }

            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            if (contentScroll == null) return;
            contentScroll.Clear();

            switch (currentTab)
            {
                case TestTab.Combat:
                    BuildCombatTab(contentScroll);
                    break;
                case TestTab.Animation:
                    BuildAnimationTab(contentScroll);
                    break;
                case TestTab.Sandbox:
                    BuildSandboxTab(contentScroll);
                    break;
            }
        }

        private void SetStatus(string message, StatusType type = StatusType.Info)
        {
            if (statusLabel == null || statusBanner == null) return;

            statusBanner.RemoveFromClassList("st-status--info");
            statusBanner.RemoveFromClassList("st-status--success");
            statusBanner.RemoveFromClassList("st-status--warning");
            statusBanner.RemoveFromClassList("st-status--error");

            switch (type)
            {
                case StatusType.Info:
                    statusBanner.AddToClassList("st-status--info");
                    break;
                case StatusType.Success:
                    statusBanner.AddToClassList("st-status--success");
                    break;
                case StatusType.Warning:
                    statusBanner.AddToClassList("st-status--warning");
                    break;
                case StatusType.Error:
                    statusBanner.AddToClassList("st-status--error");
                    break;
            }

            statusLabel.text = message;
        }

        #endregion

        #region Tab 1: Combat & Skills

        private void BuildCombatTab(VisualElement container)
        {
            var names = new List<string>(characters.Count);
            foreach (var entry in characters)
                names.Add(entry.UnitPrefab.UnitData.unitName + " [" + entry.UnitPrefab.name + "]");

            // ── Caster & Target Dropdowns ──
            var setupSection = new VisualElement();
            setupSection.AddToClassList("st-section");

            var characterField = new DropdownField("Caster (P1)", names, characterIndex);
            characterField.AddToClassList("st-field");
            characterField.RegisterValueChangedCallback(_ => SwitchCaster(characterField.index));
            setupSection.Add(characterField);

            var targetField = new DropdownField("Target (P2)", names, targetIndex);
            targetField.AddToClassList("st-field");
            targetField.RegisterValueChangedCallback(_ => targetIndex = targetField.index);
            setupSection.Add(targetField);

            // ── Target Action Buttons ──
            var targetBtnRow = new VisualElement();
            targetBtnRow.AddToClassList("st-btn-row");

            var placeTargetBtn = new Button(BeginPlacement) { text = "Đặt Target (Tile)" };
            placeTargetBtn.AddToClassList("st-btn");
            targetBtnRow.Add(placeTargetBtn);

            var spawnAdjBtn = new Button(SpawnTargetAdjacent) { text = "Spawn Cạnh Caster" };
            spawnAdjBtn.AddToClassList("st-btn");
            spawnAdjBtn.tooltip = "Spawn 1 target Player 2 ngay ô trống liền kề Caster";
            targetBtnRow.Add(spawnAdjBtn);

            setupSection.Add(targetBtnRow);

            var clearTargetsBtn = new Button(ClearAllTargets) { text = "Xóa Tất Cả Targets (P2)" };
            clearTargetsBtn.AddToClassList("st-btn");
            setupSection.Add(clearTargetsBtn);

            container.Add(setupSection);

            // ── Active Skills List ──
            var skillsSection = new VisualElement();
            skillsSection.AddToClassList("st-section");

            var skillsHeader = new Label("ACTIVE SKILLS");
            skillsHeader.AddToClassList("st-section-title");
            skillsSection.Add(skillsHeader);

            if (caster != null && caster.AttackComponent != null)
            {
                bool hasSkill = false;
                foreach (var item in caster.AttackComponent.ActiveSkills)
                {
                    if (item is not SkillBase skill || skill.Type == SkillType.Passive) continue;
                    hasSkill = true;
                    skillsSection.Add(CreateSkillCard(skill));
                }

                if (!hasSkill)
                {
                    skillsSection.Add(new Label("Nhân vật không có active skill nào.") { style = { opacity = 0.6f } });
                }
            }
            else
            {
                skillsSection.Add(new Label("Chưa có Caster trên bàn cờ.") { style = { opacity = 0.6f } });
            }

            container.Add(skillsSection);
        }

        private VisualElement CreateSkillCard(SkillBase skill)
        {
            var card = new Button(() => SelectSkill(skill));
            card.AddToClassList("st-skill-card");

            if (selectedSkill == skill)
            {
                card.AddToClassList("st-skill-card--selected");
            }

            if (skill.CurrentCooldown > 0)
            {
                card.AddToClassList("st-skill-card--cooldown");
            }

            var cardHeader = new VisualElement();
            cardHeader.AddToClassList("st-skill-header");

            var nameLabel = new Label(skill.SkillName);
            nameLabel.AddToClassList("st-skill-name");
            cardHeader.Add(nameLabel);
            card.Add(cardHeader);

            var badgeRow = new VisualElement();
            badgeRow.AddToClassList("st-badge-row");

            var mpBadge = new Label($"MP {skill.MPCost}");
            mpBadge.AddToClassList("st-badge");
            mpBadge.AddToClassList("st-badge--mp");
            badgeRow.Add(mpBadge);

            var rangeBadge = new Label($"Range {skill.Range}");
            rangeBadge.AddToClassList("st-badge");
            rangeBadge.AddToClassList("st-badge--range");
            badgeRow.Add(rangeBadge);

            var targetBadge = new Label(DescribeTargets(skill));
            targetBadge.AddToClassList("st-badge");
            targetBadge.AddToClassList("st-badge--target");
            badgeRow.Add(targetBadge);

            if (skill.CurrentCooldown > 0)
            {
                var cdBadge = new Label($"CD {skill.CurrentCooldown}");
                cdBadge.AddToClassList("st-badge");
                cdBadge.AddToClassList("st-badge--cd");
                badgeRow.Add(cdBadge);
            }

            card.Add(badgeRow);

            string condition = DescribeCondition(skill);
            if (!string.IsNullOrEmpty(condition))
            {
                var hintLabel = new Label(condition.Trim());
                hintLabel.AddToClassList("st-skill-hint");
                card.Add(hintLabel);
            }

            return card;
        }

        #endregion

        #region Tab 2: Animation & Events

        private void BuildAnimationTab(VisualElement container)
        {
            var animSection = new VisualElement();
            animSection.AddToClassList("st-section");

            var animTitle = new Label("ANIMATION PREVIEW");
            animTitle.AddToClassList("st-section-title");
            animSection.Add(animTitle);

            AddAnimationRow(animSection, "Idle", () => Preview(AnimationHashLib.Idle), AnimationHashLib.Idle, false);
            AddAnimationRow(animSection, "Move (tại chỗ)", () =>
            {
                Preview(AnimationHashLib.Idle);
                if (animator != null) animator.StartMoving();
            }, AnimationHashLib.Idle, true);
            AddAnimationRow(animSection, "Hit", () => Preview(AnimationHashLib.Hit), AnimationHashLib.Hit, false);
            AddAnimationRow(animSection, "Death", () => Preview(AnimationHashLib.Death), AnimationHashLib.Death, false);

            if (caster != null && caster.AttackComponent != null)
            {
                foreach (var item in caster.AttackComponent.ActiveSkills)
                {
                    if (item is SkillBase skill && skill.Type != SkillType.Passive)
                    {
                        int hash = AnimationHashLib.GetHashAnimByAttackType(skill.Type);
                        AddAnimationRow(animSection, skill.SkillName, () => Preview(hash), hash, false);
                    }
                }
            }

            container.Add(animSection);

            // ── Animation Event Monitor ──
            var monitorSection = new VisualElement();
            monitorSection.AddToClassList("st-section");

            var monitorHeader = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
            var monitorTitle = new Label("ANIMATION CUE MONITOR");
            monitorTitle.AddToClassList("st-section-title");
            monitorHeader.Add(monitorTitle);

            var clearLogBtn = new Button(ClearEventLogs) { text = "Clear" };
            clearLogBtn.AddToClassList("st-mini-btn");
            monitorHeader.Add(clearLogBtn);
            monitorSection.Add(monitorHeader);

            eventLogContainer = new ScrollView();
            eventLogContainer.AddToClassList("st-event-box");
            RefreshEventLogView();
            monitorSection.Add(eventLogContainer);

            container.Add(monitorSection);
        }

        private void AddAnimationRow(VisualElement parent, string text, Action previewAction, int stateHash, bool isMove)
        {
            var row = new VisualElement();
            row.AddToClassList("st-anim-row");

            var previewBtn = new Button(previewAction) { text = text };
            previewBtn.AddToClassList("st-btn");
            previewBtn.AddToClassList("st-anim-preview-btn");
            row.Add(previewBtn);

            var pingBtn = new Button(() => PingAnimation(text, stateHash, isMove)) { text = "Ping" };
            pingBtn.AddToClassList("st-btn");
            pingBtn.AddToClassList("st-anim-ping-btn");
            pingBtn.tooltip = "Highlight file clip animation trong Project Window";
            row.Add(pingBtn);

            parent.Add(row);
        }

        private void OnAnimationEventRaised(string cueName)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            eventLogs.Insert(0, $"[{time}] {cueName}");
            if (eventLogs.Count > 30) eventLogs.RemoveAt(eventLogs.Count - 1);
            RefreshEventLogView();
        }

        private void ClearEventLogs()
        {
            eventLogs.Clear();
            RefreshEventLogView();
        }

        private void RefreshEventLogView()
        {
            if (eventLogContainer == null) return;
            eventLogContainer.Clear();

            if (eventLogs.Count == 0)
            {
                eventLogContainer.Add(new Label("Chưa có cue nào được phát sinh...") { style = { opacity = 0.5f, fontSize = 10 } });
                return;
            }

            foreach (var log in eventLogs)
            {
                var label = new Label(log);
                label.AddToClassList("st-event-item");
                eventLogContainer.Add(label);
            }
        }

        #endregion

        #region Tab 3: Sandbox & Cheats

        private void BuildSandboxTab(VisualElement container)
        {
            // ── Caster Tweaks ──
            var casterSection = new VisualElement();
            casterSection.AddToClassList("st-section");
            var casterTitle = new Label("CASTER MODIFIERS");
            casterTitle.AddToClassList("st-section-title");
            casterSection.Add(casterTitle);

            var woundBtn = new Button(PrepareWoundedCaster) { text = "Làm Caster Mất 50% HP (thử Heal)" };
            woundBtn.AddToClassList("st-btn");
            casterSection.Add(woundBtn);

            var slowBtn = new Button(PrepareDebuffedCaster) { text = "Gắn Slow Lên Caster (thử Cleanse)" };
            slowBtn.AddToClassList("st-btn");
            casterSection.Add(slowBtn);

            var clearBuffsBtn = new Button(ClearCasterBuffs) { text = "Xóa Sạch Buff/Debuff Caster" };
            clearBuffsBtn.AddToClassList("st-btn");
            casterSection.Add(clearBuffsBtn);

            var healMpBtn = new Button(QuickFullHealAndMP) { text = "Hồi Đầy HP & MP" };
            healMpBtn.AddToClassList("st-btn");
            healMpBtn.AddToClassList("st-btn--primary");
            casterSection.Add(healMpBtn);

            var resetCdBtn = new Button(QuickResetCooldowns) { text = "Reset Cooldown Toàn Bộ Skill" };
            resetCdBtn.AddToClassList("st-btn");
            casterSection.Add(resetCdBtn);

            container.Add(casterSection);

            // ── Target Tweaks ──
            var targetSection = new VisualElement();
            targetSection.AddToClassList("st-section");
            var targetTitle = new Label("TARGET (P2) MODIFIERS");
            targetTitle.AddToClassList("st-section-title");
            targetSection.Add(targetTitle);

            var targetBtnRow = new VisualElement();
            targetBtnRow.AddToClassList("st-btn-row");

            var dmg20Btn = new Button(() => DamageAllTargets(20)) { text = "Gây 20 DMG" };
            dmg20Btn.AddToClassList("st-btn");
            targetBtnRow.Add(dmg20Btn);

            var dmg50Btn = new Button(() => DamageAllTargets(50)) { text = "Gây 50 DMG" };
            dmg50Btn.AddToClassList("st-btn");
            targetBtnRow.Add(dmg50Btn);

            targetSection.Add(targetBtnRow);

            var killTargetBtn = new Button(KillAllTargets) { text = "Tiêu Diệt Tất Cả Target (Kill)" };
            killTargetBtn.AddToClassList("st-btn");
            killTargetBtn.AddToClassList("st-btn--danger");
            targetSection.Add(killTargetBtn);

            container.Add(targetSection);

            // ── Sandbox Options ──
            var optionsSection = new VisualElement();
            optionsSection.AddToClassList("st-section");
            var optionsTitle = new Label("SANDBOX TOGGLES");
            optionsTitle.AddToClassList("st-section-title");
            optionsSection.Add(optionsTitle);

            var ignoreLimitsToggle = new Toggle("Bỏ qua Cooldown/MP khi test") { value = ignoreSkillLimits };
            ignoreLimitsToggle.RegisterValueChangedCallback(evt =>
            {
                ignoreSkillLimits = evt.newValue;
                if (caster != null) caster.IgnoreSkillUseLimitsForTest = ignoreSkillLimits;
                SetStatus(ignoreSkillLimits ? "Đã bật chế độ test tự do (bỏ qua Cooldown/MP)." : "Đã bật kiểm tra Cooldown/MP nghiêm ngặt.", StatusType.Info);
            });
            optionsSection.Add(ignoreLimitsToggle);

            container.Add(optionsSection);
        }

        #endregion

        #region Caster & Target Management (Hot-Swap)

        private void SpawnCaster(int index)
        {
            caster = Spawn(index, PlayerID.Player1, new Vector3Int(-1, 0, 0));
            caster.IgnoreSkillUseLimitsForTest = ignoreSkillLimits;
            animator = caster.GetComponentInChildren<UnitAnimator>();
            if (animator != null)
            {
                animator.AnimationEventRaised += OnAnimationEventRaised;
            }
        }

        private void SwitchCaster(int newIndex)
        {
            if (newIndex == characterIndex && caster != null) return;

            characterIndex = Mathf.Clamp(newIndex, 0, characters.Count - 1);
            DespawnCaster();
            SpawnCaster(characterIndex);

            RefreshCurrentTab();
            SetStatus("Đã đổi Caster sang: " + characters[characterIndex].UnitPrefab.UnitData.unitName, StatusType.Success);
            UpdateStats();
        }

        private void DespawnCaster()
        {
            CancelSelection();
            if (animator != null)
            {
                animator.AnimationEventRaised -= OnAnimationEventRaised;
                animator = null;
            }

            if (caster != null)
            {
                MapManager.Instance?.UnregisterUnit(caster.currentGridPosition);
                UnitSpawner.Instance?.GetPlayerUnits(PlayerID.Player1)?.Remove(caster);
                Destroy(caster.gameObject);
                caster = null;
            }

            foreach (var skill in ownedSkills)
            {
                if (skill != null) Destroy(skill);
            }
            ownedSkills.Clear();
        }

        private UnitController Spawn(int index, PlayerID owner, Vector3Int tile)
        {
            var entry = characters[index];
            var unit = Instantiate(entry.UnitPrefab, MapManager.Instance.MapEntity.WorldPosition(tile),
                Quaternion.identity, UnitSpawner.Instance.transform);
            unit.AssignMatchIdentity(LocalMatchAuthority.Runtime.AllocateUnit(unit), entry.Id);
            unit.Init(owner, tile);

            foreach (var skill in unit.AttackComponent.ActiveSkills)
            {
                if (skill is SkillBase clone) ownedSkills.Add(clone);
            }

            UnitSpawner.Instance.GetPlayerUnits(owner).Add(unit);
            unit.AttackComponent.enabled = false;
            return unit;
        }

        private void SpawnTargetAdjacent()
        {
            if (caster == null || caster.IsDead())
            {
                SetStatus("Caster không tồn tại hoặc đã chết.", StatusType.Warning);
                return;
            }

            var center = caster.currentGridPosition;
            var directions = MapManager.Instance?.MapEntity?.NeighboursDirection;
            Vector3Int? freeTile = null;

            if (directions != null)
            {
                foreach (var dir in directions)
                {
                    var neighborPos = center + dir;
                    if (MapManager.Instance.IsTileAvailable(neighborPos))
                    {
                        freeTile = neighborPos;
                        break;
                    }
                }
            }

            if (!freeTile.HasValue && MapManager.Instance != null && MapManager.Instance.MapEntity != null)
            {
                var areaTiles = MapManager.Instance.MapEntity.Area(center, 1f);
                foreach (var pos in areaTiles)
                {
                    if (pos != center && MapManager.Instance.IsTileAvailable(pos))
                    {
                        freeTile = pos;
                        break;
                    }
                }
            }

            if (freeTile.HasValue)
            {
                Spawn(targetIndex, PlayerID.Player2, freeTile.Value);
                SetStatus("Đã spawn target Player 2 tại " + freeTile.Value, StatusType.Success);
                UpdateStats();
            }
            else
            {
                SetStatus("Không tìm thấy ô trống liền kề Caster để đặt target.", StatusType.Warning);
            }
        }

        private void ClearAllTargets()
        {
            CancelSelection();
            var p2Units = UnitSpawner.Instance?.GetPlayerUnits(PlayerID.Player2);
            if (p2Units != null)
            {
                var list = new List<UnitController>(p2Units);
                foreach (var unit in list)
                {
                    if (unit == null) continue;
                    MapManager.Instance?.UnregisterUnit(unit.currentGridPosition);
                    p2Units.Remove(unit);
                    Destroy(unit.gameObject);
                }
            }

            SetStatus("Đã xóa tất cả target Player 2 trên bàn cờ.", StatusType.Info);
            UpdateStats();
        }

        private void DamageAllTargets(int amount)
        {
            var p2Units = UnitSpawner.Instance?.GetPlayerUnits(PlayerID.Player2);
            if (p2Units == null || p2Units.Count == 0)
            {
                SetStatus("Chưa có target nào trên bàn cờ để gây sát thương.", StatusType.Warning);
                return;
            }

            int count = 0;
            foreach (var target in p2Units)
            {
                if (target == null || target.IsDead()) continue;
                target.TakeNonLethalDamage(amount);
                count++;
            }

            SetStatus($"Đã gây {amount} sát thương lên {count} target.", StatusType.Success);
            UpdateStats();
        }

        private void KillAllTargets()
        {
            var p2Units = UnitSpawner.Instance?.GetPlayerUnits(PlayerID.Player2);
            if (p2Units == null || p2Units.Count == 0)
            {
                SetStatus("Chưa có target nào trên bàn cờ để tiêu diệt.", StatusType.Warning);
                return;
            }

            int count = 0;
            foreach (var target in p2Units)
            {
                if (target == null || target.IsDead()) continue;
                target.TakeDamage(target.GetMaxHealth(), true);
                count++;
            }

            SetStatus($"Đã tiêu diệt {count} target.", StatusType.Success);
            UpdateStats();
        }

        #endregion

        #region Stats & Realtime Polling

        private void UpdateStats()
        {
            if (hpBar == null || mpBar == null) return;

            // 1. Cập nhật MP
            if (MPManager.Instance != null)
            {
                int currentMP = MPManager.Instance.GetCurrentMP(PlayerID.Player1);
                int maxMP = MPManager.Instance.MaxMP;
                if (lastRecordedMp != currentMP)
                {
                    lastRecordedMp = currentMP;
                    mpBar.value = maxMP > 0 ? (float)currentMP / maxMP * 100f : 0f;
                    mpBar.title = $"{currentMP}/{maxMP}";
                }
            }

            // 2. Cập nhật HP & Debuff Caster
            if (caster != null)
            {
                int hp = caster.GetCurrentHealth();
                int maxHp = caster.GetMaxHealth();
                if (lastRecordedHp != hp)
                {
                    lastRecordedHp = hp;
                    hpBar.value = maxHp > 0 ? (float)hp / maxHp * 100f : 0f;
                    hpBar.title = $"{hp}/{maxHp}";
                }

                bool hasSlow = caster.BuffHandler != null && caster.BuffHandler.HasEffect(StatusEffectType.Slow);
                if (debuffBadge != null)
                {
                    debuffBadge.style.display = hasSlow ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
            else
            {
                hpBar.value = 0;
                hpBar.title = "0/0";
            }
        }

        private void QuickFullHealAndMP()
        {
            if (caster != null && !caster.IsDead())
            {
                caster.Heal(caster.GetMaxHealth());
            }

            if (MPManager.Instance != null)
            {
                MPManager.Instance.AddMP(PlayerID.Player1, MPManager.Instance.MaxMP);
            }

            SetStatus("Đã hồi đầy HP và MP cho Caster.", StatusType.Success);
            UpdateStats();
        }

        private void QuickResetCooldowns()
        {
            if (caster != null && caster.AttackComponent != null)
            {
                foreach (var skill in caster.AttackComponent.ActiveSkills)
                {
                    if (skill is SkillBase sb) sb.ResetCooldown();
                }

                SetStatus("Đã xóa toàn bộ Cooldown skill.", StatusType.Success);
                if (currentTab == TestTab.Combat) RefreshCurrentTab();
            }
        }

        private void PrepareWoundedCaster()
        {
            CancelSelection();
            if (caster == null || caster.IsDead()) { SetStatus("Caster đã chết. Nhấn Heal hoặc Reset test.", StatusType.Warning); return; }
            if (caster.GetCurrentHealth() <= 1) { SetStatus("Caster không thể mất thêm HP.", StatusType.Warning); return; }

            caster.TakeNonLethalDamage(Mathf.Max(1, caster.GetMaxHealth() / 2));
            SetStatus("Caster đã mất 50% HP. Chọn skill hồi máu rồi bấm tile của caster.", StatusType.Info);
            UpdateStats();
        }

        private void PrepareDebuffedCaster()
        {
            CancelSelection();
            if (caster == null || caster.IsDead()) { SetStatus("Caster đã chết. Nhấn Heal hoặc Reset test.", StatusType.Warning); return; }
            if (caster.BuffHandler == null) { SetStatus("Caster không có BuffDebuffHandler.", StatusType.Warning); return; }

            caster.BuffHandler.AddEffect(new ActiveStatusEffect(StatusEffectType.Slow, 25, 2, PlayerID.Player2));
            SetStatus("Caster đã dính Slow. Chọn skill giải debuff rồi bấm tile của caster.", StatusType.Info);
            UpdateStats();
        }

        private void ClearCasterBuffs()
        {
            if (caster != null && caster.BuffHandler != null)
            {
                caster.BuffHandler.ClearAll();
                SetStatus("Đã xóa toàn bộ Buff/Debuff trên Caster.", StatusType.Success);
                UpdateStats();
            }
        }

        #endregion

        #region Skill Execution & Interaction

        private void BeginPlacement()
        {
            CancelSelection();
            placingTarget = true;
            SetStatus("Bấm vào tile trống trên bàn cờ để đặt Target Player 2. [Esc] để hủy.", StatusType.Warning);
        }

        private static string DescribeTargets(SkillBase skill)
        {
            if (skill is FireballSkill) return "ô bất kỳ trong range";
            var targets = new List<string>(4);
            if (skill.CanTargetEnemies) targets.Add("địch");
            if (skill.CanTargetAllies) targets.Add("đồng minh");
            if (skill.CanTargetSelf) targets.Add("bản thân");
            if (skill.CanTargetEmptyTile) targets.Add("ô trống");
            return targets.Count > 0 ? string.Join(", ", targets) : "riêng";
        }

        private static string DescribeCondition(SkillBase skill)
        {
            if (skill is HealSkill) return "Mục tiêu phải mất HP.";
            if (skill is HerbalistCleanseSkill) return "Mục tiêu phải có debuff.";
            if (skill is AssassinShadowDualStrikeSkill) return "Cần target địch và ô trống cạnh target để dịch chuyển.";
            if (skill is AdjudicatorForbiddenSealSkill) return "Cần ít nhất một địch trong vùng ảnh hưởng.";
            return string.Empty;
        }

        private void SelectSkill(SkillBase skill)
        {
            CancelSelection();
            if (caster == null || caster.IsDead()) { SetStatus("Caster đã chết. Nhấn Heal hoặc đổi nhân vật.", StatusType.Warning); return; }

            selectedSkill = skill;
            AreaPathManager.Instance.ShowAttackArea(MapManager.Instance.MapEntity.WalkableBorder(caster.currentGridPosition, skill.Range));

            string cond = DescribeCondition(skill);
            SetStatus($"[{skill.SkillName}] Chọn: {DescribeTargets(skill)}. {(string.IsNullOrEmpty(cond) ? "" : cond + " ")}[Esc] để hủy.", StatusType.Warning);

            if (currentTab == TestTab.Combat) RefreshCurrentTab();
        }

        private void CancelSelection()
        {
            selectedSkill = null;
            placingTarget = false;
            AreaPathManager.Instance?.HideAttackArea();
        }

        private void Preview(int hash)
        {
            CancelSelection();
            if (animator != null) animator.PreviewAnimation(hash);
        }

        private void PingAnimation(string label, int stateHash, bool isMove)
        {
#if UNITY_EDITOR
            var clip = ResolveAnimationClip(stateHash, isMove);
            if (clip == null)
            {
                SetStatus("Không tìm thấy animation clip cho " + label + ".", StatusType.Warning);
                return;
            }

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(clip);
            UnityEditor.EditorUtility.FocusProjectWindow();
            UnityEditor.Selection.activeObject = clip;
            UnityEditor.EditorGUIUtility.PingObject(clip);
            SetStatus("Đã ping clip: " + clip.name, StatusType.Success);
#else
            SetStatus("Chức năng ping asset chỉ dùng trong Unity Editor.", StatusType.Warning);
#endif
        }

        #endregion

        #region Animation Resolver (Editor Only)

#if UNITY_EDITOR
        private AnimationClip ResolveAnimationClip(int stateHash, bool isMove)
        {
            var unityAnimator = animator != null ? animator.Animator : null;
            var runtimeController = unityAnimator != null ? unityAnimator.runtimeAnimatorController : null;
            if (runtimeController == null) return null;

            var overrideController = runtimeController as AnimatorOverrideController;
            var baseController = (overrideController != null
                ? overrideController.runtimeAnimatorController
                : runtimeController) as UnityEditor.Animations.AnimatorController;
            if (baseController == null) return null;

            var state = isMove
                ? FindMoveState(baseController)
                : FindState(baseController, stateHash);
            if (state == null) return null;

            var clip = ExtractClip(state.motion);
            if (clip != null && overrideController != null)
            {
                var overridden = overrideController[clip];
                if (overridden != null) clip = overridden;
            }
            return clip;
        }

        private static UnityEditor.Animations.AnimatorState FindState(UnityEditor.Animations.AnimatorController controller, int stateHash)
        {
            foreach (var layer in controller.layers)
            {
                var match = FindStateByHash(layer.stateMachine, stateHash);
                if (match != null) return match;
            }

            if (stateHash == AnimationHashLib.Idle && controller.layers.Length > 0 && controller.layers[0].stateMachine.defaultState != null)
                return controller.layers[0].stateMachine.defaultState;

            string fallbackKeyword = stateHash == AnimationHashLib.Idle ? "Idle"
                : stateHash == AnimationHashLib.Hit ? "Hit"
                : stateHash == AnimationHashLib.Death ? "Death"
                : null;
            if (fallbackKeyword != null)
            {
                foreach (var layer in controller.layers)
                {
                    var match = FindStateByKeyword(layer.stateMachine, fallbackKeyword);
                    if (match != null) return match;
                }
            }

            return null;
        }

        private static UnityEditor.Animations.AnimatorState FindMoveState(UnityEditor.Animations.AnimatorController controller)
        {
            var idleState = FindState(controller, AnimationHashLib.Idle);
            if (idleState != null && idleState.transitions != null)
            {
                foreach (var transition in idleState.transitions)
                {
                    if (transition.destinationState == null || transition.conditions == null) continue;
                    foreach (var cond in transition.conditions)
                    {
                        if (Animator.StringToHash(cond.parameter) == AnimationHashLib.IsMoving)
                            return transition.destinationState;
                    }
                }
            }

            string[] keywords = { "Run", "Walk", "Move" };
            foreach (var keyword in keywords)
            {
                foreach (var layer in controller.layers)
                {
                    var match = FindStateByKeyword(layer.stateMachine, keyword);
                    if (match != null) return match;
                }
            }
            return null;
        }

        private static UnityEditor.Animations.AnimatorState FindStateByHash(UnityEditor.Animations.AnimatorStateMachine stateMachine, int stateHash)
        {
            if (stateMachine == null) return null;
            foreach (var child in stateMachine.states)
            {
                if (child.state != null && Animator.StringToHash(child.state.name) == stateHash)
                    return child.state;
            }
            foreach (var sub in stateMachine.stateMachines)
            {
                var found = FindStateByHash(sub.stateMachine, stateHash);
                if (found != null) return found;
            }
            return null;
        }

        private static UnityEditor.Animations.AnimatorState FindStateByKeyword(UnityEditor.Animations.AnimatorStateMachine stateMachine, string keyword)
        {
            if (stateMachine == null) return null;
            foreach (var child in stateMachine.states)
            {
                if (child.state != null && child.state.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return child.state;
            }
            foreach (var sub in stateMachine.stateMachines)
            {
                var found = FindStateByKeyword(sub.stateMachine, keyword);
                if (found != null) return found;
            }
            return null;
        }

        private static AnimationClip ExtractClip(Motion motion)
        {
            if (motion is AnimationClip clip) return clip;
            if (motion is UnityEditor.Animations.BlendTree blendTree)
            {
                foreach (var child in blendTree.children)
                {
                    var childClip = ExtractClip(child.motion);
                    if (childClip != null) return childClip;
                }
            }
            return null;
        }
#endif

        #endregion

        #region Game Loop & Input

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
                SetStatus("Đã hủy chọn.", StatusType.Info);
                if (currentTab == TestTab.Combat) RefreshCurrentTab();
            }

            if (reloading || worldCamera == null) return;

            // Kiểm tra con trỏ chuột trên giao diện UI Toolkit
            var pointer = RuntimePanelUtils.ScreenToPanel(panel.panel, new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            bool isOverUI = panel.worldBound.Contains(pointer);

            // Update footer hover coordinate
            if (!isOverUI)
            {
                var map = MapManager.Instance?.MapEntity;
                if (map != null)
                {
                    var plane = map.Settings.Plane();
                    var ray = worldCamera.ScreenPointToRay(Input.mousePosition);
                    if (plane.Raycast(ray, out var distance))
                    {
                        var hoverTile = map.Tile(ray.GetPoint(distance));
                        if (hoverTile != null && footerCoordLabel != null)
                        {
                            footerCoordLabel.text = $"Tile: {hoverTile.Position}";
                        }
                    }
                }
            }

            if (isOverUI || (!placingTarget && selectedSkill == null) || !Input.GetMouseButtonUp(0)) return;

            var activeMap = MapManager.Instance.MapEntity;
            var activePlane = activeMap.Settings.Plane();
            var activeRay = worldCamera.ScreenPointToRay(Input.mousePosition);
            if (!activePlane.Raycast(activeRay, out var hitDistance)) return;

            var targetTile = activeMap.Tile(activeRay.GetPoint(hitDistance));
            if (targetTile == null)
            {
                SetStatus("Vị trí click nằm ngoài phạm vi bản đồ.", StatusType.Warning);
                return;
            }

            // Đặt Target
            if (placingTarget)
            {
                if (!MapManager.Instance.IsTileAvailable(targetTile.Position))
                {
                    SetStatus("Tile đã bị chiếm hoặc không khả dụng.", StatusType.Warning);
                    return;
                }
                Spawn(targetIndex, PlayerID.Player2, targetTile.Position);
                CancelSelection();
                SetStatus("Đã đặt target Player 2 tại " + targetTile.Position, StatusType.Success);
                UpdateStats();
                return;
            }

            // Thi triển Skill
            if (caster == null)
            {
                SetStatus("Caster không còn tồn tại trên bàn cờ. Đổi nhân vật hoặc reload.", StatusType.Error);
                CancelSelection();
                return;
            }

            validationMessage = null;
            Application.logMessageReceived += CaptureValidation;
            try
            {
                var result = LocalMatchAuthority.SubmitHumanSkill(caster, selectedSkill, targetTile.Position);
                if (result.Succeeded)
                {
                    SetStatus($"Đã dùng thành công: {selectedSkill.SkillName}.", StatusType.Success);
                    CancelSelection();
                }
                else
                {
                    string err = validationMessage ?? result.FailureReason ?? "Thi triển kỹ năng thất bại.";
                    SetStatus(err, StatusType.Error);
                }
            }
            finally
            {
                Application.logMessageReceived -= CaptureValidation;
            }

            UpdateStats();
            if (currentTab == TestTab.Combat) RefreshCurrentTab();
        }

        private void CaptureValidation(string message, string trace, LogType type)
        {
            if (type == LogType.Warning || type == LogType.Error || type == LogType.Exception)
                validationMessage = message;
        }

        private void Reload()
        {
            if (reloading) return;
            CancelSelection();
#if UNITY_EDITOR
            reloading = true;
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SetStatus("Scene test chỉ hỗ trợ trong Unity Editor Play Mode.", StatusType.Warning);
#endif
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= CaptureValidation;
            if (animator != null)
            {
                animator.AnimationEventRaised -= OnAnimationEventRaised;
            }

            foreach (var skill in ownedSkills)
            {
                if (skill != null) Destroy(skill);
            }
            ownedSkills.Clear();
        }

        #endregion
    }
}
