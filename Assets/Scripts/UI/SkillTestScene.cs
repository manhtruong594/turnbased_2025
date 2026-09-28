using System.Collections;
using System.Collections.Generic;
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
        private UnitController caster;
        private UnitAnimator animator;
        private Camera worldCamera;
        private SkillBase selectedSkill;
        private bool placingTarget;
        private bool reloading;
        private VisualElement panel;
        private Label status;
        private Label stats;
        private VisualElement skills;
        private string validationMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSelection() { characterIndex = 0; targetIndex = 0; }

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
            if (catalog == null) { status.text = "Thiếu MatchContentCatalog."; yield break; }
            foreach (var entry in catalog.Entries)
                if (entry != null && entry.UnitPrefab != null) characters.Add(entry);
            if (characters.Count == 0) { status.text = "Catalog chưa có character prefab."; yield break; }

            float deadline = Time.realtimeSinceStartup + 10f;
            while (GameMediator.Instance == null || !GameMediator.Instance.IsInitialized ||
                   TurnManager.Instance.CurrentState == TurnState.Initialization)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    status.text = "Khởi tạo thất bại. Kiểm tra Unity Console.";
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
            status.text = "Chọn loại target, bấm Đặt target lên tile rồi chọn ô trống.";
        }

        private UnitController Spawn(int index, PlayerID owner, Vector3Int tile)
        {
            // Fixture setup bypasses spawn cost only; skill commands still use gameplay authority.
            var entry = characters[index];
            var unit = Instantiate(entry.UnitPrefab, MapManager.Instance.MapEntity.WorldPosition(tile),
                Quaternion.identity, UnitSpawner.Instance.transform);
            unit.AssignMatchIdentity(LocalMatchAuthority.Runtime.AllocateUnit(unit), entry.Id);
            unit.Init(owner, tile);
            foreach (var skill in unit.AttackComponent.ActiveSkills)
                if (skill is SkillBase clone) ownedSkills.Add(clone);
            UnitSpawner.Instance.GetPlayerUnits(owner).Add(unit);
            unit.AttackComponent.enabled = false;
            return unit;
        }

        private void BuildPanel()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            panel = new ScrollView { name = "skill-test-panel" };
            panel.style.position = Position.Absolute;
            panel.style.left = 12;
            panel.style.top = 12;
            panel.style.bottom = 12;
            panel.style.width = 310;
            panel.style.paddingLeft = panel.style.paddingRight = 12;
            panel.style.paddingTop = panel.style.paddingBottom = 12;
            panel.style.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.97f);
            panel.style.color = Color.white;
            root.Add(panel);
            panel.Add(new Label("CHARACTER / SKILL TEST") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 18 } });
            status = new Label("Đang khởi tạo...");
            status.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(status);
        }

        private void BuildControls()
        {
            var names = new List<string>();
            foreach (var entry in characters) names.Add(entry.UnitPrefab.UnitData.unitName + " [" + entry.UnitPrefab.name + "]");
            var character = new DropdownField("Character (Player 1)", names, characterIndex);
            character.RegisterValueChangedCallback(_ => { characterIndex = character.index; Reload(); });
            panel.Add(character);
            var target = new DropdownField("Target (Player 2)", names, targetIndex);
            target.RegisterValueChangedCallback(_ => targetIndex = target.index);
            panel.Add(target);
            AddButton(panel, "Đặt target lên tile", BeginPlacement);
            AddButton(panel, "Làm caster mất HP (thử hồi máu)", PrepareWoundedCaster);
            AddButton(panel, "Gắn Slow lên caster (thử giải debuff)", PrepareDebuffedCaster);
            AddButton(panel, "Reset test", Reload);
            stats = new Label();
            stats.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(stats);
            var skillGuide = new Label("SKILLS — chọn rồi bấm tile. Scene test bỏ qua action, MP, cooldown; vùng tô chỉ tham khảo đường đi.");
            skillGuide.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(skillGuide);
            skills = new VisualElement();
            panel.Add(skills);
            foreach (var item in caster.AttackComponent.ActiveSkills)
            {
                if (item is not SkillBase skill || skill.Type == SkillType.Passive) continue;
                AddButton(skills, $"{skill.SkillName} | MP {skill.MPCost} | Range {skill.Range} | {DescribeTargets(skill)}", () => SelectSkill(skill));
            }
            panel.Add(new Label("ANIMATION — không áp dụng effect"));
            AddButton(panel, "Idle", () => Preview(AnimationHashLib.Idle));
            AddButton(panel, "Move (tại chỗ)", () => { Preview(AnimationHashLib.Idle); if (animator != null) animator.StartMoving(); });
            AddButton(panel, "Hit", () => Preview(AnimationHashLib.Hit));
            AddButton(panel, "Death", () => Preview(AnimationHashLib.Death));
            foreach (var item in caster.AttackComponent.ActiveSkills)
                if (item is SkillBase skill && skill.Type != SkillType.Passive)
                    AddButton(panel, "Animation: " + skill.SkillName, () => Preview(AnimationHashLib.GetHashAnimByAttackType(skill.Type)));
            panel.schedule.Execute(UpdateStats).Every(200);
            UpdateStats();
        }

        private static void AddButton(VisualElement parent, string text, System.Action action)
        {
            var button = new Button(action) { text = text };
            button.style.minHeight = 28;
            button.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(button);
        }

        private void UpdateStats()
        {
            if (stats == null || MPManager.Instance == null) return;
            string value = $"MP: {MPManager.Instance.GetCurrentMP(PlayerID.Player1)}/{MPManager.Instance.MaxMP}";
            if (caster != null)
            {
                value += $" | HP: {caster.GetCurrentHealth()}/{caster.GetMaxHealth()}\nHành động: {(caster.CanAct() ? "sẵn sàng" : "đã dùng / bị khóa — scene test vẫn cho dùng skill")}";
                if (caster.BuffHandler != null && caster.BuffHandler.HasEffect(StatusEffectType.Slow))
                    value += " | Debuff: Slow";
                foreach (var skill in caster.AttackComponent.ActiveSkills)
                    value += $"\n{skill.SkillName}: cooldown {skill.CurrentCooldown}";
            }
            foreach (var target in UnitSpawner.Instance.GetPlayerUnits(PlayerID.Player2))
                if (target != null) value += $"\n{target.UnitData.unitName}: HP {target.GetCurrentHealth()}/{target.GetMaxHealth()}";
            stats.text = value;
        }

        private void BeginPlacement()
        {
            CancelSelection();
            placingTarget = true;
            status.text = "Bấm tile trống để đặt target Player 2. Esc để hủy.";
        }

        private void PrepareWoundedCaster()
        {
            CancelSelection();
            if (caster == null || caster.IsDead()) { status.text = "Caster đã chết. Nhấn Reset test."; return; }
            if (caster.GetCurrentHealth() <= 1) { status.text = "Caster không thể mất thêm HP. Nhấn Reset test."; return; }
            caster.TakeNonLethalDamage(Mathf.Max(1, caster.GetMaxHealth() / 2));
            status.text = "Caster đã mất HP. Chọn skill hồi máu rồi bấm tile của caster (-1, 0, 0).";
            UpdateStats();
        }

        private void PrepareDebuffedCaster()
        {
            CancelSelection();
            if (caster == null || caster.IsDead()) { status.text = "Caster đã chết. Nhấn Reset test."; return; }
            if (caster.BuffHandler == null) { status.text = "Caster không có BuffDebuffHandler."; return; }
            caster.BuffHandler.AddEffect(new ActiveStatusEffect(StatusEffectType.Slow, 25, 2, PlayerID.Player2));
            status.text = "Caster đã có Slow. Chọn skill giải debuff rồi bấm tile của caster (-1, 0, 0).";
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
            if (skill is HealSkill) return " Mục tiêu phải mất HP.";
            if (skill is HerbalistCleanseSkill) return " Mục tiêu phải có debuff.";
            if (skill is AssassinShadowDualStrikeSkill) return " Cần target địch và ô trống cạnh target để dịch chuyển.";
            if (skill is AdjudicatorForbiddenSealSkill) return " Cần ít nhất một địch trong vùng ảnh hưởng.";
            return string.Empty;
        }

        private void SelectSkill(SkillBase skill)
        {
            CancelSelection();
            if (caster == null || caster.IsDead()) { status.text = "Caster đã chết. Nhấn Reset test."; return; }
            selectedSkill = skill;
            AreaPathManager.Instance.ShowAttackArea(MapManager.Instance.MapEntity.WalkableBorder(caster.currentGridPosition, skill.Range));
            status.text = skill.SkillName + ": chọn " + DescribeTargets(skill) + "." + DescribeCondition(skill) +
                          " Vùng tô chỉ tham khảo; Esc để hủy.";
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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { CancelSelection(); if (status != null) status.text = "Đã hủy chọn."; }
            if (reloading || worldCamera == null || (!placingTarget && selectedSkill == null) || !Input.GetMouseButtonUp(0)) return;
            var pointer = RuntimePanelUtils.ScreenToPanel(panel.panel, new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            if (panel.worldBound.Contains(pointer)) return;
            var map = MapManager.Instance.MapEntity;
            var plane = map.Settings.Plane();
            var ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            if (!plane.Raycast(ray, out var distance)) return;
            var tile = map.Tile(ray.GetPoint(distance));
            if (tile == null) { status.text = "Vị trí ngoài map."; return; }
            if (placingTarget)
            {
                if (!MapManager.Instance.IsTileAvailable(tile.Position)) { status.text = "Tile đã bị chiếm hoặc không khả dụng."; return; }
                Spawn(targetIndex, PlayerID.Player2, tile.Position);
                CancelSelection();
                status.text = "Đã đặt target tại " + tile.Position;
                return;
            }
            if (caster == null) { status.text = "Caster không còn tồn tại. Nhấn Reset test."; CancelSelection(); return; }
            validationMessage = null;
            Application.logMessageReceived += CaptureValidation;
            try
            {
                var result = LocalMatchAuthority.SubmitHumanSkill(caster, selectedSkill, tile.Position);
                status.text = result.Succeeded ? "Đã dùng " + selectedSkill.SkillName + ". Có thể dùng tiếp hoặc tạo thêm target."
                    : validationMessage ?? result.FailureReason;
                if (result.Succeeded) CancelSelection();
            }
            finally { Application.logMessageReceived -= CaptureValidation; }
            UpdateStats();
        }

        private void CaptureValidation(string message, string trace, LogType type)
        {
            if (type == LogType.Warning || type == LogType.Error || type == LogType.Exception) validationMessage = message;
        }

        private void Reload()
        {
            if (reloading) return;
            CancelSelection();
#if UNITY_EDITOR
            reloading = true;
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            status.text = "Scene test chỉ dùng trong Unity Editor.";
#endif
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= CaptureValidation;
            // Runtime skill clones belong to this fixture, including units already destroyed by death.
            foreach (var skill in ownedSkills) if (skill != null) Destroy(skill);
        }

        private readonly List<SkillBase> ownedSkills = new();
    }
}
