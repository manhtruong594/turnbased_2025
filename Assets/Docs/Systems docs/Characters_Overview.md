# Characters Overview

## 1. Phạm vi

Roster chính thức gồm mười class. Bảy class đã có `UnitData`/prefab; Dân Binh, Dược Sư và Phán Quan
đã có implementation cùng skill asset nhưng chưa hoàn tất tích hợp unit. Tài liệu mô tả vai trò,
cấu hình skill và mục tiêu cân bằng; số liệu runtime phải lấy từ asset tại thời điểm build.

## 2. Cấu trúc chung

Mỗi unit prefab kết hợp:

- `UnitController`: state runtime, movement, damage/heal và turn action.
- `UnitData`: HP, base damage, spawn cost, move range và icon.
- `UnitAttack`: danh sách skill runtime, attack validation và cooldown tick.
- `UnitAnimator`: animation state và skill cue.
- `SkillEffectRunner`: VFX/projectile theo cue.
- `BuffDebuffHandler`: buff/debuff và modifier.
- Collider/model/animator/UI reference cần thiết trong prefab.

## 3. Roster

### Berserker

**Vai trò:** bruiser, áp lực cận chiến, chấp nhận rủi ro để tăng damage.

Implementation liên quan:

- `BerserkerBloodHammerSkill`.
- Status `BloodRage` trong buff/debuff system.

Mục tiêu gameplay:

- Mạnh khi giao tranh kéo dài hoặc ở ngưỡng HP phù hợp.
- Có cửa sổ phản công rõ; không vừa bền nhất vừa gây damage cao nhất.

### Knight

**Vai trò:** tank/guard, giữ choke point và bảo vệ đội hình.

Implementation liên quan:

- `KnightHolySwordStanceSkill`.
- Status `StanceGuard`, có khả năng miễn displacement theo handler.

Mục tiêu gameplay:

- Giá trị đến từ vị trí và bảo vệ, không chỉ tổng HP.
- Stance phải có duration, feedback và cách khắc chế rõ.

### Magician

**Vai trò:** ranged control và damage vùng.

Implementation liên quan:

- `FireballSkill`.
- `FlameThrowerSkill`.
- `MagicianFireSealSkill`.
- `TileHazardManager` cho hazard theo tile.

Mục tiêu gameplay:

- Tầm/area mạnh nhưng cần positioning và có giới hạn cooldown.
- VFX không che target tile; hazard owner/duration phải đọc được.

### Smasher

**Vai trò:** area disruption, phá đội hình bằng damage và displacement.

Implementation liên quan:

- `SmasherEarthquakeSkill`.
- `DisplacementUtility`.

Mục tiêu gameplay:

- Tạo thay đổi vị trí có giá trị quanh capture point.
- Push phải tôn trọng tile blocked, map boundary và displacement immunity.

### Halberdier

**Vai trò:** melee reach, kiểm soát line/arc và trừng phạt đội hình dày.

Implementation liên quan:

- `HalberdierCrescentSlashSkill`.
- `SkillAreaUtility` cho line/radius query.

Mục tiêu gameplay:

- Tầm hiệu dụng cao hơn melee thường nhưng không thay thế ranged unit.
- Hướng đánh và vùng ảnh hưởng phải preview chính xác.

### Assassin

**Vai trò:** mobile finisher, đánh mục tiêu yếu và gây bleed.

Implementation liên quan:

- `AssassinBleedSkill`.
- `AssassinShadowDualStrikeSkill`.
- Status `ShadowStep`, `Bleed`, `HealBan` nếu được cấu hình.
- `TeleportUtility` cho vị trí quanh target.

Mục tiêu gameplay:

- Có khả năng tiếp cận/kết liễu nhưng dễ bị phạt nếu chọn sai thời điểm.
- Dual strike, teleport và target death giữa hai hit phải xử lý an toàn.

### Archer

**Vai trò:** ranged damage và kiểm soát từ xa.

Implementation liên quan:

- `NormalAttackSkill`/asset Normal arrow.
- `MultiArrowSkill`/asset Multi strike arrow.

Mục tiêu gameplay:

- Mạnh nhờ khoảng cách và chọn mục tiêu, yếu khi bị áp sát.
- Multi-hit phải chốt cách xử lý target chết giữa chuỗi và projectile cleanup.

### Dân Binh — Militia

**Vai trò:** bộ binh giá thấp, triển khai sớm để lấp tuyến và tranh capture point phụ.

**Chỉ số chính thức:** `Health = 70`, `BaseDamage = 8`, `spawnCost = 2`, `moveRange = 5`.

| Thứ tự | Skill asset | Type | MP / CD / Range | Target | Hiệu ứng |
|---:|---|---|---|---|---|
| 1 | `Militia Quick Slash` | `Normal` | 0 / 0 / 1 | Enemy | Chém Nhanh gây 100% damage. |
| 2 | `Militia Power Strike` | `Active` | 1 / 2 / 1 | Enemy | Dồn Sức gây 175% damage; target có `Shield` thì damage nhân thêm 1,3 trước khi khiên giảm damage. |
| 3 | `Militia Bandage` | `BuffAndDebuff` | 1 / 3 / 0 | Self | Băng Bó hồi 150% damage hiện tại. |

Implementation liên quan:

- `NormalAttackSkill`, `HealSkill`.
- `MilitiaPowerStrikeSkill` đọc `Shield` ngay trước hit.

Mục tiêu gameplay:

- Tạo lợi thế số lượng và vị trí với chi phí thấp, nhưng không có AoE hoặc khống chế.
- HP thấp và tầm ngắn khiến việc đứng cụm hoặc tách khỏi hỗ trợ dễ bị trừng phạt.

### Dược Sư — Herbalist

**Vai trò:** hỗ trợ giá thấp, hồi phục và giải debuff cho một mục tiêu quan trọng.

**Chỉ số chính thức:** `Health = 55`, `BaseDamage = 8`, `spawnCost = 2`, `moveRange = 5`.

| Thứ tự | Skill asset | Type | MP / CD / Range | Target | Hiệu ứng |
|---:|---|---|---|---|---|
| 1 | `Herbalist Staff Tap` | `Normal` | 0 / 0 / 1 | Enemy | Gõ Gậy gây 80% damage. |
| 2 | `Herbalist Field Remedy` | `BuffAndDebuff` | 1 / 2 / 2 | Ally + Self | Đắp Thuốc hồi 200% damage hiện tại. |
| 3 | `Herbalist Purification` | `BuffAndDebuff` | 1 / 3 / 2 | Ally + Self | Giải Uế xóa toàn bộ debuff; nếu xóa thành công, tạo `Shield = 30% CurrentDamage` trong 2 lượt. |

Implementation liên quan:

- `NormalAttackSkill`, `HealSkill`.
- `HerbalistCleanseSkill`, `BuffDebuffHandler.RemoveDebuffs()` và status `Shield`.

Mục tiêu gameplay:

- Buộc người chơi chọn giữa hồi HP và thanh tẩy; mỗi action chỉ hỗ trợ một target.
- 55 HP và damage thấp khiến Dược Sư cần tuyến trước bảo vệ.

### Phán Quan — Adjudicator

**Vai trò:** unit cao cấp phá phòng thủ, giảm damage địch và gây áp lực lên đội hình hồi phục.

**Chỉ số chính thức:** `Health = 140`, `BaseDamage = 14`, `spawnCost = 6`, `moveRange = 4`.

| Thứ tự | Skill asset | Type | MP / CD / Range | Target | Hiệu ứng |
|---:|---|---|---|---|---|
| 1 | `Adjudicator Verdict Stroke` | `Normal` | 0 / 0 / 2 | Enemy | Bút Phán gây 100% damage. |
| 2 | `Adjudicator Accusation` | `Active` | 1 / 3 / 3 | Enemy | Cáo Trạng gây 100% damage; target còn sống nhận `GuardBreak(20)` trong 2 lượt. |
| 3 | `Adjudicator Forbidden Seal` | `Ultimate` | 3 / 5 / 3 | Enemy + EmptyTile | Đại Ấn gây 160% damage trong radius 1; enemy còn sống nhận `Weaken(25)` và có 50% nhận `HealBan`, đều trong 2 lượt. |

Implementation liên quan:

- `NormalAttackSkill`, `AdjudicatorAccusationSkill`, `AdjudicatorForbiddenSealSkill`.
- `SkillAreaUtility`, `ActiveStatusEffect` và RNG của `LocalMatchAuthority`.

Mục tiêu gameplay:

- Giá trị cao khi đối phương tụ cụm hoặc dựa vào hồi phục, nhưng chỉ có một action và move range 4.
- Chi phí triệu hồi 6 và ultimate tốn thêm 3 MP tạo cửa sổ để đối thủ gây áp lực ở nhiều điểm.

## 4. Quan hệ chiến thuật mục tiêu

| Tình huống | Unit có lợi thế dự kiến | Đối sách dự kiến |
|---|---|---|
| Triển khai sớm, tranh nhiều điểm | Dân Binh | AoE, displacement, buộc giao tranh tập trung |
| Giữ choke/capture | Knight, Halberdier, Dân Binh | Magician/Smasher phá vị trí; Phán Quan mở burst |
| Đội hình đứng gần | Magician, Smasher, Halberdier, Phán Quan | Tách đội hình, đánh ở nhiều capture point |
| Mục tiêu có `Shield` hoặc guard | Dân Binh, Phán Quan | Giữ khoảng cách, bỏ khiên trước Dồn Sức, cleanse `GuardBreak` |
| Mục tiêu ít HP | Assassin, Archer | Shield, guard, Dược Sư hồi hoặc giải debuff |
| Giao tranh kéo dài | Berserker, Dược Sư | Burst tuyến sau, `HealBan`, control hoặc disengage |
| Đội hình phụ thuộc hồi phục | Phán Quan | Dược Sư giải `HealBan`, tản đội hình, ép Phán Quan dùng MP sớm |
| Tuyến sau không được bảo vệ | Assassin | Root/stun, body block, Dân Binh che vị trí |
| Đầu tư 6 MP vào một unit | Phán Quan | Dàn áp lực ở nhiều điểm bằng unit giá thấp |

Đây là mục tiêu cân bằng, không phải kết quả đã chứng minh. Cần playtest matrix để xác nhận.

## 5. Yêu cầu content cho mỗi unit

Một unit chỉ sẵn sàng phát hành khi có:

1. `UnitData` với tên, mô tả, icon và chỉ số đã review.
2. Prefab không missing component/reference.
3. Normal attack và skill phù hợp vai trò.
4. Idle, move, attack/cast, hit và death animation hoặc fallback được duyệt.
5. Skill cue đúng timing, VFX/projectile trả pool đúng.
6. Status icon/feedback liên quan.
7. Test valid/invalid target, cooldown, death và turn reset.

## 6. Nguyên tắc cân bằng

- So sánh tổng giá trị với `spawnCost`, không cân bằng từng chỉ số độc lập.
- Range và mobility có giá trị tương đương damage/HP.
- Area damage phải tính theo số target trung bình thực tế.
- Control mạnh cần duration, cooldown hoặc điều kiện sử dụng tương xứng.
- Không dùng AI hiện tại làm nguồn duy nhất để kết luận balance.

Chỉ số cần ghi khi playtest: pick rate, win rate, damage/heal, damage nhận, survival turn, capture
contribution, MP efficiency và tỷ lệ skill thành công.

## 7. Việc còn lại

| Mức | Hạng mục |
|---|---|
| P0 | Test từng skill trong scene với hai phe |
| P0 | Test target chết giữa multi-hit/projectile/composite sequence |
| P0 | Tạo `UnitData`/prefab và đăng ký roster/catalog cho Dân Binh, Dược Sư, Phán Quan |
| P1 | Chuẩn hóa tooltip, icon, cooldown và status feedback cho 10 class |
| P1 | Kiểm tra prefab/animator/VFX reference của roster dùng trong build |
| P1 | Thu thập playtest data và điều chỉnh `UnitData`/skill asset |
| P2 | Chốt tên hiển thị theo bối cảnh Việt huyền dị |

## 8. Không thuộc tài liệu này

Shop price, unlock progression, rarity và monetization chưa được chốt. Khi triển khai, các hệ đó phải
tham chiếu unit bằng content ID ổn định, không dùng tên hiển thị làm khóa lưu dữ liệu.
