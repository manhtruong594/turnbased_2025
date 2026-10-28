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

        private static int characterIndex;
        private static int targetIndex;

        private readonly List<MatchContentCatalog.Entry> characters = new();
        private readonly List<SkillBase> ownedSkills = new();
        private readonly Dictionary<SkillBase, VisualElement> skillItemMap = new();

        private UnitController caster;
        private UnitAnimator animator;
        private Camera worldCamera;
        private SkillBase selectedSkill;
        private bool placingTarget;
        private bool reloading;
        private bool isCollapsed;

        private VisualElement panel;
        private ScrollView scrollContainer;
        private VisualElement footerContainer;
        private Label status;
        private Label stats;
        private VisualElement skillsContainer;
        private Button collapseBtn;

        private readonly StringBuilder statsBuilder = new(256);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSelection()
        {
            characterIndex = 0;
            targetIndex = 0;
        }

        private void Awake()
        {
            BattleLaunchContext.Clear();
            MatchContext.ConfigureLocalPvP();
            // The manager survives scene reloads, while its pooled objects do not.
            TurnBasedGame.ObjectPool.ObjectPoolManager.Instance?.ClearAllPools();
        }

        private IEnumerator Start()
        {
            BuildPanel();
            var catalog = UnityEngine.Resources.Load<MatchContentCatalog>(MatchContentCatalog.ResourceName);
            if (catalog == null)
            {
                SetStatus("Thiếu MatchContentCatalog.");
                yield break;
            }

            foreach (var entry in catalog.Entries)
            {
                if (entry != null && entry.UnitPrefab != null)
                    characters.Add(entry);
            }

            if (characters.Count == 0)
            {
                SetStatus("Catalog chưa có character prefab.");
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + 10f;
            while (GameMediator.Instance == null || !GameMediator.Instance.IsInitialized ||
                   TurnManager.Instance == null || TurnManager.Instance.CurrentState == TurnState.Initialization)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    SetStatus("Khởi tạo thất bại: Timeout sau 10s (GameMediator/TurnManager chưa sẵn sàng). Kiểm tra Unity Console.");
                    yield break;
                }
                yield return null;
            }

            // Keep the real turn state, but stop automatic timeout in this isolated test scene.
            TurnManager.Instance.enabled = false;
            AreaPathManager.Instance.enabled = false;
            worldCamera = Camera.main;
            characterIndex = Mathf.Clamp(characterIndex, 0, characters.Count - 1);
            targetIndex = Mathf.Clamp(targetIndex, 0, characters.Count - 1);

            caster = Spawn(characterIndex, PlayerID.Player1, new Vector3Int(-1, 0, 0));
            caster.IgnoreSkillUseLimitsForTest = true;
            animator = caster.GetComponentInChildren<UnitAnimator>();

            BuildControls();
            SetStatus("Chọn loại target, bấm [Đặt target] rồi click ô trống trên sàn.");
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
                if (skill is SkillBase clone)
                    ownedSkills.Add(clone);
            }

            UnitSpawner.Instance.GetPlayerUnits(owner).Add(unit);
            unit.AttackComponent.enabled = false;
            return unit;
        }

        #region UI Construction
        private void BuildPanel()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;

            // Outer Shell
            panel = new VisualElement { name = "skill-test-panel" };
            panel.style.position = Position.Absolute;
            panel.style.left = 12;
            panel.style.top = 12;
            panel.style.bottom = 12;
            panel.style.width = 330;
            panel.style.backgroundColor = new Color(0.09f, 0.11f, 0.15f, 0.96f);
            panel.style.borderTopWidth = panel.style.borderBottomWidth = panel.style.borderLeftWidth = panel.style.borderRightWidth = 1;
            panel.style.borderTopColor = panel.style.borderBottomColor = panel.style.borderLeftColor = panel.style.borderRightColor = new Color(1f, 1f, 1f, 0.12f);
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 8;
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.overflow = Overflow.Hidden;
            root.Add(panel);

            // 1. Header Toolbar
            var header = new VisualElement { name = "header-toolbar" };
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = header.style.paddingRight = 10;
            header.style.paddingTop = header.style.paddingBottom = 8;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);

            var titleLabel = new Label("SKILL & ACTOR TEST")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    color = Color.white
                }
            };
            header.Add(titleLabel);

            var headerActions = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var resetBtn = CreateStyledButton("↺ Reset", Reload, new Color(0.55f, 0.15f, 0.15f, 0.85f), Color.white);
            resetBtn.style.fontSize = 10;
            resetBtn.style.minHeight = 22;
            resetBtn.style.marginBottom = 0;
            resetBtn.style.marginRight = 6;
            resetBtn.style.paddingLeft = resetBtn.style.paddingRight = 6;
            headerActions.Add(resetBtn);

            collapseBtn = CreateStyledButton("—", ToggleCollapse, new Color(1f, 1f, 1f, 0.1f), Color.white);
            collapseBtn.style.fontSize = 12;
            collapseBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            collapseBtn.style.minHeight = 22;
            collapseBtn.style.minWidth = 24;
            collapseBtn.style.marginBottom = 0;
            collapseBtn.style.paddingLeft = collapseBtn.style.paddingRight = 4;
            headerActions.Add(collapseBtn);

            header.Add(headerActions);
            panel.Add(header);

            // 2. Scrollable Body
            scrollContainer = new ScrollView { name = "body-scroll" };
            scrollContainer.style.flexGrow = 1;
            scrollContainer.style.flexShrink = 1;
            scrollContainer.style.paddingLeft = scrollContainer.style.paddingRight = 10;
            scrollContainer.style.paddingTop = scrollContainer.style.paddingBottom = 8;
            panel.Add(scrollContainer);

            // 3. Sticky Footer Status Bar
            footerContainer = new VisualElement { name = "footer-status" };
            footerContainer.style.backgroundColor = new Color(0.06f, 0.08f, 0.1f, 0.98f);
            footerContainer.style.borderTopWidth = 1;
            footerContainer.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
            footerContainer.style.paddingLeft = footerContainer.style.paddingRight = 10;
            footerContainer.style.paddingTop = footerContainer.style.paddingBottom = 8;
            footerContainer.style.minHeight = 34;
            footerContainer.style.justifyContent = Justify.Center;

            status = new Label("Đang khởi tạo...")
            {
                style =
                {
                    fontSize = 11,
                    whiteSpace = WhiteSpace.Normal,
                    color = new Color(0.85f, 0.88f, 0.92f, 1f)
                }
            };
            footerContainer.Add(status);
            panel.Add(footerContainer);
        }

        private void ToggleCollapse()
        {
            isCollapsed = !isCollapsed;
            scrollContainer.style.display = isCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
            footerContainer.style.display = isCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
            collapseBtn.text = isCollapsed ? "+" : "—";
            panel.style.bottom = isCollapsed ? StyleKeyword.Auto : 12;
        }

        private string GetCharacterDisplayName(int index)
        {
            if (characters.Count == 0 || index < 0 || index >= characters.Count) return "Trống";
            var entry = characters[index];
            var dataName = entry.UnitPrefab != null && entry.UnitPrefab.UnitData != null
                ? entry.UnitPrefab.UnitData.unitName
                : "Unit";
            var prefabName = entry.UnitPrefab != null ? entry.UnitPrefab.name : entry.Id;
            return $"{dataName} [{prefabName}]";
        }

        private VisualElement CreateActorSelector(string roleLabel, int currentIndex, System.Action<int> onDeltaChanged, out System.Action<string> updateLabelAction)
        {
            var container = new VisualElement { style = { marginBottom = 6 } };

            var label = new Label(roleLabel)
            {
                style =
                {
                    fontSize = 11,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.75f, 0.8f, 0.88f, 1f),
                    marginBottom = 2
                }
            };
            container.Add(label);

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var nameLabel = new Label(GetCharacterDisplayName(currentIndex))
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    fontSize = 11,
                    color = Color.white,
                    backgroundColor = new Color(1f, 1f, 1f, 0.05f),
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = new Color(1f, 1f, 1f, 0.12f), borderBottomColor = new Color(1f, 1f, 1f, 0.12f),
                    borderLeftColor = new Color(1f, 1f, 1f, 0.12f), borderRightColor = new Color(1f, 1f, 1f, 0.12f),
                    borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                    paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 4,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    whiteSpace = WhiteSpace.NoWrap,
                    overflow = Overflow.Hidden
                }
            };

            var prevBtn = CreateStyledButton("◀", () => onDeltaChanged(-1), new Color(1f, 1f, 1f, 0.08f), Color.white);
            prevBtn.style.minWidth = 26;
            prevBtn.style.minHeight = 24;
            prevBtn.style.marginBottom = 0;
            prevBtn.style.marginRight = 4;
            prevBtn.style.paddingLeft = prevBtn.style.paddingRight = 0;

            var nextBtn = CreateStyledButton("▶", () => onDeltaChanged(1), new Color(1f, 1f, 1f, 0.08f), Color.white);
            nextBtn.style.minWidth = 26;
            nextBtn.style.minHeight = 24;
            nextBtn.style.marginBottom = 0;
            nextBtn.style.marginLeft = 4;
            nextBtn.style.paddingLeft = nextBtn.style.paddingRight = 0;

            row.Add(prevBtn);
            row.Add(nameLabel);
            row.Add(nextBtn);
            container.Add(row);

            updateLabelAction = text => nameLabel.text = text;
            return container;
        }

        private void BuildControls()
        {
            // Card 1: Actor Setup
            var setupCard = CreateCard("ACTOR SETUP");

            var casterSelector = CreateActorSelector("Caster (P1)", characterIndex, delta =>
            {
                if (characters.Count == 0) return;
                characterIndex = (characterIndex + delta + characters.Count) % characters.Count;
                Reload();
            }, out _);
            setupCard.Add(casterSelector);

            System.Action<string> updateTargetLabel = null;
            var targetSelector = CreateActorSelector("Target (P2)", targetIndex, delta =>
            {
                if (characters.Count == 0) return;
                targetIndex = (targetIndex + delta + characters.Count) % characters.Count;
                updateTargetLabel?.Invoke(GetCharacterDisplayName(targetIndex));
            }, out updateTargetLabel);
            targetSelector.style.marginBottom = 8;
            setupCard.Add(targetSelector);

            var actionRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var placeTargetBtn = CreateStyledButton("Đặt target", BeginPlacement, new Color(0.16f, 0.38f, 0.65f, 1f), Color.white);
            placeTargetBtn.style.flexGrow = 1;
            placeTargetBtn.style.marginRight = 4;
            actionRow.Add(placeTargetBtn);

            var woundBtn = CreateStyledButton("-50% HP", PrepareWoundedCaster, new Color(1f, 1f, 1f, 0.08f), Color.white);
            woundBtn.style.flexGrow = 1;
            woundBtn.style.marginRight = 4;
            actionRow.Add(woundBtn);

            var slowBtn = CreateStyledButton("+Slow", PrepareDebuffedCaster, new Color(1f, 1f, 1f, 0.08f), Color.white);
            slowBtn.style.flexGrow = 1;
            actionRow.Add(slowBtn);

            setupCard.Add(actionRow);
            scrollContainer.Add(setupCard);

            // Card 2: Live Monitor
            var monitorCard = CreateCard("LIVE MONITOR (STATS)");
            stats = new Label
            {
                style =
                {
                    fontSize = 11,
                    whiteSpace = WhiteSpace.Normal,
                    color = new Color(0.8f, 0.85f, 0.9f, 1f)
                }
            };
            monitorCard.Add(stats);
            scrollContainer.Add(monitorCard);

            // Card 3: Active Skills
            var skillsCard = CreateCard("ACTIVE SKILLS");
            skillsContainer = new VisualElement();
            skillsCard.Add(skillsContainer);

            skillItemMap.Clear();
            foreach (var item in caster.AttackComponent.ActiveSkills)
            {
                if (item is not SkillBase skill || skill.Type == SkillType.Passive) continue;
                var skillItem = CreateSkillItem(skill);
                skillItemMap[skill] = skillItem;
                skillsContainer.Add(skillItem);
            }
            scrollContainer.Add(skillsCard);

            // Card 4: Animation Inspection
            var animCard = CreateCard("ANIMATION INSPECTION");
            var animFoldout = new Foldout { text = "Danh sách động tác", value = false };
            animFoldout.style.unityFontStyleAndWeight = FontStyle.Bold;
            animFoldout.style.fontSize = 11;
            animFoldout.style.color = new Color(0.75f, 0.8f, 0.88f, 1f);

            AddAnimationRow(animFoldout, "Idle", () => Preview(AnimationHashLib.Idle), AnimationHashLib.Idle, false);
            AddAnimationRow(animFoldout, "Move (tại chỗ)", () => { Preview(AnimationHashLib.Idle); if (animator != null) animator.StartMoving(); }, AnimationHashLib.Idle, true);
            AddAnimationRow(animFoldout, "Hit", () => Preview(AnimationHashLib.Hit), AnimationHashLib.Hit, false);
            AddAnimationRow(animFoldout, "Death", () => Preview(AnimationHashLib.Death), AnimationHashLib.Death, false);

            foreach (var item in caster.AttackComponent.ActiveSkills)
            {
                if (item is SkillBase skill && skill.Type != SkillType.Passive)
                {
                    int hash = AnimationHashLib.GetHashAnimByAttackType(skill.Type);
                    AddAnimationRow(animFoldout, "Skill: " + skill.SkillName, () => Preview(hash), hash, false);
                }
            }
            animCard.Add(animFoldout);
            scrollContainer.Add(animCard);

            // Auto-refresh stats smoothly
            panel.schedule.Execute(UpdateStats).Every(250);
            UpdateStats();
        }

        private VisualElement CreateSkillItem(SkillBase skill)
        {
            var itemBtn = new Button(() => SelectSkill(skill));
            itemBtn.style.paddingLeft = itemBtn.style.paddingRight = 8;
            itemBtn.style.paddingTop = itemBtn.style.paddingBottom = 6;
            itemBtn.style.marginBottom = 6;
            itemBtn.style.backgroundColor = new Color(1f, 1f, 1f, 0.05f);
            itemBtn.style.borderTopLeftRadius = itemBtn.style.borderTopRightRadius =
                itemBtn.style.borderBottomLeftRadius = itemBtn.style.borderBottomRightRadius = 4;
            itemBtn.style.borderTopWidth = itemBtn.style.borderBottomWidth = itemBtn.style.borderLeftWidth = itemBtn.style.borderRightWidth = 1;
            itemBtn.style.borderTopColor = itemBtn.style.borderBottomColor = itemBtn.style.borderLeftColor = itemBtn.style.borderRightColor = new Color(1f, 1f, 1f, 0.12f);
            itemBtn.style.flexDirection = FlexDirection.Column;
            itemBtn.style.alignItems = Align.Stretch;

            // Row 1: Skill Name + MP/Range Badges
            var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
            var nameLabel = new Label(skill.SkillName)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12, color = Color.white }
            };
            topRow.Add(nameLabel);

            var badgeRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var mpBadge = new Label($"MP {skill.MPCost}")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.4f, 0.8f, 1f),
                    backgroundColor = new Color(0.1f, 0.3f, 0.5f, 0.5f),
                    paddingLeft = 4, paddingRight = 4, paddingTop = 1, paddingBottom = 1,
                    marginRight = 4,
                    borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3
                }
            };
            var rangeBadge = new Label($"R {skill.Range}")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.9f, 0.8f, 0.4f),
                    backgroundColor = new Color(0.4f, 0.35f, 0.1f, 0.5f),
                    paddingLeft = 4, paddingRight = 4, paddingTop = 1, paddingBottom = 1,
                    borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3
                }
            };
            badgeRow.Add(mpBadge);
            badgeRow.Add(rangeBadge);
            topRow.Add(badgeRow);
            itemBtn.Add(topRow);

            // Row 2: Target description & Condition
            string desc = DescribeTargets(skill) + DescribeCondition(skill);
            var subLabel = new Label(desc)
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.7f, 0.72f, 0.75f, 0.9f),
                    marginTop = 3,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            itemBtn.Add(subLabel);

            return itemBtn;
        }

        private static VisualElement CreateCard(string title = null)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(1f, 1f, 1f, 0.04f);
            card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderLeftWidth = card.style.borderRightWidth = 1;
            card.style.borderTopColor = card.style.borderBottomColor = card.style.borderLeftColor = card.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 6;
            card.style.paddingLeft = card.style.paddingRight = 8;
            card.style.paddingTop = card.style.paddingBottom = 8;
            card.style.marginBottom = 8;

            if (!string.IsNullOrEmpty(title))
            {
                var titleLabel = new Label(title)
                {
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Bold,
                        fontSize = 11,
                        color = new Color(0.75f, 0.8f, 0.88f, 1f),
                        marginBottom = 6
                    }
                };
                card.Add(titleLabel);
            }
            return card;
        }

        private static Button CreateStyledButton(string text, System.Action action, Color? bgColor = null, Color? textColor = null)
        {
            var button = new Button(action) { text = text };
            button.style.minHeight = 26;
            button.style.marginBottom = 4;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius =
                button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 4;
            button.style.borderTopWidth = button.style.borderBottomWidth = button.style.borderLeftWidth = button.style.borderRightWidth = 1;
            button.style.borderTopColor = button.style.borderBottomColor = button.style.borderLeftColor = button.style.borderRightColor = new Color(1f, 1f, 1f, 0.12f);

            if (bgColor.HasValue)
                button.style.backgroundColor = bgColor.Value;
            if (textColor.HasValue)
                button.style.color = textColor.Value;

            return button;
        }

        private void AddAnimationRow(VisualElement parent, string text, System.Action previewAction, int stateHash, bool isMove)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Stretch, marginBottom = 4 } };
            var previewButton = CreateStyledButton(text, previewAction, new Color(1f, 1f, 1f, 0.06f), Color.white);
            previewButton.style.flexGrow = 1;
            previewButton.style.flexShrink = 1;
            previewButton.style.marginBottom = 0;
            previewButton.style.marginRight = 4;
            previewButton.style.fontSize = 11;

            var pingButton = CreateStyledButton("Ping", () => PingAnimation(text, stateHash, isMove), new Color(1f, 1f, 1f, 0.1f), Color.white);
            pingButton.style.minWidth = 44;
            pingButton.style.flexShrink = 0;
            pingButton.style.marginBottom = 0;
            pingButton.style.fontSize = 10;

            row.Add(previewButton);
            row.Add(pingButton);
            parent.Add(row);
        }

        private void SetStatus(string message)
        {
            if (status != null)
                status.text = message;
        }
        #endregion

        #region State & Logic
        private void UpdateStats()
        {
            if (stats == null || MPManager.Instance == null) return;
            statsBuilder.Clear();

            // 1. Caster stats
            if (caster != null && !caster.IsDead())
            {
                statsBuilder.Append("• Caster: ").Append(caster.UnitData != null ? caster.UnitData.unitName : "Hero");
                statsBuilder.Append(" | HP: ").Append(caster.GetCurrentHealth()).Append('/').Append(caster.GetMaxHealth());
                statsBuilder.Append(" | MP: ").Append(MPManager.Instance.GetCurrentMP(PlayerID.Player1)).Append('/').Append(MPManager.Instance.MaxMP);
                if (caster.BuffHandler != null && caster.BuffHandler.HasEffect(StatusEffectType.Slow))
                    statsBuilder.Append(" [SLOW]");

                bool hasCooldown = false;
                foreach (var skill in caster.AttackComponent.ActiveSkills)
                {
                    if (skill.CurrentCooldown > 0)
                    {
                        if (!hasCooldown) { statsBuilder.Append("\n  Cooldown: "); hasCooldown = true; }
                        else statsBuilder.Append(", ");
                        statsBuilder.Append(skill.SkillName).Append('(').Append(skill.CurrentCooldown).Append(')');
                    }
                }
            }
            else
            {
                statsBuilder.Append("• Caster: Đã chết hoặc chưa khởi tạo.");
            }

            // 2. Target units stats
            statsBuilder.Append("\n• Targets (Player 2):");
            if (UnitSpawner.Instance != null)
            {
                var targets = UnitSpawner.Instance.GetPlayerUnits(PlayerID.Player2);
                if (targets.Count == 0)
                {
                    statsBuilder.Append(" Chưa có (bấm Đặt target)");
                }
                else
                {
                    foreach (var target in targets)
                    {
                        if (target != null && !target.IsDead())
                        {
                            statsBuilder.Append("\n  - ").Append(target.UnitData.unitName)
                                .Append(" @").Append(target.currentGridPosition)
                                .Append(" | HP: ").Append(target.GetCurrentHealth()).Append('/').Append(target.GetMaxHealth());
                        }
                    }
                }
            }

            string newText = statsBuilder.ToString();
            if (stats.text != newText)
                stats.text = newText;
        }

        private void BeginPlacement()
        {
            CancelSelection();
            placingTarget = true;
            SetStatus("Click tile trống trên sàn để đặt target Player 2. Nhấn Esc để hủy.");
        }

        private void PrepareWoundedCaster()
        {
            CancelSelection();
            if (caster == null || caster.IsDead()) { SetStatus("Caster đã chết. Nhấn Reset."); return; }
            if (caster.GetCurrentHealth() <= 1) { SetStatus("Caster không thể mất thêm HP."); return; }
            caster.TakeNonLethalDamage(Mathf.Max(1, caster.GetMaxHealth() / 2));
            SetStatus("Caster đã mất HP. Chọn skill hồi máu rồi click tile (-1, 0, 0).");
            UpdateStats();
        }

        private void PrepareDebuffedCaster()
        {
            CancelSelection();
            if (caster == null || caster.IsDead()) { SetStatus("Caster đã chết. Nhấn Reset."); return; }
            if (caster.BuffHandler == null) { SetStatus("Caster không có BuffDebuffHandler."); return; }
            caster.BuffHandler.AddEffect(new ActiveStatusEffect(StatusEffectType.Slow, 25, 2, PlayerID.Player2));
            SetStatus("Caster đã có Slow. Chọn skill giải debuff rồi click tile (-1, 0, 0).");
            UpdateStats();
        }

        private static string DescribeTargets(SkillBase skill)
        {
            if (skill is FireballSkill) return "ô trong range (kể cả ô trống)";
            var targets = new List<string>(4);
            if (skill.CanTargetEnemies) targets.Add("địch");
            if (skill.CanTargetAllies) targets.Add("đồng minh");
            if (skill.CanTargetSelf) targets.Add("bản thân");
            if (skill.CanTargetEmptyTile) targets.Add("ô trống");
            return targets.Count > 0 ? string.Join(", ", targets) : "điều kiện riêng";
        }

        private static string DescribeCondition(SkillBase skill)
        {
            if (skill is HealSkill) return " • Mục tiêu phải mất HP.";
            if (skill is HerbalistCleanseSkill) return " • Mục tiêu phải có debuff.";
            if (skill is AssassinShadowDualStrikeSkill) return " • Cần target địch & ô trống cạnh bên.";
            if (skill is AdjudicatorForbiddenSealSkill) return " • Cần địch trong vùng ảnh hưởng.";
            if (!string.IsNullOrEmpty(skill.Description) && skill.Description != "Không có mô tả")
                return " • " + skill.Description;
            return string.Empty;
        }

        private void SelectSkill(SkillBase skill)
        {
            CancelSelection();
            if (caster == null || caster.IsDead()) { SetStatus("Caster đã chết. Nhấn Reset."); return; }

            selectedSkill = skill;
            HighlightSelectedSkill(skill);

            AreaPathManager.Instance.ShowAttackArea(MapManager.Instance.MapEntity.WalkableBorder(caster.currentGridPosition, skill.Range));
            SetStatus(skill.SkillName + ": chọn " + DescribeTargets(skill) + "." + DescribeCondition(skill) + " Vùng tô chỉ tham khảo; Esc để hủy.");
        }

        private void HighlightSelectedSkill(SkillBase skill)
        {
            foreach (var kvp in skillItemMap)
            {
                if (kvp.Key == skill)
                {
                    kvp.Value.style.borderTopColor = kvp.Value.style.borderBottomColor =
                        kvp.Value.style.borderLeftColor = kvp.Value.style.borderRightColor = new Color(0.23f, 0.51f, 0.96f, 1f);
                    kvp.Value.style.borderTopWidth = kvp.Value.style.borderBottomWidth =
                        kvp.Value.style.borderLeftWidth = kvp.Value.style.borderRightWidth = 2;
                }
                else
                {
                    kvp.Value.style.borderTopColor = kvp.Value.style.borderBottomColor =
                        kvp.Value.style.borderLeftColor = kvp.Value.style.borderRightColor = new Color(1f, 1f, 1f, 0.12f);
                    kvp.Value.style.borderTopWidth = kvp.Value.style.borderBottomWidth =
                        kvp.Value.style.borderLeftWidth = kvp.Value.style.borderRightWidth = 1;
                }
            }
        }

        private void CancelSelection()
        {
            selectedSkill = null;
            placingTarget = false;
            HighlightSelectedSkill(null);
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
                SetStatus("Không tìm thấy animation asset cho " + label + ".");
                return;
            }

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(clip);
            UnityEditor.EditorUtility.FocusProjectWindow();
            UnityEditor.Selection.activeObject = clip;
            UnityEditor.EditorGUIUtility.PingObject(clip);
            SetStatus(string.IsNullOrEmpty(assetPath)
                ? "Đã ping animation: " + clip.name
                : "Đã ping animation: " + clip.name + " (" + assetPath + ")");
#else
            SetStatus("Chức năng ping chỉ dùng trong Unity Editor.");
#endif
        }

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
                if (child.state != null && child.state.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
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

        #region Input & Frame Loop
        private static Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        private static bool WasPointerActionReleased()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonUp(0);
#else
            return false;
#endif
        }

        private static bool WasCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private void Update()
        {
            if (WasCancelPressed())
            {
                CancelSelection();
                SetStatus("Đã hủy chọn.");
            }

            if (reloading || worldCamera == null || (!placingTarget && selectedSkill == null) || !WasPointerActionReleased())
                return;

            var mousePos = GetPointerScreenPosition();
            if (panel != null && panel.panel != null)
            {
                var panelPointer = RuntimePanelUtils.ScreenToPanel(panel.panel, new Vector2(mousePos.x, Screen.height - mousePos.y));
                if (panel.panel.Pick(panelPointer) != null)
                    return;
            }

            var map = MapManager.Instance.MapEntity;
            var plane = map.Settings.Plane();
            var ray = worldCamera.ScreenPointToRay(mousePos);
            if (!plane.Raycast(ray, out var distance)) return;

            var tile = map.Tile(ray.GetPoint(distance));
            if (tile == null)
            {
                SetStatus("Vị trí click ngoài map.");
                return;
            }

            if (placingTarget)
            {
                if (!MapManager.Instance.IsTileAvailable(tile.Position))
                {
                    SetStatus("Tile " + tile.Position + " đã bị chiếm hoặc không khả dụng.");
                    return;
                }

                Spawn(targetIndex, PlayerID.Player2, tile.Position);
                CancelSelection();
                SetStatus("Đã đặt target tại " + tile.Position);
                UpdateStats();
                return;
            }

            if (caster == null)
            {
                SetStatus("Caster không còn tồn tại. Nhấn Reset.");
                CancelSelection();
                return;
            }

            var result = LocalMatchAuthority.SubmitHumanSkill(caster, selectedSkill, tile.Position);
            SetStatus(result.Succeeded
                ? $"Đã dùng {selectedSkill.SkillName} thành công. Bấm tiếp để dùng lại hoặc chọn skill khác."
                : (!string.IsNullOrEmpty(result.FailureReason) ? result.FailureReason : "Không thể thực thi kỹ năng."));

            if (result.Succeeded)
                CancelSelection();

            UpdateStats();
        }

        private void Reload()
        {
            if (reloading) return;
            CancelSelection();
#if UNITY_EDITOR
            reloading = true;
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SetStatus("Scene test chỉ dùng trong Unity Editor.");
#endif
        }

        private void OnDestroy()
        {
            // Runtime skill clones belong to this fixture, including units already destroyed by death.
            foreach (var skill in ownedSkills)
            {
                if (skill != null)
                    Destroy(skill);
            }
            ownedSkills.Clear();
            skillItemMap.Clear();
        }
        #endregion
    }
}
