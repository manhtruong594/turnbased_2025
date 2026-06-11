# Characters Overview

Tài liệu này mô tả vai trò chiến thuật, gameplay chính và đề xuất ultimate cho các class unit. Các thông số damage/range/cooldown là mốc thiết kế ban đầu để phù hợp với grid turn-based hiện tại; khi implement cần cân lại theo HP, damage, map và thời lượng turn thực tế.

Nguồn tham chiếu hiện tại:

- Core game: PvP turn-based, chiếm cứ điểm là điều kiện thắng chính.
- Skill system: dùng `SkillBase` theo hướng Strategy/Data-Driven bằng `ScriptableObject`.
- VFX/art direction: giấy dó, mực tàu, sơn son, vàng kim, hoa văn trống đồng, bùa chú và hiệu ứng nét mực.

---

## Tổng Quan Class

| Class | Vũ khí/chủ đề | Skill tiêu biểu | Vai trò chiến thuật |
|-------|---------------|-----------------|---------------------|
| Berserker | Chùy gai lớn, cuồng nộ, đổi máu lấy sát thương | Huyết Chùy Phá Trận | Phá đội hình, kết liễu nhóm địch gần nhau, chấp nhận rủi ro tự mất máu. |
| Knight | Kiếm dài, kỷ luật, phòng tuyến | Thánh Kiếm Lập Thế | Giữ vị trí, bảo vệ cứ điểm, tạo vùng khống chế hướng trước mặt. |
| Magician | Phù thủy lửa, bùa chú, hỏa pháp | Hỏa Ấn Thiên Trụ | AOE tầm xa, tạo hazard, ép địch rời khỏi khu vực quan trọng. |
| Smasher | Búa nặng, địa chấn, phá giáp | Địa Chấn Toái Sơn | Khống chế diện rộng, đẩy lệch vị trí, phá đội hình quanh cứ điểm. |
| Halberdier | Rìu chiến dài, tầm với, chống áp sát | Nguyệt Nha Trảm Trận | Zone tuyến giữa, chặn đường lao vào, kéo/đẩy địch khỏi cứ điểm. |
| Assassin | Hai kiếm ngắn, cơ động, ám sát | Ảnh Bộ Song Sát | Đột kích mục tiêu yếu máu, luồn ra sau tuyến, thưởng lớn khi kết liễu. |
| Archer | Cung, bắn xa, dồn tên liên tiếp | Normal Arrow, Multi Strike Arrow | Gây áp lực từ xa, ép đối thủ né vị trí mở, kết liễu mục tiêu đã bị khống chế. |

---

## Berserker - Huyết Chùy Phá Trận

| Thuộc tính | Đề xuất |
|------------|---------|
| SkillType | `Ultimate` |
| Range | 1 |
| Cooldown | 4 turn |
| TargetTypes | Enemy |
| Effect timing | `OnRelease` hoặc `OnAnimationImpact` sau khi runner hỗ trợ melee impact |
| Vùng ảnh hưởng | 1 target chính + các ô kề cận quanh target |

Mô tả gameplay:

- Berserker nhảy/lao tới mục tiêu gần kề, đập cây chùy gai xuống đất.
- Target chính nhận sát thương lớn: `caster damage * 2.2`.
- Enemy ở các ô kề cận quanh target nhận sát thương phụ: `caster damage * 1.1`.
- Berserker tự mất 10% HP hiện tại sau khi dùng skill. Nếu HP quá thấp, vẫn cho dùng nhưng không thể tự giết mình; để lại tối thiểu 1 HP.
- Nếu target chính bị hạ gục, Berserker được buff `Blood Rage` trong 1 turn: +1 move và +20% damage cho turn tiếp theo.

Hiệu ứng phụ cần code sau:

- `Blood Rage`: buff tạm thời cho caster.
- Tự sát thương không làm caster chết.

VFX/SFX:

- Cast: vòng bùa đỏ son quét quanh chân, hơi thở màu đỏ đen.
- Release: vệt chùy gai kéo thành một đường mực đỏ trên không trung.
- Impact: cột bụi đất, mảnh gỗ/đá văng ra, nhiều vết gai đỏ như dấu ấn trên mặt đất.
- SFX: tiếng gỗ/kim loại trầm, nhịp trống lớn, âm thanh máu sôi nhẹ sau impact.

Ghi chú balance:

- Mạnh khi địch đứng cụm quanh cứ điểm.
- Có rủi ro vì tự mất máu, nên phù hợp class sát thương cao nhưng thiếu phòng thủ.

---

## Knight - Thánh Kiếm Lập Thế

| Thuộc tính | Đề xuất |
|------------|---------|
| SkillType | `Ultimate` |
| Range | 1 |
| Cooldown | 4 turn |
| TargetTypes | Enemy |
| Effect timing | `OnRelease` hoặc `OnAnimationImpact` sau khi runner hỗ trợ melee impact |
| Vùng ảnh hưởng | Đường thẳng 3 ô phía trước Knight |

Mô tả gameplay:

- Knight cầm kiếm dài chém một đường dọc tạo thành vệt kiếm sáng.
- Tất cả enemy trong đường thẳng 3 ô phía trước nhận damage: `caster damage * 1.6`.
- Target đầu tiên trên đường kiếm nhận thêm `Guard Break`: giảm phòng thủ/giáp trong 1 turn nếu game có chỉ số giáp.
- Sau khi chém, Knight nhận `Stance Guard` trong 1 turn: giảm 20% damage nhận vào và miễn nhiễm bị đẩy lệch vị trí.
- Nếu đang đứng trên cứ điểm, `Stance Guard` kéo dài thêm 1 turn để khuyến khích gameplay giữ vị trí.

Hiệu ứng phụ cần code sau:

- `Guard Break`: debuff phòng thủ.
- `Stance Guard`: buff phòng thủ/anti displacement.
- Directional line targeting: tính các tile theo hướng Knight nhìn.

VFX/SFX:

- Cast: ánh sáng vàng kim chạy dọc lưỡi kiếm, một vòng trận pháp nhỏ dưới chân.
- Release: vệt chém rộng như nét mực vàng cắt qua không khí.
- Impact: các mảnh ký tự Nôm/Hán vàng vỡ ra trên enemy bị chém.
- Buff guard: khiên ánh sáng mờ như hoa văn trống đồng xuất hiện trước ngực.

Ghi chú balance:

- Damage không cao bằng Berserker nhưng có giá trị phòng thủ và giữ cứ điểm.
- Cần map có chokepoint để skill tỏa sáng.

---

## Magician - Hỏa Ấn Thiên Trụ

| Thuộc tính | Đề xuất |
|------------|---------|
| SkillType | `Ultimate` |
| Range | 3 |
| Cooldown | 5 turn |
| TargetTypes | Enemy + EmptyTile |
| Effect timing | `OnProjectileImpact` nếu có projectile, hoặc `OnRelease` nếu cast trực tiếp |
| Vùng ảnh hưởng | Hình tròn quanh target, radius 1-2 tùy balance |

Mô tả gameplay:

- Magician ném một lá bùa lửa lên trời, sau đó một cột lửa rơi xuống tile chỉ định.
- Tất cả enemy trong radius nhận damage ban đầu: `caster damage * 1.4`, kèm 25% tỉ lệ nhận hiệu ứng `Burn` trong 2 turn
- Các tile trong vùng ảnh hưởng bị đánh dấu `Burning Ground` trong 2 turn.
- Enemy đứng trên `Burning Ground` ở đầu turn hoặc khi bước vào cũng sẽ có 50% tỉ lệ nhận `Burn` trong 2 turn
- Nếu enemy đã bị `Burn`, damage ban đầu tăng thêm 25%

Hiệu ứng phụ cần code sau:

- `Burn`: damage-over-time trên unit.
- `Burning Ground`: tile hazard tồn tại nhiều turn.

VFX/SFX:

- Cast: lá bùa giấy đỏ bốc lửa, ký tự pháp thuật xoay quanh tay.
- Projectile: ấn lửa bay lên cao rồi rơi xuống như ấn triện đỏ rực.
- Impact: cột lửa dựng thẳng, vòng sóng nhiệt lan ra theo radius.
- Burning Ground: nền tile cháy âm ỉ với vệt mực đen và tàn tro bay.

Ghi chú balance:

- Phù hợp ép vị trí, chặn đường vào cứ điểm hoặc buộc địch rời khỏi chokepoint.
- Cooldown nên cao hơn các ultimate melee vì có range và tile hazard.

---

## Smasher - Địa Chấn Toái Sơn

| Thuộc tính | Đề xuất |
|------------|---------|
| SkillType | `Ultimate` |
| Range | 1 |
| Cooldown | 4 turn |
| TargetTypes | Enemy + EmptyTile |
| Effect timing | `OnAnimationImpact` sau khi runner hỗ trợ melee impact, tạm thời có thể set `OnRelease` |
| Vùng ảnh hưởng | 1 tile |

Mô tả gameplay:

- Smasher nâng búa qua đầu và đập xuống đất, tạo sóng chấn động trên 1 tile.
- Enemy trong tile nhận damage: `caster damage * 1.8`.
- Enemy bị đẩy lùi 1 tile theo hướng tấn công của caster nếu tile sau lưng trống. Nếu bị đẩy vào vật cản, nhận thêm damage và bị choáng 1 turn.

Hiệu ứng phụ cần code sau:

<!-- - `Stagger`: debuff giảm move -->
- Displacement/knockback theo hướng từ tâm impact ra ngoài.
- Collision damage khi bị đẩy vào obstacle/unit.

VFX/SFX:

- Cast: camera rung nhẹ, bụi đất bị hút về đầu búa.
- Release: đường cong của búa tạo vệt sáng màu vàng đất.
- Impact: mặt đất nứt theo hình nón quạt, vòng shockwave thấp lan trên tile.
- Knockback: vệt bụi kéo sau chân enemy bị đẩy.

Ghi chú balance:

- Thấp hơn Magician về range nhưng mạnh về khống chế và phá đội hình.
- Rất có giá trị khi địch đang giữ cứ điểm hoặc đứng cạnh vực/vật cản.

---

## Halberdier - Nguyệt Nha Trảm Trận

Halberdier là unit tuyến giữa dùng rìu chiến dài để kiểm soát khoảng cách. Class này không cần sát thương bùng nổ như Berserker; sức mạnh chính nằm ở việc chiếm ô thuận lợi, khóa lối đi hẹp và phạt các unit cố áp sát cứ điểm.

| Thuộc tính | Đề xuất |
|------------|---------|
| SkillType | `Ultimate` |
| Range | 2 |
| Cooldown | 4 turn |
| TargetTypes | Enemy |
| Effect timing | `OnRelease` hoặc `OnAnimationImpact` sau khi runner hỗ trợ melee impact |
| Vùng ảnh hưởng | Một đường thẳng 2 ô phía trước|

Mô tả gameplay:

- Halberdier xoay rìu nhiều vòng và lướt tới ô chỉ định
- Enemy ở ô đầu tiên trong đường thẳng nhận damage chính: `caster damage * 1.7`.
- Enemy ở các ô phụ nhận damage nhẹ hơn: `caster damage * 1.0`.
- Nếu chỉ có 1 target hoặc target đang đứng trên cứ điểm, 100% apply `HealBan` cho targt: cấm hồi máu trong 2 turn
- Nhận thêm `Shield` Buff chặn sát thương cho Halberdier trong 2 turn

Biến thể active phụ có thể dùng trước ultimate:

<!-- - `Long Reach`: đòn đánh thường của Halberdier có thể đánh range 2 theo đường thẳng nhưng damage thấp hơn Archer.
- `Brace`: kết thúc lượt trong tư thế thủ; enemy bước vào ô kề trước mặt sẽ bị phản kích nhẹ hoặc bị dừng di chuyển. -->

Hiệu ứng phụ cần code sau:

VFX/SFX:

- Cast: cán rìu gõ xuống đất, vòng mực đen lan thành hình bán nguyệt.
- Release: vệt chém dài màu vàng son, đầu rìu để lại nét mực cong như trăng khuyết.
- Impact: target bị trúng có dây bùa/mực quấn quanh thân trước khi bị kéo.
- SFX: tiếng cán gỗ xoay nhanh, tiếng kim loại nặng rít qua không khí, tiếng dây kéo căng khi `Hooked`.

Animation cue gợi ý:

- `Cast`: hạ trọng tâm, đặt rìu ngang người.
- `Release`: xoay nửa vòng rồi chém kéo từ ngoài vào trong.
- `Impact`: lưỡi rìu móc trúng target, apply damage và kéo/pin.
- `Complete`: chống cán rìu xuống đất, giữ stance chặn đường.

Ghi chú balance:

- Mạnh nhất khi đứng sau blocker hoặc cạnh cứ điểm, vì range 2 giúp gây áp lực mà không phải bước vào ô nguy hiểm.
- Không nên cho damage quá cao; nếu vừa kéo vừa burst mạnh sẽ lấn vai trò Assassin và Berserker.
- Counter tự nhiên là Archer/Magician vì Halberdier cần hướng đứng đẹp và dễ bị poke từ xa.

---

## Assassin - Ảnh Bộ Song Sát

Assassin là unit cơ động cầm hai kiếm ngắn, chuyên săn mục tiêu đã yếu máu hoặc đang đứng lệch khỏi đội hình. Class này nên tạo cảm giác "vào nhanh, ra nhanh", nhưng phải có rủi ro nếu lao vào sai thời điểm.

| Thuộc tính | Đề xuất |
|------------|---------|
| SkillType | `Ultimate` |
| Range | 3 |
| Cooldown | 4 turn |
| TargetTypes | Enemy |
| Effect timing | `OnRelease` hoặc `OnAnimationImpact` |
| Vùng ảnh hưởng | 1 target chính; có thể thêm 1 enemy kề target nếu cần splash nhẹ |

Mô tả gameplay:

- Assassin lướt tới xuất hiện ở ô trống phía sau hoặc bên cạnh target.
- Target chính nhận 2 hit liên tiếp: mỗi hit `caster damage * 0.9`.
- Nếu target dưới 40% HP trước khi nhận hit thứ hai, hit thứ hai tăng lên `caster damage * 1.4`.
- Nếu skill hạ gục target, Assassin được `Shadow Step`: tàng hình trong 1 turn, không thể bị chọn làm mục tiêu nhưng vẫn dính các hiệu ứng AOE

Biến thể active/passive phụ:

<!-- - `Marked Prey`: mỗi lần Assassin đánh cùng một target trong 2 turn, cộng dồn một dấu ấn; đủ 2 dấu thì đòn tiếp theo gây thêm damage. -->

Hiệu ứng phụ cần code sau:

- `Shadow Step`: teleport ngắn tới ô hợp lệ quanh target hoặc trở về vị trí trước khi cast.
- `ExecuteThreshold`: tăng damage theo phần trăm HP của target.

VFX/SFX:

- Cast: bóng mực đen tụ dưới chân, hai lưỡi kiếm lóe ánh xanh chàm.
- Release: Assassin biến thành vệt mực xé ngang grid, để lại vài lá bùa nhỏ cháy tàn.
- Impact: hai vết chém chéo hiện lên trên target, vết thứ hai sáng mạnh nếu kích hoạt execute.
- Shadow Step: khói mực nổ nhẹ ở vị trí cũ và vị trí mới.
- SFX: tiếng kiếm ngắn cắt gió sắc, nhịp trống nhỏ gấp, tiếng giấy bị rạch.

Animation cue gợi ý:

- `Cast`: cúi người, đưa hai kiếm ra sau.
- `Release`: dash/teleport tới target.
- `Impact`: hai hit rõ nhịp để VFX và damage có thể tách timing.
- `Complete`: chọn ô rút lui nếu có `Shadow Step`, nếu không thì đứng lại ở cạnh target.

Ghi chú balance:

- Nên có damage cao khi kết liễu nhưng không quá ổn định khi mở combat.
- Cần yêu cầu ô trống quanh target để teleport, tránh dùng ultimate xuyên mọi phòng tuyến.

---

## Archer - Xạ Thủ Kiểm Soát Tầm Xa

Archer đã có data và skill asset trong project. Vai trò nên giữ là unit tầm xa cơ động, gây áp lực lên tile mở và kết liễu mục tiêu bị teammate khóa chân. Vì gameplay thắng bằng chiếm cứ điểm, Archer không nên chỉ là damage dealer đứng yên; class này cần buộc đối thủ cân nhắc đường đi và vị trí nấp.

### Data hiện có

| Asset | Giá trị hiện tại |
|-------|------------------|
| Unit data | `Assets/Scripts/Data/Archer Data.asset` |
| HP | 100 |
| BaseDamage | 10 |
| SpawnCost | 3 |
| MoveRange | 6 |
| MoveSpeed | 5 |

### Skill hiện có

| Asset | Class/logic | SkillType | Range | Cooldown | TargetTypes | Ghi chú |
|-------|-------------|-----------|-------|----------|-------------|---------|
| `Normal arrow.asset` | `NormalAttackSkill` | `Normal` | 1 | 0 | Enemy + EmptyTile | Damage `caster damage * 1.25`, có projectile VFX. |
| `Multi strike arrow.asset` | `MultiArrowSkill` | `Ultimate` | 5 | 0 hiện tại | Enemy + EmptyTile | Bắn tối đa 4 mũi tên, mũi đầu `1.2x`, mũi sau random `0.4x-0.7x`, có 25% áp hiệu ứng phụ `FreezeEffect(duration: 2)` trên mỗi mũi phụ. |

Ghi chú kỹ thuật:

- `MultiArrowSkill.cs` đã tồn tại và có logic multi-hit.
- Asset `Multi strike arrow.asset` đang serialize field của `MultiArrowSkill`; nếu Unity Inspector hiển thị class cũ do cache/metadata, nên mở lại asset trong Unity để xác nhận script binding.
- `Normal arrow.asset` đang để range 1, chưa phản ánh fantasy Archer tầm xa. Nếu muốn Archer bắn thường đúng vai trò, nên cân nhắc tăng range normal lên 3-4 hoặc tạo skill normal riêng cho bắn xa.
- `Multi strike arrow.asset` đang cooldown 0 dù là `Ultimate`; nên cân nhắc cooldown 3-4 turn để tránh spam.

### Đề xuất gameplay hoàn chỉnh

#### Normal Arrow - Tiễn Thường

| Thuộc tính | Đề xuất |
|------------|---------|
| SkillType | `Normal` |
| Range | 3 hoặc 4 |
| Cooldown | 0 |
| TargetTypes | Enemy |
| Effect timing | `OnProjectileImpact` |
| Vai trò | Poke tầm xa, ép địch tránh đường bắn |

Mô tả gameplay:

- Archer bắn 1 mũi tên vào target trong tầm nhìn.
- Damage giữ gần hiện tại: `caster damage * 1.0` đến `1.25`.
- Nếu target đang bị `Pinned`, `Stagger`, `Freeze` hoặc đứng trên cứ điểm, đòn bắn có thể nhận thêm +10-15% damage để khuyến khích combo với Halberdier/Smasher.

#### Multi Strike Arrow - Liên Châu Tỏa Tiễn

| Thuộc tính | Đề xuất |
|------------|---------|
| SkillType | `Ultimate` |
| Range | 5 |
| Cooldown | 3 hoặc 4 turn |
| TargetTypes | Enemy |
| Effect timing | `OnProjectileImpact` hoặc nhiều `ApplyEffect()` theo animation/projectile |
| Vùng ảnh hưởng | 1 target chính; các mũi phụ có thể tiếp tục vào target hoặc chia sang enemy kề target |

Mô tả gameplay:

- Archer giương cung, bắn liên tiếp 4 mũi tên vào cùng target.
- Mũi đầu gây damage ổn định: `caster damage * 1.2`.
- Các mũi sau gây damage thấp hơn: `caster damage * random(0.4, 0.7)`.
- Mỗi mũi sau có 25% áp hiệu ứng phụ hiện tại là `FreezeEffect(duration: 2)`.
- Nếu target chết trước khi hết số mũi tên, các mũi còn lại có thể:
  - biến mất để giữ implementation đơn giản;
  - hoặc chuyển sang enemy gần target nhất trong radius 1 nếu muốn ultimate có cảm giác mạnh hơn.

VFX/SFX:

- Cast: dây cung sáng như chỉ vàng, bùa giấy nhỏ xoay quanh cổ tay.
- Release: từng mũi tên để lại vệt mực mảnh, mũi cuối có vệt sáng đậm hơn.
- Impact: ký tự băng/mực xanh chàm đóng trên target nếu `FreezeEffect` kích hoạt.
- SFX: tiếng dây cung bật nhanh nhiều nhịp, tiếng tên cắm vào gỗ/gốm, tiếng băng nứt nhẹ khi freeze.

Ghi chú balance:

- Archer nên mạnh khi được bảo vệ bởi Knight/Halberdier và yếu khi bị Assassin áp sát.
- Range cao phải đi kèm yêu cầu line-of-sight hoặc điểm mù từ obstacle nếu map hỗ trợ.
- Ultimate multi-hit rất hợp để phá unit thấp HP, nhưng cần cooldown để không xóa nhịp phản công của đối thủ.

---

## Synergy Và Counter

| Combo | Ý tưởng |
<!-- |-------|---------|
| Halberdier + Archer | Halberdier kéo/pin enemy khỏi cứ điểm, Archer dùng `Multi Strike Arrow` để kết liễu mục tiêu không thể rời vị trí. |
| Smasher + Magician | Smasher đẩy/stagger nhóm địch vào vùng xấu, Magician đặt `Burning Ground` khóa lối thoát. |
| Knight + Archer | Knight giữ chokepoint, Archer đứng sau tạo damage ổn định từ xa. |
| Assassin + Berserker | Berserker làm mềm đội hình, Assassin dọn mục tiêu thấp máu và rút ra bằng `Shadow Step`. |
| Halberdier counter Assassin | `Brace` hoặc `Pinned` làm Assassin khó vào/ra tự do. |
| Archer counter Halberdier | Archer bắn từ ngoài vùng rìu chiến, buộc Halberdier phải đổi vị trí. | -->

---

## Hướng Implement Sau Này

Nên tách effect thành các strategy/component nhỏ thay vì viết logic quá lớn trong từng skill:

- `DamageAreaEffect`: xử lý damage theo radius/line/cone/cleave.
- `StatusEffect`: base cho `Bleed`, `Burn`, `GuardBreak`, `Stagger`, `StanceGuard`, `Pinned`, `Exposed`, `Freeze`.
- `TileHazardEffect`: quản lý vùng tile tồn tại nhiều turn như `BurningGround` hoặc smoke tile.
- `DisplacementEffect`: đẩy lùi/kéo unit và xử lý va chạm.
- `TeleportEffect`: xử lý dash/teleport ngắn của Assassin.
- `ConditionalDamageEffect`: tăng damage theo HP threshold, status hiện có hoặc target đang đứng trên cứ điểm.

Cách này giữ concrete skill ngắn gọn, dùng lại logic giữa các class và hợp với hướng Strategy/Data-Driven của skill system hiện tại.
