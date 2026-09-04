# Skill System

## 1. Kiến trúc

Skill dùng Strategy + Template Method:

- `ISkill` định nghĩa metadata, cooldown, range, `CanUse`, `Execute`, reset/reduce cooldown và clone.
- `SkillBase` là `ScriptableObject` chứa validation/execution pipeline chung.
- Các class skill cụ thể override validation/effect/VFX config khi cần.
- `UnitAttack` giữ các skill runtime của unit và điều phối attack mode.
- `UnitAnimator` phát animation cue.
- `SkillEffectRunner` tạo cast/release/projectile/impact effect theo cue.
- `SkillEventBus` phát used, learned, cooldown complete và failed event.

Mỗi unit phải dùng clone runtime của skill data. Không ghi cooldown trực tiếp lên asset dùng chung giữa
nhiều unit.

## 2. Data contract

### `ISkill`

- Metadata: `SkillName`, `Description`, `SkillType`, `Icon`.
- Rule: `Cooldown`, `CurrentCooldown`, `Range`.
- Action: `CanUse`, `Execute`, `ResetCooldown`, `ReduceCooldown`, `Clone`.

### Loại skill

- `Normal`: đòn thường, thường không cooldown.
- `Active`: kỹ năng chủ động.
- `Passive`: kỹ năng bị động; cần trigger riêng nếu sử dụng.
- `Ultimate`: kỹ năng mạnh với giới hạn riêng.
- `BuffAndDebuff`: kỹ năng thay đổi trạng thái.

### Target mask

`TargetType` là flags: `Ally`, `Enemy`, `Self`, `EmptyTile`. Skill có thể kết hợp nhiều flag.

## 3. Validation pipeline

Thứ tự cần giữ:

1. Caster tồn tại và chưa chết.
2. Đúng lượt/owner và caster còn action nếu luật yêu cầu.
3. Cooldown bằng 0.
4. Target position tồn tại trên map.
5. Khoảng cách không vượt range.
6. Unit/tile tại đích khớp target mask.
7. Điều kiện riêng của skill: line, area, HP threshold, immunity, tile vacant.

Validation thất bại không được áp effect, tăng cooldown hoặc hoàn tất action. Failure reason nên đi qua
event/UI thay vì chỉ log.

## 4. Execution pipeline

```text
CanUse
  └── cache caster/target
        └── phát skill used
              └── animation cue
                    ├── Cast
                    ├── Release
                    ├── Projectile
                    └── Impact / ApplyEffect
                          └── cooldown + action complete
```

`SkillEffectApplyTiming` quyết định effect áp ngay, tại release/impact hoặc sau projectile. Mỗi đường
thực thi phải có fallback hoàn tất khi animation event/VFX không tồn tại; đồng thời tránh áp effect hai
lần nếu cả fallback và event cùng chạy.

## 5. Skill code hiện có

| Class | Vai trò chính |
|---|---|
| `NormalAttackSkill` | Đòn đánh cơ bản |
| `HealSkill` | Hồi HP |
| `FireballSkill` | Projectile/damage từ xa |
| `MultiArrowSkill` | Nhiều hit/mũi tên |
| `FlameThrowerSkill` | Sát thương theo line/vùng |
| `BerserkerBloodHammerSkill` | Bruiser damage/status |
| `KnightHolySwordStanceSkill` | Stance phòng thủ |
| `MagicianFireSealSkill` | Hỏa ấn/hazard vùng |
| `SmasherEarthquakeSkill` | Area damage/displacement |
| `HalberdierCrescentSlashSkill` | Melee area/line |
| `AssassinBleedSkill` | Bleed |
| `AssassinShadowDualStrikeSkill` | Tiếp cận và đánh kép |

Data assets nằm chủ yếu trong `Assets/Scripts/Data/SkillData`. Một số tên asset cũ không trùng hoàn
toàn tên class; không đổi tên serialized asset nếu chưa kiểm tra reference.

## 6. Utility liên quan

- `SkillAreaUtility`: radius, line, adjacent tile và unit query.
- `DisplacementUtility`: push và kết quả moved/blocked/immune.
- `TeleportUtility`: tìm tile hợp lệ quanh target.
- `TileHazardManager`: lưu hazard, tick theo lượt, enter-tile và turn-start effect.
- `BuffDebuffHandler`: status, immunity, damage modifier và duration.

## 7. Tạo skill mới

1. Xác định vai trò, target, range, cooldown, timing và edge case.
2. Tái sử dụng utility/status hiện có trước khi tạo hệ thống mới.
3. Tạo class kế thừa `SkillBase` nếu effect không thể cấu hình từ class có sẵn.
4. Override phần nhỏ nhất cần thiết; giữ validation chung.
5. Tạo asset bằng `CreateAssetMenu`, cấu hình icon/VFX/animation cue.
6. Gán asset vào unit prefab/data theo flow hiện tại.
7. Test valid/invalid target, cooldown, action completion và target death.

## 8. Known gaps

| Mức | Vấn đề |
|---|---|
| P0 | `ReduceCooldown` chưa phát cooldown-complete event khi về 0 |
| P0 | Failure từ `UnitAttack` chưa nối đầy đủ với `SkillEventBus` |
| P0 | Target chết/mất giữa effect sequence cần regression test |
| P1 | `UnitAttack` còn TODO cho line-of-sight đúng nghĩa |
| P1 | Button cooldown/icon fallback cần kiểm chứng trong scene |
| P2 | Legacy serialized field trong asset cũ cần migration có kiểm soát |

## 9. Ma trận kiểm thử

Mỗi skill đang phát hành cần kiểm tra:

- Caster/target đúng và sai phe.
- Min/max/out-of-range.
- Cooldown 0 và đang cooldown.
- Tile trống/blocked theo target rule.
- Target chết trước cast, giữa projectile và tại impact.
- Animation event có/không có.
- VFX prefab có/không có; object trả pool đúng.
- Damage/heal/status đúng một lần.
- Action và UI trở về trạng thái đúng sau success/failure.
