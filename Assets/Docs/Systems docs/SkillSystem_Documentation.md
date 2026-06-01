# Skill System Documentation

- **Version**: 1.1
- **Updated**: 2026-05-27
- **Scope**: `Assets/Scripts/Skills`, `UnitAttack`, `UnitAnimator`, `SkillEffectRunner`, skill data assets.

---

## Kien Truc

Skill system hien tai dung cac pattern chinh:

| Pattern | Noi ap dung | Muc dich |
|---------|-------------|----------|
| Strategy | `ISkill`, `SkillBase`, concrete skill classes | Moi skill la mot strategy rieng, co the them skill moi bang class moi. |
| Template Method | `SkillBase.CanUse()` va `SkillBase.Execute()` | Gom validation/execution flow chung, concrete skill chi override logic rieng. |
| Observer | `SkillEventBus` | Tach logic skill khoi UI/VFX/Sound listener. |
| Data-Driven | `ScriptableObject` skill assets | Config range, cooldown, target, icon, VFX trong Inspector. |
| Bridge | `UnitSkillsBridge` | Noi `UnitAttack` voi UI `SkillButton` va object pool. |

### Thanh Phan Chinh

| Component | Vai tro |
|-----------|---------|
| `ISkill` | Contract cho skill: thong tin hien thi, cooldown, range, `CanUse`, `Execute`, clone. |
| `SkillBase` | Base `ScriptableObject`, chua validation pipeline, execution coroutine, cooldown, VFX config access. |
| `UnitAttack` | Quan ly danh sach skill runtime, selected skill, attack mode, click target va cooldown tick dau turn. |
| `UnitSkillsBridge` | Tao/cap nhat/huy skill buttons bang `ObjectPoolManager`. |
| `SkillButton` | Hien icon/description, forward click ve `UnitAttack`, khoa button khi cooldown > 0. |
| `UnitController` | Xoay unit ve target, goi animation skill, finish turn sau khi skill hoan tat. |
| `UnitAnimator` | Play animation theo `SkillType`, nhan Animation Event va forward cue sang `SkillEffectRunner`. |
| `SkillEffectRunner` | Spawn cast/release/projectile/impact VFX va goi `skill.ApplyEffect()` theo timing. |
| `SkillEventBus` | Singleton event bus cho `OnSkillUsed`, `OnSkillLearned`, `OnCooldownComplete`, `OnSkillFailed`. |

---

## Runtime Flow

1. `UnitAttack.Init()` clone tung skill trong `startingSkills`, reset cooldown va tao `SkillButton`.
2. Click skill button -> `_selectedSkill = skill` -> `EnterAttackMode()`.
3. `EnterAttackMode()` hien attack area bang `_selectedSkill.Range`.
4. Click tile trong attack mode -> `selectedSkill.CanUse(unit, tile.Position)`.
5. Neu hop le -> `selectedSkill.Execute(unit, tile.Position)`.
6. `SkillBase.Execute()` start coroutine `ExcuteAsync`.
7. `ExcuteAsync` luu execution context, goi `caster.PerformSkill(this, targetPos)`, start cooldown.
8. `UnitController.PerformSkill()` xoay unit ve target va goi `UnitAnimator.PlayAttack(skill)`.
9. Animation Event goi cue: Cast, Release, Impact, Complete.
10. `SkillEffectRunner` spawn VFX/projectile va goi `SkillBase.ApplyEffect()` khi dung timing.
11. `ApplyEffect()` start effect coroutine va goi `ExecuteEffectAsync(caster, targetPos)`.
12. Mac dinh `ExecuteEffectAsync()` fallback ve `ExecuteEffect()` cho instant skill.
13. Khi effect coroutine ket thuc, `OnExecuteComplete()` trigger `OnSkillUsed`, `FinishTurnActions()`.

`SkillBase` co timeout 10 giay trong giai doan cho `ApplyEffect()`. Neu animation/projectile khong bao gio goi `ApplyEffect()`, coroutine se log error va bat buoc ket thuc de khong treo turn. Sau khi `ApplyEffect()` da bat dau, effect coroutine duoc phep chay nhieu frame, dung cho channeling/damage-over-time skill.

---

## Validation Pipeline

`SkillBase.CanUse()` kiem tra theo thu tu:

1. `ValidateCooldown()`
2. `ValidateRange(caster, targetPos)`
3. `ValidateTarget(caster, targetPos)`
4. `ValidateCustomConditions(caster, targetPos)`

### Cooldown

- `SkillType.Normal` luon bo qua cooldown.
- Cac type khac chi dung duoc khi `currentCooldown <= 0`.
- `StartCooldown()` duoc goi ngay khi bat dau execute, truoc khi effect that su apply.
- `UnitController.OnTurnBegin()` -> `UnitAttack.ReduceSkillsCooldowns()` giam cooldown moi dau turn.
- `SkillEventBus.TriggerCooldownComplete()` da co API nhung hien chua duoc goi tu `ReduceCooldown()`.

### Range

`ValidateRange()` dung:

```csharp
MapManager.Instance.GetDistance(caster.currentGridPosition, targetPos) <= range
```

Vung hien thi attack mode trong `UnitAttack.EnterAttackMode()` dung `_cachedMap.WalkableBorder(currentTile, selectedSkill.Range)`.

### Target

`TargetType` la `[Flags]`:

| Flag | Gia tri | Y nghia |
|------|---------|---------|
| `Ally` | `1` | Target unit cung owner. |
| `Enemy` | `2` | Target unit khac owner. |
| `Self` | `4` | Target caster hoac unit da dead theo logic hien tai. |
| `EmptyTile` | `8` | Target o trong. |

Luu y: `ValidateTarget()` hien dang xem `targetUnit == caster || targetUnit.IsDead()` la dieu kien `Self`. Neu can phan biet self va dead unit, nen tach logic sau nay.

---

## VFX Va Animation Cue

`SkillVfxConfig` nam trong `SkillBase`:

| Field | Muc dich |
|-------|----------|
| `castVfxPrefab` | VFX luc bat dau cast. |
| `releaseVfxPrefab` | VFX luc release, vi du muzzle/slash. Neu prefab co component `Projectile`, no duoc xem nhu projectile fallback. |
| `projectilePrefab` | Projectile bay tu spawn point den target. Uu tien hon `releaseVfxPrefab` neu duoc set. |
| `impactVfxPrefab` | VFX tai target khi impact. |
| `effectApplyTiming` | `Automatic`, `OnAnimationImpact`, `OnProjectileImpact`, `OnRelease`. |
| `castSpawnPoint` | `Caster`, `EffectSpawnPoint`, hoac `Target`. |
| `releaseSpawnPoint` | `Caster`, `EffectSpawnPoint`, hoac `Target`. |
| `impactSpawnPoint` | `Caster`, `EffectSpawnPoint`, hoac `Target`. |

### Animation Events

Animation clip cua unit co the goi cac method sau tren `UnitAnimator`:

| Cue | Animation Event | Muc dich |
|-----|-----------------|----------|
| Cast | `AnimEvent_Cast()` hoac `AnimEvent_SkillCue("Cast")` | Spawn cast VFX. |
| Release | `AnimEvent_Release()`, `AnimEvent_AttackStart()` hoac `AnimEvent_SkillCue("Release")` | Spawn release VFX/projectile. |
| Impact | `AnimEvent_Impact()`, `AnimEvent_AttackHit()`, `AnimEvent_StartEffect()` hoac `AnimEvent_SkillCue("Impact")` | Spawn impact VFX va apply effect neu timing yeu cau. |
| Complete | `AnimEvent_AttackComplete()` hoac `AnimEvent_SkillCue("Complete")` | Cue ket thuc animation, hien chi forward sang runner. |

### Effect Timing

`SkillBase.ResolveEffectApplyTiming(hasProjectile)`:

- Neu `effectApplyTiming != Automatic`, dung gia tri config.
- Neu `Automatic` va co projectile -> `OnProjectileImpact`.
- Neu `Automatic` va khong co projectile -> `OnAnimationImpact`.

Current code notes:

- `OnRelease` goi `skill.ApplyEffect()` ngay tai release cue.
- `OnProjectileImpact` goi `skill.ApplyEffect()` trong callback `Projectile.OnReachTarget`.
- `OnAnimationImpact` la timing duoc resolve cho skill khong co projectile, nhung `SkillEffectRunner.HandleImpactCue()` hien chi spawn impact VFX va chua goi `skill.ApplyEffect()`. Neu dung melee/instant skill khong co projectile va khong set `OnRelease`, can cap nhat runner hoac config timing de tranh timeout.
- Cac field legacy nhu `VfxPrefab`, `VfxHitPrefab`, `HasDelayApplyEffect`, `DelayApplyEffectTime` van con trong mot so asset YAML cu, nhung khong con la field trong `SkillBase` hien tai.

---

## Cac Skill Hien Co

### Code Classes

| Class | Create Menu | Logic chinh |
|-------|-------------|-------------|
| `NormalAttackSkill` | `Skills/Normal Attack` | Damage 1 target: `caster.GetCurrentDamage() * multipleDmg`. |
| `FireballSkill` | `Skills/Fireball` | AOE damage enemy quanh target theo `aoeRadius`. Override `CanUse()` de bo qua `ValidateTarget()`, nen co the cast vao o trong. |
| `HealSkill` | `Skills/Heal` | Heal target theo `caster.GetCurrentDamage() * multiple`; chi dung khi target ton tai va chua full mau. |
| `TripleArrowSkill` | Chua co `CreateAssetMenu` | Dang la placeholder, `ExecuteEffect()` rong, chua co runtime logic. |
| `FlameThrowerSkill` | `Skills/Flame Thrower` | Damage lien tuc theo tick bang `ExecuteEffectAsync()`. |

### Skill Data Assets

| Asset | Class | Skill Name | Type | CD | Range | TargetTypes | Config rieng |
|-------|-------|------------|------|----|-------|-------------|--------------|
| `Normal arrow.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy + EmptyTile (`10`) | `multipleDmg = 1.25`, co projectile VFX. |
| `Slash attack.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy + EmptyTile (`10`) | `multipleDmg = 1.25`, VFX legacy con trong YAML. |
| `Axe whirl wind.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy (`2`) | `multipleDmg = 1.25`, VFX legacy con trong YAML. |
| `Heavy slash attack.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy (`2`) | `multipleDmg = 1.25`, VFX legacy con trong YAML. |
| `Triple strike arrow.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 | 1 | Enemy (`2`) | Hien van tro toi `NormalAttackSkill`, khong phai `TripleArrowSkill`. |
| `Fireball.asset` | `FireballSkill` | `FireBall` | `Ultimate` | 3 | 2 | Enemy + EmptyTile (`10`) | `multipleDmg = 1.25`, `aoeRadius = 2`. |
| `Heal skill.asset` | `HealSkill` | `Unnamed Skill` | `BuffAndDebuff` | 2 | 2 | Ally + Self + EmptyTile (`13`) | `multiple = 1.5`; custom validation yeu cau target unit ton tai va chua full HP. |

Khong con mana/cost trong interface va base class hien tai. Neu can cost, nen them vao contract/data flow rieng thay vi chi ghi trong doc.

---

## Tao Skill Moi

1. Tao class ke thua `SkillBase`.
2. Them `[CreateAssetMenu]` neu skill can tao asset tu Project window.
3. Override `ExecuteEffect(UnitController caster, Vector3Int targetPos)` cho instant skill.
4. Override `ExecuteEffectAsync(UnitController caster, Vector3Int targetPos)` cho skill can nhieu frame, vi du channeling hoac damage-over-time.
5. Override `ValidateCustomConditions()` neu co dieu kien rieng.
6. Chi override `CanUse()` khi that su can thay doi validation pipeline chung.
7. Tao asset skill trong Project, config `skillType`, `cooldown`, `range`, `targetTypes`, `vfxConfig`.
8. Gan asset vao `UnitAttack.startingSkills` cua prefab/unit.

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

## Luu Y Khi Setup Animation/VFX

- Skill dung projectile nen set `projectilePrefab` trong `SkillVfxConfig` va de `effectApplyTiming = Automatic` hoac `OnProjectileImpact`.
- Skill instant/melee nen set `effectApplyTiming = OnRelease` trong asset hien tai, tru khi `SkillEffectRunner.HandleImpactCue()` duoc cap nhat de apply effect o `OnAnimationImpact`.
- Animation attack phai co it nhat mot cue co the dan den `ApplyEffect()`. Neu khong, `SkillBase` se timeout sau 10 giay.
- `UnitAnimator.PlayAttack()` chon animation bang `AnimationHashLib.GetHashAnimByAttackType(skill.Type)`, vi vay `SkillType` trong asset anh huong truc tiep animation duoc play.

---

## Known Gaps / TODO

- `TripleArrowSkill` chua co logic va chua duoc data asset `Triple strike arrow.asset` su dung.
- `SkillEventBus.OnCooldownComplete` co event nhung chua duoc trigger tu cooldown reduction.
- `SkillEventBus.OnSkillFailed` co event nhung `UnitAttack` hien chi log `"Cannot use skill on this tile."`.
- `SkillEffectRunner.HandleImpactCue()` nen apply effect khi timing la `OnAnimationImpact`.
- Mot so asset cu con serialized field legacy khong con trong `SkillBase`; nen mo/save asset trong Unity sau khi migration de YAML sach hon.
