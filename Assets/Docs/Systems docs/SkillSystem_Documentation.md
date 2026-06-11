# Skill System Documentation

- **Version**: 1.1
- **Updated**: 2026-06-10
- **Scope**: `Assets/Scripts/Skills`, `UnitAttack`, `UnitAnimator`, `SkillEffectRunner`, skill data assets.

---

## Kiến Trúc

Skill system hiện tại dùng các pattern chính:

| Pattern | Nơi áp dụng | Mục đích |
|---------|-------------|----------|
| Strategy | `ISkill`, `SkillBase`, concrete skill classes | Mỗi skill là một strategy riêng, có thể thêm skill mới bằng class mới. |
| Template Method | `SkillBase.CanUse()` và `SkillBase.Execute()` | Gom validation/execution flow chung, concrete skill chỉ override logic riêng. |
| Observer | `SkillEventBus` | Tách logic skill khỏi UI/VFX/Sound listener. |
| Data-Driven | `ScriptableObject` skill assets | Config range, cooldown, target, icon, VFX trong Inspector. |
| Bridge | `UnitSkillsBridge` | Nối `UnitAttack` với UI `SkillButton` và object pool. |

### Thành Phần Chính

| Component | Vai trò |
|-----------|---------|
| `ISkill` | Contract cho skill: thông tin hiển thị, cooldown, range, `CanUse`, `Execute`, clone. |
| `SkillBase` | Base `ScriptableObject`, chứa validation pipeline, execution coroutine, cooldown, VFX config access. |
| `UnitAttack` | Quản lý danh sách skill runtime, selected skill, attack mode, click target và cooldown tick đầu turn. |
| `UnitSkillsBridge` | Tạo/cập nhật/hủy skill buttons bằng `ObjectPoolManager`. |
| `SkillButton` | Hiện icon/description, forward click về `UnitAttack`, khóa button khi cooldown > 0. |
| `UnitController` | Xoay unit về target, gọi animation skill, finish turn sau khi skill hoàn tất. |
| `UnitAnimator` | Play animation theo `SkillType`, nhận Animation Event và forward cue sang `SkillEffectRunner`. |
| `SkillEffectRunner` | Spawn cast/release/projectile/impact VFX và gọi `skill.ApplyEffect()` theo timing. |
| `SkillEventBus` | Singleton event bus cho `OnSkillUsed`, `OnSkillLearned`, `OnCooldownComplete`, `OnSkillFailed`. |

---

## Runtime Flow

1. `UnitAttack.Init()` clone từng skill trong `startingSkills`, reset cooldown và tạo `SkillButton`.
2. Click skill button -> `_selectedSkill = skill` -> `EnterAttackMode()`.
3. `EnterAttackMode()` hiện attack area bằng `_selectedSkill.Range`.
4. Click tile trong attack mode -> `selectedSkill.CanUse(unit, tile.Position)`.
5. Nếu hợp lệ -> `selectedSkill.Execute(unit, tile.Position)`.
6. `SkillBase.Execute()` start coroutine `ExcuteAsync`.
7. `ExcuteAsync` lưu execution context, gọi `caster.PerformSkill(this, targetPos)`, start cooldown.
8. `UnitController.PerformSkill()` xoay unit về target và gọi `UnitAnimator.PlayAttack(skill)`.
9. Animation Event gọi cue: Cast, Release, Impact, Complete.
10. `SkillEffectRunner` spawn VFX/projectile và gọi `SkillBase.ApplyEffect()` khi đúng timing.
11. `ApplyEffect()` start effect coroutine và gọi `ExecuteEffectAsync(caster, targetPos)`.
12. Mặc định `ExecuteEffectAsync()` fallback về `ExecuteEffect()` cho instant skill.
13. Khi effect coroutine kết thúc, `OnExecuteComplete()` trigger `OnSkillUsed`, `FinishTurnActions()`.

`SkillBase` có timeout 10 giây trong giai đoạn chờ `ApplyEffect()`. Nếu animation/projectile không bao giờ gọi `ApplyEffect()`, coroutine sẽ log error và bắt buộc kết thúc để không treo turn. Sau khi `ApplyEffect()` đã bắt đầu, effect coroutine được phép chạy nhiều frame, dùng cho channeling/damage-over-time skill.

---

## Validation Pipeline

`SkillBase.CanUse()` kiểm tra theo thứ tự:

1. `ValidateCooldown()`
2. `ValidateRange(caster, targetPos)`
3. `ValidateTarget(caster, targetPos)`
4. `ValidateCustomConditions(caster, targetPos)`

### Cooldown

- `SkillType.Normal` luôn bỏ qua cooldown.
- Các type khác chỉ dùng được khi `currentCooldown <= 0`.
- `StartCooldown()` được gọi ngay khi bắt đầu execute, trước khi effect thật sự apply.
- `UnitController.OnTurnBegin()` -> `UnitAttack.ReduceSkillsCooldowns()` giảm cooldown mỗi đầu turn.
- `SkillEventBus.TriggerCooldownComplete()` đã có API nhưng hiện chưa được gọi từ `ReduceCooldown()`.

### Range

`ValidateRange()` dùng:

```csharp
MapManager.Instance.GetDistance(caster.currentGridPosition, targetPos) <= range
```

Vùng hiển thị attack mode trong `UnitAttack.EnterAttackMode()` dùng `_cachedMap.WalkableBorder(currentTile, selectedSkill.Range)`.

### Target

`TargetType` là `[Flags]`:

| Flag | Giá trị | Ý nghĩa |
|------|---------|---------|
| `Ally` | `1` | Target unit cùng owner. |
| `Enemy` | `2` | Target unit khác owner. |
| `Self` | `4` | Target caster hoặc unit đã dead theo logic hiện tại. |
| `EmptyTile` | `8` | Target ô trống. |

Lưu ý: `ValidateTarget()` hiện đang xem `targetUnit == caster || targetUnit.IsDead()` là điều kiện `Self`. Nếu cần phân biệt self và dead unit, nên tách logic sau này.

---

## VFX Và Animation Cue

`SkillVfxConfig` nằm trong `SkillBase`:

| Field | Mục đích |
|-------|----------|
| `castVfxPrefab` | VFX lúc bắt đầu cast. |
| `releaseVfxPrefab` | VFX lúc release, ví dụ muzzle/slash. Nếu prefab có component `Projectile`, nó được xem như projectile fallback. |
| `projectilePrefab` | Projectile bay từ spawn point đến target. Ưu tiên hơn `releaseVfxPrefab` nếu được set. |
| `impactVfxPrefab` | VFX tại target khi impact. |
| `effectApplyTiming` | `Automatic`, `OnAnimationImpact`, `OnProjectileImpact`, `OnRelease`. |
| `castSpawnPoint` | `Caster`, `EffectSpawnPoint`, hoặc `Target`. |
| `releaseSpawnPoint` | `Caster`, `EffectSpawnPoint`, hoặc `Target`. |
| `impactSpawnPoint` | `Caster`, `EffectSpawnPoint`, hoặc `Target`. |

### Animation Events

Animation clip của unit có thể gọi các method sau trên `UnitAnimator`:

| Cue | Animation Event | Mục đích |
|-----|-----------------|----------|
| Cast | `AnimEvent_Cast()` hoặc `AnimEvent_SkillCue("Cast")` | Spawn cast VFX. |
| Release | `AnimEvent_Release()`, `AnimEvent_AttackStart()` hoặc `AnimEvent_SkillCue("Release")` | Spawn release VFX/projectile. |
| Impact | `AnimEvent_Impact()`, `AnimEvent_AttackHit()`, `AnimEvent_StartEffect()` hoặc `AnimEvent_SkillCue("Impact")` | Spawn impact VFX và apply effect nếu timing yêu cầu. |
| Complete | `AnimEvent_AttackComplete()` hoặc `AnimEvent_SkillCue("Complete")` | Cue kết thúc animation, hiện chỉ forward sang runner. |

### Effect Timing

`SkillBase.ResolveEffectApplyTiming(hasProjectile)`:

- Nếu `effectApplyTiming != Automatic`, dùng giá trị config.
- Nếu `Automatic` và có projectile -> `OnProjectileImpact`.
- Nếu `Automatic` và không có projectile -> `OnAnimationImpact`.

Current code notes:

- `OnRelease` gọi `skill.ApplyEffect()` ngay tại release cue.
- `OnProjectileImpact` gọi `skill.ApplyEffect()` trong callback `Projectile.OnReachTarget`.
- `OnAnimationImpact` gọi `skill.ApplyEffect()` trong `SkillEffectRunner.HandleImpactCue()` sau khi spawn impact VFX, dùng được cho melee/instant skill có animation impact cue.
- Các field legacy như `VfxPrefab`, `VfxHitPrefab`, `HasDelayApplyEffect`, `DelayApplyEffectTime` vẫn còn trong một số asset YAML cũ, nhưng không còn là field trong `SkillBase` hiện tại.

---

## Các Skill Hiện Có

### Code Classes

| Class | Create Menu | Logic chính |
|-------|-------------|-------------|
| `NormalAttackSkill` | `Skills/Normal Attack` | Damage 1 target: `caster.GetCurrentDamage() * multipleDmg`. |
| `FireballSkill` | `Skills/Fireball` | AOE damage enemy quanh target theo `aoeRadius`. Override `CanUse()` để bỏ qua `ValidateTarget()`, nên có thể cast vào ô trống. |
| `HealSkill` | `Skills/Heal` | Heal target theo `caster.GetCurrentDamage() * multiple`; chỉ dùng khi target tồn tại và chưa full máu. |
| `TripleArrowSkill` | Chưa có `CreateAssetMenu` | Đang là placeholder, `ExecuteEffect()` rỗng, chưa có runtime logic. |
| `FlameThrowerSkill` | `Skills/Flame Thrower` | Damage liên tục theo tick bằng `ExecuteEffectAsync()`. |

### Skill Data Assets

| Asset | Class | Skill Name | Type | CD | Range | TargetTypes | Config riêng |
|-------|-------|------------|------|----|-------|-------------|--------------|
| `Normal arrow.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy + EmptyTile (`10`) | `multipleDmg = 1.25`, có projectile VFX. |
| `Slash attack.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy + EmptyTile (`10`) | `multipleDmg = 1.25`, VFX legacy còn trong YAML. |
| `Axe whirl wind.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy (`2`) | `multipleDmg = 1.25`, VFX legacy còn trong YAML. |
| `Heavy slash attack.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy (`2`) | `multipleDmg = 1.25`, VFX legacy còn trong YAML. |
| `Triple strike arrow.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy (`2`) | Hiện vẫn trỏ tới `NormalAttackSkill`, không phải `TripleArrowSkill`. |
| `Fireball.asset` | `FireballSkill` | `FireBall` | `Ultimate` | 3 | 2 | Enemy + EmptyTile (`10`) | `multipleDmg = 1.25`, `aoeRadius = 2`. |
| `Heal skill.asset` | `HealSkill` | `Unnamed Skill` | `BuffAndDebuff` | 2 | 2 | Ally + Self + EmptyTile (`13`) | `multiple = 1.5`; custom validation yêu cầu target unit tồn tại và chưa full HP. |

Không còn mana/cost trong interface và base class hiện tại. Nếu cần cost, nên thêm vào contract/data flow riêng thay vì chỉ ghi trong doc.

---

## Tạo Skill Mới

1. Tạo class kế thừa `SkillBase`.
2. Thêm `[CreateAssetMenu]` nếu skill cần tạo asset từ Project window.
3. Override `ExecuteEffect(UnitController caster, Vector3Int targetPos)` cho instant skill.
4. Override `ExecuteEffectAsync(UnitController caster, Vector3Int targetPos)` cho skill cần nhiều frame, ví dụ channeling hoặc damage-over-time.
5. Override `ValidateCustomConditions()` nếu có điều kiện riêng.
6. Chỉ override `CanUse()` khi thật sự cần thay đổi validation pipeline chung.
7. Tạo asset skill trong Project, config `skillType`, `cooldown`, `range`, `targetTypes`, `vfxConfig`.
8. Gán asset vào `UnitAttack.startingSkills` của prefab/unit.

Template:

```csharp
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/New Skill")]
    public class NewSkill : SkillBase
    {
        [SerializeField] private float damageMultiplier = 1f;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead()) return;

            int damage = Mathf.RoundToInt(caster.GetCurrentDamage() * damageMultiplier);
            target.TakeDamage(damage);
        }
    }
}
```

---

## Lưu Ý Khi Setup Animation/VFX

- Skill dùng projectile nên set `projectilePrefab` trong `SkillVfxConfig` và để `effectApplyTiming = Automatic` hoặc `OnProjectileImpact`.
- Skill instant/melee có thể set `effectApplyTiming = OnRelease` để apply sớm ở release cue, hoặc `OnAnimationImpact` nếu animation clip có impact cue ổn định.
- Animation attack phải có ít nhất một cue có thể dẫn đến `ApplyEffect()`. Nếu không, `SkillBase` sẽ timeout sau 10 giây.
- `UnitAnimator.PlayAttack()` chọn animation bằng `AnimationHashLib.GetHashAnimByAttackType(skill.Type)`, vì vậy `SkillType` trong asset ảnh hưởng trực tiếp animation được play.

---

## Known Gaps / TODO

- `TripleArrowSkill` chưa có logic và chưa được data asset `Triple strike arrow.asset` sử dụng.
- `SkillEventBus.OnCooldownComplete` có event nhưng chưa được trigger từ cooldown reduction.
- `SkillEventBus.OnSkillFailed` có event nhưng `UnitAttack` hiện chỉ log `"Cannot use skill on this tile."`.
- Một số asset cũ còn serialized field legacy không còn trong `SkillBase`; nên mở/save asset trong Unity sau khi migration để YAML sạch hơn.
