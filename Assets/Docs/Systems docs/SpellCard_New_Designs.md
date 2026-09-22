# Đề xuất 5 spell card mới

Ngày đối chiếu: 2026-09-21. Logic runtime và 5 `SpellCardData` assets đã được triển khai; chưa kiểm chứng trong Play Mode.

## 1. Cơ sở thiết kế

Dựa trên [Spell Card System](SpellCard_System.md), [Core Gameloop System](Core_Gameloop_System.md), [Characters Overview](Characters_Overview.md) và [Gameplay commands](Multiplayer_Gameplay_Commands.md), đối chiếu với code và asset hiện tại:

- Bốn lá trong `Assets/Scripts/Data/SpellData`: Damage Spell (1 MP, 4 damage), Root Spell (2 MP, Root 2 lượt), Shield Spell (1 MP, Shield 20 trong 2 lượt), Stun Spell (2 MP, Stun 1 lượt).
- Damage Spell, Root Spell và Stun Spell đang dùng `AllEnemies`; Shield Spell dùng `SingleAlly`. Thông số này là mốc tham khảo, chưa phải bằng chứng cân bằng.
- Các `UnitData` hiện có đang dùng `Health = 100`, `BaseDamage = 10`, `spawnCost = 3`. Chi 3 MP cho spell vì vậy cạnh tranh trực tiếp với việc triệu hồi thêm một unit.
- Thắng bằng capture yêu cầu sở hữu tất cả cứ điểm. Spell mới hỗ trợ giữ unit, mở đường và trì hoãn đối phương; không trực tiếp đổi owner của cứ điểm.

Ân Xá Bình Minh dùng effect thanh tẩy khống chế cứng và hồi máu theo phần trăm mới. Nhà Tù Từ Bi ghép effect đã có bằng `CompositeEffect`. Tối Hậu Thư Tro Tàn, Hiến Tế Hỏa Linh và Nham Sơn Trấn Lộ dùng effect chuyên biệt cùng `SpellRuntimeEffectManager`. Cả năm dùng `consumeOnUse = true`.

| Spell card | Vai trò | MP | Target | Effect theo thứ tự |
|---|---|---:|---|---|
| Ân Xá Bình Minh | Giải cứu, duy trì quân số | 2 | `SingleAlly` | `CleanseEffect` → `HealEffect` |
| Nhà Tù Từ Bi | Cứu quân mình nhưng giữ chân, hoặc giữ chân địch nhưng cứu chúng | 2 | `AnyUnit` | `HealEffect` → `RootEffect` |
| Tối Hậu Thư Tro Tàn | Ép tối đa hai địch di chuyển hoặc chịu damage và debuff | 3 | `AutomaticEnemies` | Dấu ấn → 20 damage → `Weaken(25%, 2)` → `GuardBreak(25%, 2)` |
| Hiến Tế Hỏa Linh | Đổi một unit lấy MP và Burn diện gần | 0 | `SingleAlly` | Hiến tế → nhận 3 MP → áp `Burn(10, 2)` trong phạm vi 1 ô |
| Nham Sơn Trấn Lộ | Tạo ô địa hình tạm thời không thể đi qua | 2 | `EmptyTile` | Tạo núi đá → chặn di chuyển trong 2 turn |

### Quy tắc dùng chung

- Ân Xá Bình Minh và Nhà Tù Từ Bi đề xuất `range = 0`. Theo luồng authority hiện tại, spell không có unit caster làm gốc tính khoảng cách; `SingleAlly` và `AnyUnit` chọn một unit trên bản đồ. Không hiểu `range = 0` là chỉ dùng lên bản thân. Tối Hậu Thư Tro Tàn tự chọn mục tiêu trên toàn map.
- `duration` theo lượt của unit nhận status, không theo tổng số lần hai phe đổi lượt. Status có hiệu lực ngay khi áp dụng; tick ở đầu lượt của unit và xóa khi hết hạn theo vòng đời action hiện tại. Buff cast trong lượt mình có thể tác dụng ngay trước tick đầu tiên.
- Status cùng `StatusEffectType` bị thay thế cả value và duration, không cộng dồn, không tự giữ giá trị mạnh hơn.
- Spell không cấp thêm move/action, không reset cooldown. Thanh tẩy status cũng không hoàn lại action đã dùng.

## 2. Ân Xá Bình Minh — Dawn Absolution

**Ý tưởng:** Một ấn sáng phá xiềng xích rồi khép miệng vết thương. Lá cứu viện cho unit đang bị khống chế hoặc sắp mất vị trí quan trọng.

**Mô tả trên card:** “Xóa toàn bộ hiệu ứng khống chế cứng của một đồng minh, sau đó hồi 20% max HP. Giữ nguyên buff và debuff mềm.”

| Trường | Giá trị đề xuất |
|---|---|
| `spellName` | `Ân Xá Bình Minh` |
| `mpCost` | `2` |
| `targetType` | `SingleAlly` |
| `range` | `0` |
| `consumeOnUse` | `true` |
| `spellEffect` | `CompositeEffect` |
| `effects[0]` | `HardCrowdControlCleanseEffect` |
| `effects[1]` | `PercentMaxHealthHealEffect`: `percent = 20` |

**Ứng dụng gameplay:** Cứu unit đang giữ vị trí khỏi `Stun` hoặc `Freeze/ Root` và hồi 20% max HP.`Bleed`, `HealBan` cùng các debuff mềm khác được giữ nguyên. Gỡ Stun/Freeze chỉ cho phép hành động nếu các cờ action còn hợp lệ.

**Thứ tự bắt buộc:** Gỡ `Stun`/`Freeze/Root` trước rồi hồi máu. `HealBan` không bị xóa nên vẫn có thể chặn phần hồi máu. Heal bị giới hạn bởi HP tối đa; card không hồi sinh unit đã chết và không tạo miễn nhiễm debuff cho các lượt sau.

**Đánh đổi và đối phó:** Không thêm sát thương hoặc shield. Đối phương có thể dồn damage kết liễu trước khi được cứu hoặc áp debuff trở lại. Dùng lên unit đầy HP và không có debuff không tạo lợi ích; không giả định hệ thống sẽ tự chặn lượt cast đó.

**Cân bằng thử nghiệm:** 20 HP tương đương 20% HP cơ bản hiện tại. Giá trị chính là thời điểm thanh tẩy; mức 2 MP buộc người chơi cân nhắc cứu quân hay giữ MP triệu hồi.

**Gợi ý hình ảnh:** Xiềng tím tan trong vòng sáng trắng vàng; nhịp hồi máu xuất hiện sau nhịp phá xiềng.

## 3. Nhà Tù Từ Bi — Merciful Prison

**Mô tả trên card:** “Hồi 25 HP cho một unit, sau đó Root unit đó trong 2 lượt. Unit vẫn có thể tấn công nếu đủ điều kiện.”

**Cấu hình:** `AnyUnit`; `CompositeEffect`: `HealEffect(healAmount = 25)` → `RootEffect(duration = 2)`. Cùng một chuỗi effect cho cả hai phe, không có nhánh tự đổi lợi ích theo owner.

**Hai cách dùng trái ngược:**

1. **Cứu người giữ vị trí:** Hồi HP cho đồng minh đã đứng đúng ô, chấp nhận mất khả năng di chuyển. Phù hợp khi nhiệm vụ là giữ đường hoặc đánh từ vị trí hiện tại; bất lợi nếu cần rút khỏi hazard hoặc chạy tới cứ điểm khác.
2. **Cầm chân đối phương:** Root một unit cơ động để bảo vệ hướng tiếp cận, đổi lại hồi HP cho chính mục tiêu. Hợp khi khoảng cách tới cứ điểm quan trọng hơn việc hạ unit đó; bất lợi nếu unit địch đang đứng đúng vị trí bắn hoặc giữ một ô bạn cần đi qua.

**Ví dụ:** Đối phương có một unit gần đủ HP đang chuẩn bị chạy sang cứ điểm cuối. Hồi máu bị giới hạn bởi HP tối đa nên cái giá nhỏ, còn Root buộc họ dùng unit khác hoặc thanh tẩy. Ngược lại, dùng lên địch sắp chết có thể cứu chúng khỏi một đòn kết liễu và giữ nguyên vật cản trên đường của bạn.

**Combo và thứ tự:** Có thể hồi một đồng minh rồi dùng Cleanse sau để gỡ Root, nhưng phải chi thêm tài nguyên. Nếu mục tiêu có `HealBan`, Heal không có tác dụng còn Root vẫn được áp; đây là tương tác có thể khai thác lên địch. Lá này không xóa `HealBan`, không cho đi xuyên unit và không tự hủy Root khi nhận damage.

**Đối phó:** Cleanse để lấy lại khả năng di chuyển; tiếp tục tấn công từ vị trí hiện tại; chuyển nhiệm vụ chiếm điểm sang unit khác. Không coi Root là miễn displacement: hiệu ứng đẩy/teleport vẫn theo validation riêng của skill.

**Điểm cần cân bằng:** Root Spell hiện có giá 2 MP và tác động `AllEnemies`, nên Nhà Tù Từ Bi không phải lựa chọn tốt hơn để chỉ khống chế địch. Giá trị riêng là có thể chuyển giữa cứu đồng minh và cầm chân địch bằng cùng một slot card. Cần so tỷ lệ chọn thực tế trước khi chốt giá; không tự sửa Root Spell trong phạm vi đề xuất này.

**Mức triển khai:** Dùng effect hiện có và targeting `AnyUnit`. Kiểm tra cả hai phe, mục tiêu đầy HP, `HealBan`, Root cũ và buff/debuff đang tồn tại. Hình ảnh: dây leo sáng quấn quanh unit, hoa nở đồng thời với hồi máu.

## 4. Tối Hậu Thư Tro Tàn — Ashen Ultimatum

**Mô tả trên card:** “Đánh dấu ngẫu nhiên tối đa 2 kẻ địch trên bản đồ, ưu tiên kẻ địch đang đứng trong capture point hoặc đang dính hiệu ứng ngăn di chuyển. Cuối lượt kế tiếp của phe đó, mỗi mục tiêu chưa di chuyển kể từ khi bị đánh dấu chịu X damage, sau đó nhận Weaken và GuardBreak.”

**Cấu hình triển khai thử nghiệm:** Giá 3 MP, `AutomaticEnemies`, tự động chọn tối đa hai enemy còn sống trên map và không mở bước chọn mục tiêu thủ công. Cast chỉ đặt dấu ấn; chưa gây damage hoặc debuff ngay. Mặc định: 20 damage, `Weaken(25%, 2)` và `GuardBreak(25%, 2)`; các field vẫn chỉnh được trong asset để playtest.

**Quy tắc chọn mục tiêu:**

1. Lập danh sách enemy còn sống và đang hiện diện trên map tại thời điểm spell được phân giải. Không tính unit đang spawn/despawn hoặc đã chết.
2. Chia ứng viên theo thứ tự ưu tiên: thỏa cả hai điều kiện; chỉ đứng trong capture point hoặc chỉ chịu khống chế cứng; không thỏa điều kiện nào. Chọn ngẫu nhiên không lặp trong nhóm ưu tiên cao nhất còn ứng viên, rồi tiếp tục xuống nhóm sau cho tới khi đủ hai mục tiêu.
3. “Đứng trong capture point” nghĩa là vị trí hiện tại thuộc một capture point, không phụ thuộc owner của điểm.
4. “Khống chế cứng” dùng đúng trạng thái khóa action hiện tại: `Stun` hoặc `Freeze`. `Root` không được tính là khống chế cứng vì chỉ khóa di chuyển.
5. Nếu chỉ có một enemy hợp lệ thì đánh dấu một mục tiêu. Nếu không có enemy hợp lệ thì cast không thành công, không trừ MP và không consume card.

**Luật theo dõi di chuyển và phân giải:**

1. Mỗi dấu ấn lưu runtime ID mục tiêu, phe mục tiêu, thời hạn và cờ đã di chuyển. Hai mục tiêu được theo dõi độc lập.
2. Cờ đã di chuyển bật khi vị trí tile của mục tiêu thay đổi thành công sau lúc nhận dấu ấn. Di chuyển chủ động, displacement và teleport đều được tính; đi ra rồi quay lại vẫn được xem là đã di chuyển.
3. Kiểm tra đúng một lần ở cuối lượt kế tiếp của phe mục tiêu, sau toàn bộ hành động của phe đó và trước khi bắt đầu lượt bên kia. Không dùng tick đầu lượt làm hạn chót.
4. Mục tiêu đã di chuyển: xóa dấu, không gây damage và không áp debuff. Mục tiêu chưa di chuyển: mặc định chịu 20 damage; nếu còn sống, nhận `Weaken(25%, 2)` và `GuardBreak(25%, 2)`.
5. Damage chịu Shield và modifier nhận damage hiện có. `Weaken` giảm outgoing damage theo `Value`%; `GuardBreak` tăng incoming damage theo `Value`%. Mỗi status dùng value và duration riêng sẽ được chốt khi cân bằng.
6. Dấu ấn là debuff có thể Cleanse. Mục tiêu chết hoặc rời map trước hạn thì hủy dấu. Nếu trận đã kết thúc, không phân giải dấu ấn và không trì hoãn điều kiện thắng capture.
7. Áp lại cùng loại lên một mục tiêu thay thế dấu cũ và thời hạn, không tạo nhiều lần phân giải.

**Quyết định chiến thuật và đối phó:** Ưu tiên capture point và hard CC làm spell gây áp lực mạnh lên unit đang giữ vị trí hoặc khó tự di chuyển. Đối thủ có thể dùng move, displacement, teleport hoặc Cleanse để tránh hình phạt. Root không được ưu tiên như hard CC, nhưng có thể ngăn mục tiêu tự di chuyển; Stun/Freeze vừa tăng ưu tiên chọn vừa làm việc né phạt khó hơn.

**Điểm cần cân bằng:** Các giá trị 20 damage, 25%/2 lượt cho hai debuff và giá 3 MP là mặc định thử nghiệm, chưa phải mốc cân bằng cuối. Cần đánh giá sức mạnh tổng hợp khi hai mục tiêu đều đang ở capture point hoặc bị hard CC, cùng khả năng ép đối thủ tiêu tốn hai hành động di chuyển.

**Phần tái sử dụng:** Pipeline MP/consume, enemy enumeration, `DamageEffect`, `Weaken`, `GuardBreak`, Cleanse, capture-point lookup và event lượt hiện có.

**Đã triển khai:** Targeting tự động có ưu tiên và random không lặp; effect đặt dấu; state lưu runtime ID/cờ di chuyển; hook mọi thay đổi tile; phân giải cuối lượt một lần; rollback và snapshot multiplayer. Random do authority quyết định. **Còn thiếu:** icon/VFX riêng và hiển thị thời hạn dấu trên UI.

**Mức triển khai:** Cao hơn hai lựa chọn trên. Cần kiểm chứng chọn mục tiêu theo từng tầng ưu tiên, số enemy dưới hai, di chuyển chủ động, displacement, teleport, đi rồi quay lại, Cleanse, target death/despawn, hết trận và đồng bộ host/client.

## 5. Hiến Tế Hỏa Linh — Ember Sacrifice

**Mô tả trên card:** “Hy sinh một unit đồng minh để nhận lại MP. Khi unit đó chết, áp Burn lên toàn bộ kẻ địch trong phạm vi 1 ô quanh vị trí hiến tế.”

**Cấu hình triển khai thử nghiệm:** `SingleAlly`, `range = 0`, `mpCost = 0`, `consumeOnUse = true`. Mặc định nhận 3 MP và áp `Burn(10, 2)`; các field vẫn chỉnh được trong asset.

**Luật phân giải:**

1. Chỉ chọn unit đồng minh còn sống và đang hiện diện trên map. Không chọn unit đang spawn/despawn hoặc đã được đánh dấu chết.
2. Khi confirm, lưu tile hiện tại rồi hiến tế unit ngay lập tức. Hiến tế đặt HP về 0 và chạy đúng một lần luồng chết chuẩn; không phải damage nên bỏ qua Shield, `StanceGuard`, `GuardBreak`, miễn nhiễm và các modifier damage.
3. Chỉ sau khi xác nhận unit đã chết thành công, mặc định cộng 3 MP cho owner của card qua `MPManager.AddMP`. MP không vượt `MaxMP = 20`; phần vượt trần mất đi, không chuyển thành tài nguyên khác.
4. Lấy toàn bộ enemy còn sống trong phạm vi map-distance 1 tính từ tile tử trận và mặc định áp `Burn(10, 2)`. Không áp Burn lên đồng minh; unit hiến tế không phải mục tiêu.
5. Burn chỉ là status, không gây damage ngay lúc cast. Burn tick ở đầu lượt của từng unit theo vòng đời status hiện tại.
6. Nếu target không còn hợp lệ trước lúc authority resolve, cast thất bại, không consume card và không thay đổi MP. Toàn bộ chết, nhận MP và áp Burn phải nằm trong cùng transaction để rollback cùng nhau khi có lỗi.
7. Unit chết có thể làm trống capture point hoặc thay đổi điều kiện trận. Phân giải hiến tế, MP và Burn trước khi phát snapshot/kết quả game cuối cùng; không cho callback chết kích hoạt effect hai lần.

**Đánh đổi và đối phó:** Giá thật của card là một unit đang sống. Chọn unit ít HP hoặc đã hoàn thành vai trò giảm thiệt hại; chọn vị trí gần nhiều enemy tăng giá trị Burn. Đối thủ có thể giãn đội hình, dùng Cleanse hoặc Shield để giảm tác động các tick Burn.

**Điểm cần cân bằng:** Xác nhận 3 MP, Burn 10/2 lượt và việc có cho hiến tế unit cuối cùng của một phe hay không. So lợi ích MP với `spawnCost = 3`, giá trị unit còn lại và số enemy trung bình trong radius 1; không để vòng lặp spawn unit rồi hiến tế tạo MP ròng vô hạn.

**Phần tái sử dụng:** Targeting `SingleAlly`, `MPManager.AddMP`, `MapManager.GetUnitsInRange`, `StatusEffectType.Burn`, Cleanse, luồng chết unit và transaction/rollback phía authority.

**Đã triển khai:** Effect hiến tế gọi nhánh chết chuẩn qua `TrySacrifice`, không đi qua damage; MP/Burn là field cấu hình; kết quả chết, MP và status đi qua snapshot hiện có. **Còn thiếu:** icon/VFX riêng và playtest vòng lặp tài nguyên.

**Gợi ý hình ảnh:** Unit tan thành tro lửa, luồng năng lượng quay về thanh MP; vòng lửa bùng ra trong phạm vi 1 ô rồi để lại biểu tượng Burn trên enemy trúng hiệu ứng.

## 6. Nham Sơn Trấn Lộ — Stonewall Rise

**Mô tả trên card:** “Tạo một núi đá trên ô trống chỉ định. Ô đó không thể đi qua trong 2 turn.”

**Cấu hình triển khai thử nghiệm:** `EmptyTile`, `mpCost = 2`, `range = 0`, `consumeOnUse = true`, duration cố định 2 turn. Đây là temporary terrain/blocker, không phải unit hoặc status.

**Target hợp lệ:**

1. Tile tồn tại, vốn walkable, đang trống và chưa có blocker tạm thời.
2. Không đặt dưới unit, trên obstacle có sẵn, spawn point hoặc capture point. Hạn chế spawn/capture tránh trạng thái không thể spawn hoặc không thể hoàn thành điều kiện thắng.
3. Authority kiểm tra lại target khi resolve. Nếu tile vừa bị chiếm hoặc trở thành không hợp lệ, cast thất bại, không trừ MP và không consume card.

**Luật tồn tại và chặn đường:**

1. Núi đá xuất hiện ngay sau khi cast thành công và tồn tại qua hai lần kết thúc player turn kế tiếp; mỗi `OnPlayerTurnEnded` giảm duration một lần. Ví dụ cast giữa lượt Player 1: giảm còn 1 khi Player 1 kết thúc lượt và biến mất khi Player 2 kết thúc lượt.
2. Khi tồn tại, tile không hợp lệ cho pathfinding, điểm đến move, spawn, displacement và teleport của cả hai phe. Không unit nào có thể đi xuyên hoặc kết thúc trên tile đó.
3. Theo yêu cầu hiện tại, núi đá chỉ chặn di chuyển. Nó không mặc định chặn line of sight, attack hoặc projectile; muốn chặn các luồng này cần quyết định thiết kế riêng.
4. Khi hết hạn, despawn VFX/prefab và gỡ blocker. Tile trở lại trạng thái walkable ban đầu nếu không còn blocker khác; không tự thay đổi preset hay dữ liệu map gốc.
5. Cast lại lên cùng tile khi blocker còn tồn tại không hợp lệ, không cộng dồn hoặc refresh duration.
6. Blocker phải do authority tạo/tick/xóa và nằm trong snapshot/rollback. Client chỉ hiển thị state đã được đồng bộ.

**Ứng dụng gameplay:** Khóa lối hẹp, buộc đối thủ đi vòng, bảo vệ hướng tiếp cận hoặc trì hoãn tiếp viện tới capture point. Người chơi phải cân nhắc thời điểm cast vì duration giảm ở mỗi lần kết thúc player turn, không phải hai lượt riêng của caster.

**Điểm cần cân bằng:** Chốt `mpCost`, range cast và giới hạn số núi đá cùng tồn tại. Playtest map có hành lang một ô để tránh khóa toàn bộ đường đi hoặc nhốt vĩnh viễn unit giữa blocker và biên map.

**Phần tái sử dụng:** Targeting tile, `MapManager`, pathfinding, event `OnPlayerTurnEnded`, transaction/rollback và object pool cho VFX.

**Đã triển khai:** Registry temporary blocker độc lập với asset bên thứ ba; validation cho pathfinding/move/spawn/displacement/teleport; tick duration; rollback và snapshot multiplayer. Không sửa trực tiếp dữ liệu `TilePreset` hoặc YAML map. **Còn thiếu:** prefab/VFX núi đá và Play Mode test cho map hành lang hẹp.

**Gợi ý hình ảnh:** Cụm đá nhô lên từ mặt đất với bụi và vết nứt; hiển thị số turn còn lại trên chân núi, sau đó vỡ vụn và trả tile về trạng thái cũ.
## 7. Kiểm chứng khi triển khai asset

Các mục dưới đây chưa chạy; đây là điều kiện nghiệm thu cho bước triển khai sau:

- Đã tạo 5 `SpellCardData` trong `Assets/Scripts/Data/SpellData` qua Unity Editor; hai card dùng `CompositeEffect` đã giữ đúng thứ tự effect khi serialize. Còn cần thêm vào nguồn spell selection/content catalog phù hợp với luồng trận đang dùng.
- Cast đúng/sai phe, target chết hoặc mất trước confirm, đủ/thiếu MP, cancel: chỉ cast thành công mới trừ đúng một lần MP và consume đúng một card.
- Ân Xá Bình Minh: thử `HealBan + Bleed`, Root, Stun, Freeze; buff cũ còn nguyên, heal không vượt HP tối đa, action đã dùng không được hoàn lại.
- Nhà Tù Từ Bi: thử cả hai phe, mục tiêu đầy HP, `HealBan`, Root cũ và buff/debuff đang tồn tại.
- Tối Hậu Thư Tro Tàn: thử random không lặp theo từng tầng ưu tiên; 0/1/2+ enemy; capture point; `Stun`/`Freeze`; di chuyển chủ động, displacement, teleport và đi rồi quay lại; Cleanse; target death/despawn; hết trận; đồng bộ target và kết quả random giữa host/client.
- Hiến Tế Hỏa Linh: thử target chết/mất trước confirm, Shield và modifier damage, MP gần/đúng trần, không có hoặc có nhiều enemy trong radius 1, Burn cũ, Cleanse, capture point, rollback và callback chết chỉ chạy một lần.
- Nham Sơn Trấn Lộ: thử tile occupied/non-walkable/spawn/capture, hành lang hẹp, pathfinding, move, spawn, displacement, teleport, hai lần end-turn, hết hạn khôi phục tile, rollback và đồng bộ blocker/VFX.
- Kiểm tra UI status/MP/hand, Console và đồng bộ host/client khi dùng trong multiplayer. Playtest ảnh hưởng lên tranh cứ điểm trước khi chốt giá MP.

## 8. Nguồn implementation đã đối chiếu

- `Assets/Scripts/SpellCard/SpellCardData.cs`: schema card và enum target/status.
- `Assets/Scripts/SpellCard/SpellEffects.cs`: effect và thứ tự `CompositeEffect`.
- `Assets/Scripts/SpellCard/BuffDebuffHandler.cs`: thay thế status cùng loại, tick, shield, cleanse và phá Freeze.
- `Assets/Scripts/Unit/UnitController.cs`: damage, heal, move/action và thời điểm tick.
- `Assets/Scripts/CapturePoint/CapturePointManager.cs`: capture và điều kiện sở hữu toàn bộ cứ điểm.
- `Assets/Scripts/Data/SpellData/*.asset` và các `UnitData` trong `Assets/Scripts/Data`: thông số tham khảo hiện tại.
- `Assets/Scripts/Manager/MPManager.cs`: cộng MP có giới hạn `MaxMP = 20`.
- `Assets/Scripts/Manager/MapManager.cs`: unit lookup, range và validation tile hiện tại.
- `Assets/Scripts/Unit/UnitController.cs`: damage và luồng chết hiện tại.
- `Assets/Scripts/Skills/TileHazardManager.cs`: mẫu state theo tile, tick cuối lượt và snapshot/rollback.
