# Kế Hoạch Implement Skill Các Character Còn Lại

## Phạm Vi

Plan này áp dụng cho các character còn lại trong `Characters_Overview.md`, không bao gồm Archer và không lặp lại Berserker/Knight đã có logic riêng.

Danh sách cần implement:

- Magician - `Hỏa Ấn Thiên Trụ`
- Smasher - `Địa Chấn Toái Sơn`
- Halberdier - `Nguyệt Nha Trảm Trận`
- Assassin - `Ảnh Bộ Song Sát`

Mục tiêu kỹ thuật:

- Tiếp tục dùng `SkillBase` theo Strategy/Template Method.
- Tách logic dùng chung thành helper/effect nhỏ khi có thể.
- Không viết hàm quá dài; nếu hàm trên 50 dòng thì thêm comment mô tả phía trên.
- Ưu tiên status effect có thể tái sử dụng thay vì hard-code riêng trong từng skill.
- Skill melee/instant nên cấu hình `effectApplyTiming = OnRelease` cho tới khi `SkillEffectRunner` hỗ trợ đầy đủ `OnAnimationImpact`.

---

## Giai Đoạn 1 - Nền Tảng Dùng Chung

### 1. Chuẩn hóa status effect

Thêm hoặc xác nhận các `StatusEffectType` sau:

| Effect | Loại | Mục đích |
|---|---|---|
| `Burn` | Debuff | Damage-over-time cho Magician. Đã có, cần kiểm tra balance/value. |
| `BurningGround` | Tile hazard | Vùng lửa tồn tại nhiều turn, không nên nhét vào `BuffDebuffHandler` của unit. |
| `Stun` | Debuff | Dùng cho Smasher khi va vào obstacle/unit. Đã có. |
| `HealBan` | Debuff | Cấm hồi máu cho Halberdier. |
| `Shield` | Buff | Chặn damage cho Halberdier. Đã có. |
| `ShadowStep` | Buff | Trạng thái tàng hình/không thể bị chọn làm target trực tiếp cho Assassin. |

Việc cần làm:

- Thêm `HealBan` và `ShadowStep` vào `StatusEffectType`.
- Cập nhật `StatusEffectTypeExtensions.IsDebuff()`.
- Cập nhật `BuffDebuffHandler` với API:
  - `HasHealBan()`
  - `IsUntargetableDirectly()`
  - `CanReceiveHealing()`
- Chặn heal trong `UnitController.Heal()` nếu target đang có `HealBan`.
- Cập nhật `SkillBase.ValidateTarget()` hoặc `UnitAttack` để không cho chọn target có `ShadowStep` bằng skill single-target trực tiếp, nhưng vẫn cho AOE gây damage.

### 2. Tạo helper tính vùng ảnh hưởng

Nên thêm helper tĩnh, ví dụ `SkillAreaUtility`, để tránh lặp logic trong từng skill:

- `GetRadiusTiles(Vector3Int center, int radius)`
- `GetLineTiles(Vector3Int origin, Vector3Int direction, int length)`
- `GetAdjacentTiles(Vector3Int center)`
- `GetEnemiesInTiles(PlayerID casterOwner, IEnumerable<Vector3Int> tiles)`
- `GetFirstEnemyInLine(...)`

Lợi ích:

- Magician dùng radius.
- Smasher dùng target tile và hướng knockback.
- Halberdier dùng line 2 ô.
- Assassin dùng các ô kề target để teleport.

### 3. Tạo hệ thống tile hazard tối thiểu

Magician cần `BurningGround`, nên cần một component/service riêng, ví dụ:

- `TileHazardManager`
- `TileHazardInstance`
- `TileHazardType`

API đề xuất:

- `AddHazard(TileHazardType type, Vector3Int position, int duration, PlayerID owner, int value)`
- `TickHazards(PlayerID activePlayer)` hoặc tick theo cuối round/tick global.
- `TryApplyEnterTileEffect(UnitController unit, Vector3Int tilePos)`
- `TryApplyTurnStartEffect(UnitController unit)`

Điểm tích hợp:

- `UnitController.OnTurnBegin()` gọi hazard đầu turn cho unit.
- `UnitController.UpdateGridPosition()` hoặc `GameMediator.OnUnitMoved` gọi hazard khi unit bước vào tile.

---

## Giai Đoạn 2 - Magician: Hỏa Ấn Thiên Trụ

### Skill class

Tạo `MagicianFireSealSkill : SkillBase`.

Config đề xuất:

| Field | Giá trị mặc định |
|---|---|
| `damageMultiplier` | `1.4f` |
| `aoeRadius` | `1` hoặc `2` |
| `burnChanceOnImpact` | `0.25f` |
| `burnChanceFromGround` | `0.5f` |
| `burnDuration` | `2` |
| `burnDamagePerTurn` | tùy balance, ví dụ `caster damage * 0.4` |
| `burningGroundDuration` | `2` |
| `bonusDamageAgainstBurningTargetPercent` | `25` |

Logic:

1. Cho phép cast vào enemy hoặc empty tile.
2. Lấy toàn bộ tile trong radius.
3. Với mỗi enemy trong vùng:
   - Nếu target đang có `Burn`, tăng damage thêm 25%.
   - Gây damage `caster.GetCurrentDamage() * damageMultiplier`.
   - Roll 25% để apply `Burn` trong 2 turn.
4. Tạo `BurningGround` trên các tile trong vùng trong 2 turn.
5. Enemy đứng trên `BurningGround` ở đầu turn hoặc khi bước vào có 50% nhận `Burn`.

Asset cần tạo:

- `Assets/Scripts/Data/SkillData/Magician Fire Seal.asset`
- `skillType = Ultimate`
- `cooldown = 5`
- `range = 3`
- `targetTypes = Enemy | EmptyTile`
- `effectApplyTiming = OnRelease` hoặc `OnProjectileImpact` nếu có projectile.

Acceptance test:

- Cast vào empty tile vẫn chạy.
- Enemy trong radius nhận damage.
- Enemy đang Burn nhận thêm 25% initial damage.
- Hazard tồn tại đúng 2 turn.
- Bước vào hazard có roll Burn.

---

## Giai Đoạn 3 - Smasher: Địa Chấn Toái Sơn

### Skill class

Tạo `SmasherEarthquakeSkill : SkillBase`.

Config đề xuất:

| Field | Giá trị mặc định |
|---|---|
| `damageMultiplier` | `1.8f` |
| `knockbackDistance` | `1` |
| `collisionDamageMultiplier` | `0.5f` |
| `stunDuration` | `1` |

Logic:

1. Cho phép cast vào enemy hoặc empty tile trong range 1.
2. Enemy trên target tile nhận damage `caster damage * 1.8`.
3. Tính hướng knockback từ caster tới target.
4. Nếu tile sau lưng target trống và walkable:
   - Di chuyển enemy sang tile đó.
5. Nếu không thể knockback do obstacle/unit/map edge:
   - Gây thêm collision damage.
   - Apply `Stun` 1 turn.
6. Nếu target có `StanceGuard` hoặc `IsImmuneToDisplacement()` thì không bị knockback, nhưng vẫn nhận damage chính.

Helper cần có:

- `DisplacementUtility.TryPushUnit(UnitController unit, Vector3Int direction, int distance)`
- Kiểm tra occupied tile bằng `MapManager.GetUnitAtTile()`.
- Kiểm tra walkable nếu `MapEntity` có API phù hợp; nếu chưa có, tạm thời chỉ check tile tồn tại và không có unit.

Asset cần tạo:

- `Assets/Scripts/Data/SkillData/Smasher Earthquake.asset`
- `skillType = Ultimate`
- `cooldown = 4`
- `range = 1`
- `targetTypes = Enemy | EmptyTile`
- `effectApplyTiming = OnRelease`

Acceptance test:

- Target nhận damage chính.
- Target bị đẩy 1 tile nếu tile sau trống.
- Va obstacle/unit thì nhận thêm damage và Stun.
- Target có `StanceGuard` không bị đẩy.

---

## Giai Đoạn 4 - Halberdier: Nguyệt Nha Trảm Trận

### Skill class

Tạo `HalberdierCrescentSlashSkill : SkillBase`.

Config đề xuất:

| Field | Giá trị mặc định |
|---|---|
| `lineLength` | `2` |
| `primaryDamageMultiplier` | `1.7f` |
| `secondaryDamageMultiplier` | `1.0f` |
| `healBanDuration` | `2` |
| `shieldValue` | tùy balance, ví dụ `caster damage` hoặc số cố định |
| `shieldDuration` | `2` |

Logic:

1. Tính line 2 ô theo hướng từ caster tới target.
2. Enemy đầu tiên trong line nhận damage chính `1.7x`.
3. Enemy các ô còn lại nhận damage phụ `1.0x`.
4. Nếu chỉ có 1 target trúng hoặc target chính đang đứng trên capture point:
   - Apply `HealBan` cho target chính trong 2 turn.
5. Caster nhận `Shield` trong 2 turn.

Asset cần tạo:

- `Assets/Scripts/Data/SkillData/Halberdier Crescent Slash.asset`
- `skillType = Ultimate`
- `cooldown = 4`
- `range = 2`
- `targetTypes = Enemy`
- `effectApplyTiming = OnRelease`

Acceptance test:

- Line 2 ô đúng hướng target.
- Target đầu tiên nhận damage cao hơn target phụ.
- `HealBan` chặn `UnitController.Heal()`.
- Caster nhận Shield và Shield giảm damage qua `ModifyIncomingDamage()`.

---

## Giai Đoạn 5 - Assassin: Ảnh Bộ Song Sát

### Skill class

Tạo `AssassinShadowDualStrikeSkill : SkillBase`.

Ghi chú: `AssassinBleedSkill` hiện có là skill bleed đơn giản, chưa phản ánh ultimate trong docs. Có thể giữ làm skill phụ hoặc thay asset ultimate bằng class mới.

Config đề xuất:

| Field | Giá trị mặc định |
|---|---|
| `range` | `3` |
| `firstHitMultiplier` | `0.9f` |
| `secondHitMultiplier` | `0.9f` |
| `executeSecondHitMultiplier` | `1.4f` |
| `executeHealthThresholdPercent` | `0.4f` |
| `shadowStepDuration` | `1` |

Logic:

1. Validate target enemy trong range 3.
2. Tìm ô teleport hợp lệ quanh target:
   - Ưu tiên ô phía sau target theo hướng caster -> target.
   - Nếu không có, chọn ô bên cạnh target.
   - Nếu không có ô hợp lệ, không cho cast.
3. Teleport Assassin tới ô hợp lệ.
4. Hit 1 gây `caster damage * 0.9`.
5. Trước hit 2, nếu target dưới 40% HP:
   - Hit 2 dùng `caster damage * 1.4`.
   - Ngược lại dùng `caster damage * 0.9`.
6. Nếu target chết:
   - Apply `ShadowStep` trong 1 turn.
7. `ShadowStep` làm Assassin không thể bị chọn làm target trực tiếp, nhưng vẫn nhận AOE.

Helper cần có:

- `TeleportUtility.TryFindAdjacentTileAroundTarget(...)`
- `UnitController.TeleportTo(Vector3Int gridPos)` hoặc dùng `UpdateGridPosition()` kèm set transform world position.

Asset cần tạo:

- `Assets/Scripts/Data/SkillData/Assassin Shadow Dual Strike.asset`
- `skillType = Ultimate`
- `cooldown = 4`
- `range = 3`
- `targetTypes = Enemy`
- `effectApplyTiming = OnRelease`

Acceptance test:

- Không cast được nếu không có ô teleport hợp lệ quanh target.
- Hit 2 tăng damage khi target dưới 40% HP trước hit 2.
- Hạ target thì Assassin nhận `ShadowStep`.
- Unit có `ShadowStep` không bị chọn bởi single-target skill, nhưng vẫn nhận AOE.

---

## Thứ Tự Implement Đề Xuất

1. Thêm status/API chung: `HealBan`, `ShadowStep`, `CanReceiveHealing()`, `IsUntargetableDirectly()`.
2. Thêm `SkillAreaUtility`.
3. Implement Halberdier trước vì ít phụ thuộc nhất và test được `HealBan/Shield`.
4. Implement Smasher để hoàn thiện displacement và dùng sẵn `IsImmuneToDisplacement()`.
5. Implement Assassin vì cần teleport và target rule cho `ShadowStep`.
6. Implement Magician cuối vì cần `TileHazardManager`, là phần có nhiều integration nhất.

Lý do:

- Halberdier giúp kiểm tra status mới với ít rủi ro.
- Smasher tạo nền cho displacement, sau này dùng được cho nhiều skill.
- Assassin phụ thuộc teleport và untargetable rule.
- Magician cần hazard theo turn và theo movement, nên nên làm sau khi các hook movement/turn đã rõ.

---

## Checklist Hoàn Thành

### Code

- [x] `StatusEffectType` có đủ `HealBan`, `ShadowStep`.
- [x] `BuffDebuffHandler` có API query effect cần thiết.
- [x] `UnitController.Heal()` tôn trọng `HealBan`.
- [x] Single-target validation tôn trọng `ShadowStep`.
- [x] Có helper line/radius/adjacent tile.
- [x] Có displacement helper cho Smasher.
- [x] Có teleport helper/API cho Assassin.
- [x] Có tile hazard manager cho Magician.

### Skill

- [x] `MagicianFireSealSkill`
- [x] `SmasherEarthquakeSkill`
- [x] `HalberdierCrescentSlashSkill`
- [x] `AssassinShadowDualStrikeSkill`

### Asset

- [x] `Magician Fire Seal.asset`
- [x] `Smasher Earthquake.asset`
- [x] `Halberdier Crescent Slash.asset`
- [x] `Assassin Shadow Dual Strike.asset`
- [x] Gán asset vào prefab/unit tương ứng.
- [x] Kiểm tra `effectApplyTiming` để không timeout animation flow.

### Verification

- [x] `dotnet build turnbased_2025.sln --no-restore` không lỗi.
- [ ] Test từng skill trong scene với ít nhất 2 unit đối địch.
- [ ] Test cooldown và button UI cập nhật đúng.
- [ ] Test status icon hoặc fallback khi icon chưa có.
- [ ] Test unit chết giữa chuỗi effect không gây null/missing reference.

---

## Rủi Ro Cần Lưu Ý

- `OnAnimationImpact` hiện còn gap trong docs skill system; nếu asset dùng timing này mà runner chưa gọi `ApplyEffect()`, turn có thể timeout.
- `ShadowStep` cần phân biệt single-target và AOE. Không nên chặn damage AOE ở `TakeDamage()`.
- `BurningGround` không nên là status trên unit vì nó thuộc tile và cần tick/expire riêng.
- Displacement cần cẩn thận với `MapManager.RegisterUnit/UnregisterUnit` để không làm sai occupancy.
- Nếu tạo enum mới, cần kiểm tra các asset/UI icon data có bị thiếu icon nhưng không crash.
