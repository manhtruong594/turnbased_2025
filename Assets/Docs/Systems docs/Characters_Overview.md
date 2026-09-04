# Characters Overview

## 1. Phạm vi

Roster hiện có bảy hướng unit. Tài liệu mô tả vai trò, skill implementation và mục tiêu cân bằng; số
liệu cụ thể phải lấy từ `UnitData`/skill asset tại thời điểm build.

## 2. Cấu trúc chung

Mỗi unit prefab kết hợp:

- `UnitController`: state runtime, movement, damage/heal và turn action.
- `UnitData`: HP, base damage, spawn cost, move range/speed và icon.
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

## 4. Quan hệ chiến thuật mục tiêu

| Tình huống | Unit có lợi thế dự kiến | Đối sách dự kiến |
|---|---|---|
| Giữ choke/capture | Knight, Halberdier | Magician/Smasher phá vị trí |
| Đội hình đứng gần | Magician, Smasher, Halberdier | Tách đội hình, Assassin áp sát |
| Mục tiêu ít HP | Assassin, Archer | Shield, guard, deny target |
| Giao tranh kéo dài | Berserker | Burst, control hoặc disengage |
| Tuyến sau không được bảo vệ | Assassin | Root/stun, body block, focus fire |

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
| P1 | Chuẩn hóa tooltip, icon, cooldown và status feedback cho 7 unit |
| P1 | Kiểm tra prefab/animator/VFX reference của roster dùng trong build |
| P1 | Thu thập playtest data và điều chỉnh `UnitData`/skill asset |
| P2 | Chốt tên hiển thị theo bối cảnh Việt huyền dị |

## 8. Không thuộc tài liệu này

Shop price, unlock progression, rarity và monetization chưa được chốt. Khi triển khai, các hệ đó phải
tham chiếu unit bằng content ID ổn định, không dùng tên hiển thị làm khóa lưu dữ liệu.
