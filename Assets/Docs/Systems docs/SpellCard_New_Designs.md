# Đề xuất 6 spell card mới

Ngày đối chiếu: 2026-09-19. Đây là thiết kế để thử nghiệm cân bằng; chưa tạo `SpellCardData` asset hoặc kiểm chứng trong Play Mode.

## 1. Cơ sở thiết kế

Dựa trên [Spell Card System](SpellCard_System.md), [Core Gameloop System](Core_Gameloop_System.md), [Characters Overview](Characters_Overview.md) và [Gameplay commands](Multiplayer_Gameplay_Commands.md), đối chiếu với code và asset hiện tại:

- Bốn lá trong `Assets/Scripts/Data/SpellData`: Damage Spell (1 MP, 4 damage), Root Spell (2 MP, Root 2 lượt), Shield Spell (1 MP, Shield 20 trong 2 lượt), Stun Spell (2 MP, Stun 1 lượt).
- Damage Spell, Root Spell và Stun Spell đang dùng `AllEnemies`; Shield Spell dùng `SingleAlly`. Thông số này là mốc tham khảo, chưa phải bằng chứng cân bằng.
- Các `UnitData` hiện có đang dùng `Health = 100`, `BaseDamage = 10`, `spawnCost = 3`. Chi 3 MP cho spell vì vậy cạnh tranh trực tiếp với việc triệu hồi thêm một unit.
- Thắng bằng capture yêu cầu sở hữu tất cả cứ điểm. Spell mới hỗ trợ giữ unit, mở đường và trì hoãn đối phương; không trực tiếp đổi owner của cứ điểm.

Ba lá đầu ghép effect đã có bằng `CompositeEffect`, dùng một mục tiêu và `consumeOnUse = true`. Không cần thiết kế effect class mới. Ba lựa chọn bổ sung ở mục 5 tập trung vào đánh đổi, cách dùng hai mặt và ép đối phương chọn vị trí; mức hỗ trợ hiện tại được ghi riêng cho từng lá.

| Spell card | Vai trò | MP | Target | Effect theo thứ tự |
|---|---|---:|---|---|
| Ân Xá Bình Minh | Giải cứu, duy trì quân số | 2 | `SingleAlly` | `CleanseEffect` → `HealEffect` |
| Chiến Kỳ Phá Vây | Hỗ trợ unit đột phá | 2 | `SingleAlly` | `DamageBuffEffect` → `ShieldEffect` |
| Huyết Băng Phong Ấn | Khống chế, bào mòn | 3 | `SingleEnemy` | `FreezeEffect` → `BleedEffect` |

### Quy tắc dùng chung

- Đề xuất `range = 0`. Theo luồng authority hiện tại, spell không có unit caster làm gốc tính khoảng cách; `SingleAlly`/`SingleEnemy` chọn một unit trên bản đồ. Không hiểu `range = 0` là chỉ dùng lên bản thân.
- `duration` theo lượt của unit nhận status, không theo tổng số lần hai phe đổi lượt. Status có hiệu lực ngay khi áp dụng; tick ở đầu lượt của unit và xóa khi hết hạn theo vòng đời action hiện tại. Buff cast trong lượt mình có thể tác dụng ngay trước tick đầu tiên.
- Status cùng `StatusEffectType` bị thay thế cả value và duration, không cộng dồn, không tự giữ giá trị mạnh hơn.
- Spell không cấp thêm move/action, không reset cooldown. Thanh tẩy status cũng không hoàn lại action đã dùng.

## 2. Ân Xá Bình Minh — Dawn Absolution

**Ý tưởng:** Một ấn sáng phá xiềng xích rồi khép miệng vết thương. Lá cứu viện cho unit đang bị khống chế hoặc sắp mất vị trí quan trọng.

**Mô tả trên card:** “Xóa toàn bộ debuff của một đồng minh, sau đó hồi 20 HP. Giữ nguyên buff.”

| Trường | Giá trị đề xuất |
|---|---|
| `spellName` | `Ân Xá Bình Minh` |
| `mpCost` | `2` |
| `targetType` | `SingleAlly` |
| `range` | `0` |
| `consumeOnUse` | `true` |
| `spellEffect` | `CompositeEffect` |
| `effects[0]` | `CleanseEffect` |
| `effects[1]` | `HealEffect`: `healAmount = 20` |

**Ứng dụng gameplay:** Cứu Knight đang giữ lối vào cứ điểm; gỡ Root cho unit còn move để tiếp tục tranh điểm; gỡ Bleed và phục hồi cho unit bị Assassin truy kích. Gỡ Stun/Freeze chỉ cho phép hành động nếu các cờ action còn hợp lệ.

**Thứ tự bắt buộc:** Cleanse trước Heal để xóa cả `HealBan` trước khi hồi máu. Heal bị giới hạn bởi HP tối đa; card không hồi sinh unit đã chết và không tạo miễn nhiễm debuff cho các lượt sau.

**Đánh đổi và đối phó:** Không thêm sát thương hoặc shield. Đối phương có thể dồn damage kết liễu trước khi được cứu hoặc áp debuff trở lại. Dùng lên unit đầy HP và không có debuff không tạo lợi ích; không giả định hệ thống sẽ tự chặn lượt cast đó.

**Cân bằng thử nghiệm:** 20 HP tương đương 20% HP cơ bản hiện tại. Giá trị chính là thời điểm thanh tẩy; mức 2 MP buộc người chơi cân nhắc cứu quân hay giữ MP triệu hồi.

**Gợi ý hình ảnh:** Xiềng tím tan trong vòng sáng trắng vàng; nhịp hồi máu xuất hiện sau nhịp phá xiềng.

## 3. Chiến Kỳ Phá Vây — Breakthrough Standard

**Ý tưởng:** Đặt chiến kỳ lên một mũi tiến công, tăng lực đánh và giúp unit chịu được phản kích khi tranh chấp đường vào cứ điểm.

**Mô tả trên card:** “Một đồng minh nhận +5 damage và giảm 5 sát thương mỗi lần nhận damage, mỗi hiệu ứng có duration 1 lượt.”

| Trường | Giá trị đề xuất |
|---|---|
| `spellName` | `Chiến Kỳ Phá Vây` |
| `mpCost` | `2` |
| `targetType` | `SingleAlly` |
| `range` | `0` |
| `consumeOnUse` | `true` |
| `spellEffect` | `CompositeEffect` |
| `effects[0]` | `DamageBuffEffect`: `damageBonus = 5`, `duration = 1` |
| `effects[1]` | `ShieldEffect`: `shieldValue = 5`, `duration = 1` |

**Ứng dụng gameplay:** Cast trước khi Berserker hoặc Halberdier tấn công để tăng áp lực lên unit chặn đường; hỗ trợ một unit tiến lên giữ vị trí sau giao tranh. Card không tăng move range, không đẩy mục tiêu và không tự chiếm cứ điểm.

**Tương tác cần hiểu đúng:** `DamageBuffEffect` cộng số cố định vào damage của unit, không phải +5% và không tăng `DamageEffect` của lá spell khác. Skill đọc `GetCurrentDamage()` có thể hưởng buff theo công thức riêng của skill. Shield hiện tại giảm damage mỗi lần nhận, kể cả tick damage qua `TakeDamage`; không phải một túi 5 HP bị tiêu hao.

**Đánh đổi và đối phó:** Shield thấp hơn Shield Spell hiện có và duration ngắn hơn. Root/Stun/Freeze có thể ngăn unit tận dụng buff; Weaken giảm damage đầu ra. Đánh nhiều hit nhỏ có thể bị Shield giảm nhiều lần, nên cần kiểm tra combo thay vì chỉ so damage một hit.

**Lưu ý phối hợp:** Cast lên unit đang có Shield 20 sẽ thay Shield 20 bằng Shield 5; không dùng lá này như một lớp khiên cộng thêm. Nếu cast trong lượt mình trước khi unit đánh, buff có hiệu lực ngay và có thể còn tới lượt kế tiếp của unit trước khi bị xóa theo action.

**Cân bằng thử nghiệm:** Với `BaseDamage = 10`, bonus 5 đưa damage cơ sở lên 15 trước các modifier khác. Kiểm tra riêng skill nhiều hit hoặc nhiều mục tiêu vì tổng lợi ích có thể lớn hơn 5 damage.

**Gợi ý hình ảnh:** Chiến kỳ đỏ vàng phía trên unit, vệt sáng trên vũ khí và một vòng khiên mỏng quanh chân.

## 4. Huyết Băng Phong Ấn — Bloodfrost Seal

**Ý tưởng:** Giam đối thủ trong băng đỏ, để vết thương tiếp tục rỉ máu bên trong. Người chơi phải chọn giữ khống chế hay phá băng để dồn damage.

**Mô tả trên card:** “Đóng băng một kẻ địch 2 lượt và gây Bleed 6 damage ở đầu mỗi lượt của mục tiêu trong 2 lượt. Bleed không phá băng.”

| Trường | Giá trị đề xuất |
|---|---|
| `spellName` | `Huyết Băng Phong Ấn` |
| `mpCost` | `3` |
| `targetType` | `SingleEnemy` |
| `range` | `0` |
| `consumeOnUse` | `true` |
| `spellEffect` | `CompositeEffect` |
| `effects[0]` | `FreezeEffect`: `duration = 2` |
| `effects[1]` | `BleedEffect`: `bleedDamage = 6`, `duration = 2` |

**Ứng dụng gameplay:** Khóa unit đang tiến tới cứ điểm cuối, tập trung quân xử lý mục tiêu khác; hoặc chuẩn bị một đòn phá băng để kết liễu. Không tự giải phóng ô đang bị unit địch chiếm và không thay đổi quyền sở hữu cứ điểm khi cast.

**Hai cách khai thác:**

1. Giữ băng: tránh hit trực tiếp vào mục tiêu, tận dụng thời gian khống chế để di chuyển và tranh điểm. Bleed gây tối đa 12 damage danh nghĩa qua hai tick nếu tồn tại đủ lâu, trước các modifier nhận damage.
2. Phá băng: hit damage trực tiếp đầu tiên nhận hệ số ×1,2 từ Freeze, phá Freeze và chuyển số lượt còn lại thành `Slow(20)`. Bleed tiếp tục theo duration riêng. Dùng Damage Spell sau đó cũng có thể phá băng vì `DamageEffect` gọi damage trực tiếp.

**Đánh đổi và đối phó:** Giá 3 MP bằng spawn cost của một unit trong các asset hiện tại. Cleanse gỡ cả Freeze lẫn Bleed; Shield giảm từng tick Bleed, nên Shield 20 có thể chặn hoàn toàn tick 6 nếu không có modifier khác. Tấn công trực tiếp quá sớm tự đánh đổi thời gian khống chế lấy khả năng dồn damage.

**Lưu ý phối hợp:** Bleed hiện có trên mục tiêu bị thay bằng Bleed 6/2 lượt; không cộng dồn với Bleed của Assassin. Không ghi “12 damage chắc chắn” trên tooltip vì cleanse, shield, target death và thời điểm tick có thể thay đổi kết quả.

**Gợi ý hình ảnh:** Khối băng xanh sẫm với vết nứt đỏ; mỗi tick lóe đỏ bên trong, hit trực tiếp mới phát nhịp vỡ băng.

## 5. Ba lựa chọn bổ sung có chiều sâu chiến thuật

Thông số dưới đây là điểm khởi đầu cho playtest. Cả ba dùng một card mỗi lần cast, `consumeOnUse = true`, `range = 0`; không cấp lại action. Hai lá đầu tận dụng nguyên effect hiện có. Lá thứ ba cần logic theo dõi điều kiện nhưng tái sử dụng damage/status hiện tại.

| Lựa chọn | Quyết định cốt lõi | MP | Mức triển khai |
|---|---|---:|---|
| Khế Ước Huyết Chiến | Đổi sinh lực tương lai lấy thời điểm tấn công quyết định | 2 | Ghép effect hiện có |
| Nhà Tù Từ Bi | Cứu quân mình nhưng giữ chân, hoặc giữ chân địch nhưng cứu chúng | 2 | Ghép effect hiện có, `AnyUnit` |
| Tối Hậu Thư Tro Tàn | Ép địch rời vị trí hoặc chịu hình phạt sau một lượt phản ứng | 3 | Cần effect điều kiện và dấu ấn mới |

### 5.1. Khế Ước Huyết Chiến — Bloodbound Bargain

**Mô tả trên card:** “Một đồng minh nhận +8 damage trong 1 lượt và Bleed 10 damage mỗi lượt trong 2 lượt. Sức mạnh đến ngay; món nợ đến sau.”

**Cấu hình:** `SingleAlly`; `CompositeEffect`: `DamageBuffEffect(damageBonus = 8, duration = 1)` → `BleedEffect(bleedDamage = 10, duration = 2)`.

**Quyết định chiến thuật:**

- Chọn unit có thể tận dụng bonus ngay: một unit đang bị chặn đường hoặc đã dùng action sẽ khó thu được lợi ích trước khi trả giá.
- Chọn thời điểm all-in: đổi tối đa 20 damage danh nghĩa ở hai tick sau lấy khả năng hạ unit chặn đường trong lượt này. Bleed chịu modifier nhận damage, không phải chi phí HP bắt buộc xuyên Shield.
- Chọn ngân sách combo: trả thêm MP/card để bảo vệ unit mang khế ước, hay chấp nhận mất unit sau khi hoàn thành nhiệm vụ? Đây là đánh đổi tài nguyên, không có cơ chế tự hoàn MP khi unit chết.

**Ví dụ:** Halberdier chưa đánh và đang có nhiều mục tiêu hợp lệ. Cast trước skill để tận dụng bonus nếu skill đọc `GetCurrentDamage()`, rồi dùng unit khác khai thác khoảng trống vừa mở. Nếu chỉ còn một mục tiêu ít HP mà đòn thường đã đủ kết liễu, giữ card thường có lợi hơn.

**Combo có chủ đích:** Shield Spell có thể chặn tick Bleed; Ân Xá Bình Minh xóa Bleed nhưng giữ `DamageBuff`. Hai combo này tốn thêm card/MP và có thể khiến cái giá HP gần như biến mất. Nếu combo quá hiệu quả khi playtest, cần tăng giá/giảm bonus; không quảng bá Bleed là cái giá không thể tránh.

**Đối phó:** Stun/Freeze unit đã được buff, tránh đội hình dày trước skill vùng, hoặc dồn damage trước khi người chơi kịp hồi phục. Tái áp khế ước chỉ thay thế status cùng loại; không tích lũy damage bonus.

**Điểm cần cân bằng:** Với damage cơ sở 10, bonus 8 là mức tăng lớn; skill nhiều hit có thể hưởng nhiều lần. `duration = 1` vẫn có thể cho giá trị ở lượt cast và lượt kế tiếp theo vòng đời status hiện tại, không có nghĩa “chỉ đòn tiếp theo”. Nếu muốn chỉ một đòn, cần logic mới và đó chưa phải thiết kế đang đề xuất.

**Mức triển khai:** Dùng effect hiện có; cần kiểm tra unit nhiều hit, combo Shield/Cleanse và chết ở tick Bleed. Hình ảnh: ấn đỏ trên vũ khí, sợi máu nối về ngực unit.

### 5.2. Nhà Tù Từ Bi — Merciful Prison

**Mô tả trên card:** “Hồi 25 HP cho một unit bất kỳ, sau đó Root unit đó trong 2 lượt. Unit vẫn có thể tấn công nếu đủ điều kiện.”

**Cấu hình:** `AnyUnit`; `CompositeEffect`: `HealEffect(healAmount = 25)` → `RootEffect(duration = 2)`. Cùng một chuỗi effect cho cả hai phe, không có nhánh tự đổi lợi ích theo owner.

**Hai cách dùng trái ngược:**

1. **Cứu người giữ vị trí:** Hồi HP cho đồng minh đã đứng đúng ô, chấp nhận mất khả năng di chuyển. Phù hợp khi nhiệm vụ là giữ đường hoặc đánh từ vị trí hiện tại; bất lợi nếu cần rút khỏi hazard hoặc chạy tới cứ điểm khác.
2. **Cầm chân đối phương:** Root một unit cơ động để bảo vệ hướng tiếp cận, đổi lại hồi HP cho chính mục tiêu. Hợp khi khoảng cách tới cứ điểm quan trọng hơn việc hạ unit đó; bất lợi nếu unit địch đang đứng đúng vị trí bắn hoặc giữ một ô bạn cần đi qua.

**Ví dụ:** Đối phương có một unit gần đủ HP đang chuẩn bị chạy sang cứ điểm cuối. Hồi máu bị giới hạn bởi HP tối đa nên cái giá nhỏ, còn Root buộc họ dùng unit khác hoặc thanh tẩy. Ngược lại, dùng lên địch sắp chết có thể cứu chúng khỏi một đòn kết liễu và giữ nguyên vật cản trên đường của bạn.

**Combo và thứ tự:** Có thể hồi một đồng minh rồi dùng Cleanse sau để gỡ Root, nhưng phải chi thêm tài nguyên. Nếu mục tiêu có `HealBan`, Heal không có tác dụng còn Root vẫn được áp; đây là tương tác có thể khai thác lên địch. Lá này không xóa `HealBan`, không cho đi xuyên unit và không tự hủy Root khi nhận damage.

**Đối phó:** Cleanse để lấy lại khả năng di chuyển; tiếp tục tấn công từ vị trí hiện tại; chuyển nhiệm vụ chiếm điểm sang unit khác. Không coi Root là miễn displacement: hiệu ứng đẩy/teleport vẫn theo validation riêng của skill.

**Điểm cần cân bằng:** Root Spell hiện có giá 2 MP và tác động `AllEnemies`, nên Nhà Tù Từ Bi không phải lựa chọn tốt hơn để chỉ khống chế địch. Giá trị riêng là có thể chuyển giữa cứu đồng minh và cầm chân địch bằng cùng một slot card. Cần so tỷ lệ chọn thực tế trước khi chốt giá; không tự sửa Root Spell trong phạm vi đề xuất này.

**Mức triển khai:** Dùng effect hiện có và targeting `AnyUnit`. Kiểm tra cả hai phe, mục tiêu đầy HP, `HealBan`, Root cũ và buff/debuff đang tồn tại. Hình ảnh: dây leo sáng quấn quanh unit, hoa nở đồng thời với hồi máu.

### 5.3. Tối Hậu Thư Tro Tàn — Ashen Ultimatum

**Mô tả trên card:** “Đánh dấu một kẻ địch và ô đang đứng. Cuối lượt kế tiếp của phe đó, nếu mục tiêu còn ở ô đã đánh dấu: gây 18 damage rồi Root 1 lượt. Rời ô trước thời điểm kiểm tra để tránh hình phạt.”

**Cấu hình đề xuất:** `SingleEnemy`, giá 3 MP. Cast chỉ đặt dấu ấn; chưa gây damage hoặc Root ngay. Lưu runtime ID mục tiêu, ô gốc và lượt hết hạn. Đây là cơ chế mới, không thể tạo đủ hành vi bằng `CompositeEffect` đơn thuần.

**Quyết định chiến thuật:**

- Người cast chọn một ô đối thủ có lý do muốn giữ: vị trí bắn, lối hẹp hoặc vị trí bảo vệ đường tới cứ điểm. Dấu ấn trên một unit vốn đã định rời đi thường lãng phí MP.
- Đối thủ được thấy vị trí và hạn chót, rồi chọn bỏ vị trí, trả tài nguyên để hóa giải, hoặc chịu đòn để hoàn thành nhiệm vụ quan trọng hơn.
- Người cast quyết định có đầu tư thêm Root/Stun để ép trúng hay không. Combo tăng độ chắc chắn nhưng tốn thêm card/MP; giữ tài nguyên cho hướng khác có thể hiệu quả hơn.

**Ví dụ:** Archer địch giữ một lối hẹp từ vị trí thuận lợi. Dấu ấn buộc đối phương cân nhắc di chuyển khỏi ô bắn hoặc chấp nhận hình phạt để tiếp tục gây áp lực. Việc họ rời ô chỉ mở cơ hội tiếp cận; không làm cứ điểm tự mất owner và không trao cho bên cast một lượt di chuyển chen ngang.

**Luật phân giải đề xuất, cần triển khai rõ:**

1. Kiểm tra đúng một lần cuối lượt kế tiếp của phe mục tiêu, sau các hành động của phe đó và trước khi bắt đầu lượt bên kia. Không dùng tick đầu lượt hiện có làm hạn chót vì sẽ lấy mất cơ hội phản ứng.
2. So vị trí tại thời điểm kiểm tra: đi ra rồi quay lại ô gốc vẫn chịu phạt. Ở ô khác thì dấu ấn biến mất, không gây damage và không để lại hazard trên ô.
3. Dấu ấn theo runtime ID, không chuyển sang unit khác đi vào ô gốc. Mục tiêu chết trước hạn thì hủy dấu; không kích hoạt lên xác hoặc unit mới thay thế.
4. Dấu ấn là debuff có thể Cleanse. Áp lại cùng loại thay thế dấu cũ và hạn chót, không tạo nhiều lần nổ.
5. Khi trúng, dùng `DamageEffect(damage = 18)` rồi `RootEffect(duration = 1)` nếu mục tiêu còn sống. Damage chịu Shield/modifier và có thể phá Freeze như damage trực tiếp hiện tại. Root mới áp cuối lượt phải tồn tại qua lượt hành động kế tiếp của mục tiêu.
6. Nếu trận đã kết thúc, không phân giải dấu ấn. Không trì hoãn điều kiện thắng capture chỉ để chờ spell nổ.

**Combo và đối phó:** Root/Stun ép đối thủ cần Cleanse hoặc hỗ trợ di chuyển; Shield giảm phần damage nhưng không ngăn Root. Một skill displacement hợp lệ có thể cứu mục tiêu bằng cách đưa ra khỏi ô, nhưng đẩy địch ra quá sớm cũng tự làm mất hình phạt của mình. Đối thủ có thể chủ động chịu damage nếu di chuyển sẽ phá kế hoạch tấn công hoặc không còn ý nghĩa với kết quả trận.

**Điểm cần cân bằng:** Hình phạt bị né hoàn toàn nếu đối thủ rời ô, nhưng combo với Stun có thể làm mất lựa chọn đó. Bắt đầu ở 3 MP, 18 damage và Root 1 lượt; đánh giá tổng giá của combo khống chế, không chỉ sức mạnh từng lá. Do Root Spell hiện tại tác động nhiều địch, cần thử đặc biệt trường hợp khóa diện rộng rồi đặt dấu lên mục tiêu quan trọng.

**Phần tái sử dụng:** Targeting một địch, `DamageEffect`, `RootEffect`, pipeline MP/consume, Cleanse và event lượt hiện có.

**Phần phải bổ sung:** Effect đặt dấu, status lưu ô gốc/hạn chót, phân giải cuối lượt có guard chạy một lần; UI hiển thị ô và thời hạn; dữ liệu snapshot/rollback cho multiplayer. Không lưu ô gốc chỉ trong VFX hoặc giả định `ActiveStatusEffect` hiện tại đã đủ dữ liệu.

**Mức triển khai:** Cao hơn hai lựa chọn trên; cần kiểm chứng cuối lượt thủ công/timeout, di chuyển đi rồi về, displacement, Cleanse, target death, hết trận và đồng bộ host/client. Hình ảnh: ô gốc có vòng tro và ký hiệu đếm hạn, dấu ấn trên unit nối về ô.

### Gợi ý lựa chọn

- Muốn thử nhanh một lá có cách dùng linh hoạt theo cả hai phe: **Nhà Tù Từ Bi**.
- Muốn lối đánh all-in và xây combo bảo vệ unit chủ lực: **Khế Ước Huyết Chiến**.
- Muốn đấu trí vị trí, phản ứng của đối thủ và phối hợp nhiều lượt rõ nhất: **Tối Hậu Thư Tro Tàn**; đổi lại cần triển khai thêm logic.

## 6. Kiểm chứng khi triển khai asset

Các mục dưới đây chưa chạy; đây là điều kiện nghiệm thu cho bước triển khai sau:

- Tạo từng `SpellCardData` qua Unity Editor, nhập đúng thứ tự `CompositeEffect`, lưu và mở lại để xác nhận managed reference còn nguyên. Thêm vào nguồn spell selection/content catalog phù hợp với luồng trận đang dùng.
- Cast đúng/sai phe, target chết hoặc mất trước confirm, đủ/thiếu MP, cancel: chỉ cast thành công mới trừ đúng một lần MP và consume đúng một card.
- Ân Xá Bình Minh: thử `HealBan + Bleed`, Root, Stun, Freeze; buff cũ còn nguyên, heal không vượt HP tối đa, action đã dùng không được hoàn lại.
- Chiến Kỳ Phá Vây: thử damage thường, skill nhiều hit/nhiều mục tiêu, Shield cũ mạnh hơn, cast trước/sau action và vòng đời `duration = 1` qua lượt đối thủ.
- Huyết Băng Phong Ấn: thử hai tick Bleed không phá Freeze; hit trực tiếp phá Freeze và tạo Slow; thử Shield, Cleanse, Bleed cũ và mục tiêu chết ở tick đầu.
- Kiểm tra UI status/MP/hand, Console và đồng bộ host/client khi dùng trong multiplayer. Playtest ảnh hưởng lên tranh cứ điểm trước khi chốt giá MP.

## 7. Nguồn implementation đã đối chiếu

- `Assets/Scripts/SpellCard/SpellCardData.cs`: schema card và enum target/status.
- `Assets/Scripts/SpellCard/SpellEffects.cs`: effect và thứ tự `CompositeEffect`.
- `Assets/Scripts/SpellCard/BuffDebuffHandler.cs`: thay thế status cùng loại, tick, shield, cleanse và phá Freeze.
- `Assets/Scripts/Unit/UnitController.cs`: damage, heal, move/action và thời điểm tick.
- `Assets/Scripts/CapturePoint/CapturePointManager.cs`: capture và điều kiện sở hữu toàn bộ cứ điểm.
- `Assets/Scripts/Data/SpellData/*.asset` và các `UnitData` trong `Assets/Scripts/Data`: thông số tham khảo hiện tại.
