# Tài liệu thiết kế game

## 1. Thông tin sản phẩm

| Thuộc tính | Giá trị |
|---|---|
| Tên làm việc | The Summoners: Ordain and Abyss |
| Thể loại | Turn-based strategy, tactical grid, deck/spell support |
| Góc nhìn | Isometric 3D |
| Engine | Unity `6000.3.9f1`, Universal Render Pipeline `17.3.0` |
| Nền tảng mục tiêu | PC trước; Android sau khi hoàn tất tối ưu và điều khiển |
| Chế độ hiện có | Local player đấu AI |
| Chế độ mục tiêu | Local hoàn chỉnh; multiplayer chỉ triển khai khi có quyết định kiến trúc mạng |

## 2. Tầm nhìn

Người chơi vào vai một Thầy Pháp triệu hồi linh thể trên bàn cờ, dùng Linh Lực để hóa hình đơn vị
và thi triển bùa chú. Mỗi lượt là lựa chọn giữa mở rộng lực lượng, chiếm vị trí, tấn công và dùng
spell card để thay đổi thế trận.

### Trụ cột thiết kế

1. **Quyết định rõ ràng**: người chơi hiểu chi phí, tầm, mục tiêu hợp lệ và kết quả dự kiến trước
   khi xác nhận hành động.
2. **Vị trí quan trọng**: đường đi, tầm đánh, choke point và capture point tạo giá trị chiến thuật.
3. **Đội hình có bản sắc**: mỗi unit có vai trò, kỹ năng và quan hệ khắc chế dễ nhận biết.
4. **Không khí Việt huyền dị**: mỹ thuật dân gian, chất liệu giấy, gỗ, gốm, sơn mài và nghi lễ
   trấn yểm định hình hình ảnh và âm thanh.
5. **Trận đấu gọn**: lượt không bị kẹt, feedback nhanh, số hệ thống vừa đủ cho team nhỏ duy trì.

## 3. Thuật ngữ trong game

| Thuật ngữ kỹ thuật | Tên hiển thị đề xuất | Ý nghĩa |
|---|---|---|
| Turn | Lượt | Khoảng thời gian một phe được hành động |
| MP/Mana | Linh Lực | Tài nguyên dùng để triệu hồi, sử dụng skill và dùng spell card |
| Dice roll | Gieo Quẻ | Cơ chế cấp hoặc biến thiên tài nguyên theo lượt |
| Spawn Unit | Hóa Hình | Đưa unit từ bộ bài vào spawn point |
| Capture Point | Trấn Điểm | Vị trí chiến lược quyết định quyền kiểm soát/thắng trận |
| Spell Card | Phù/Pháp Bài | Hiệu ứng dùng từ bộ spell của người chơi |

Tên hiển thị phải được chốt thống nhất trước localization; identifier trong code không đổi theo tên
marketing.

## 4. Vòng lặp trận đấu

1. Chọn đội hình tối đa 6 unit và tối đa 4 spell trước trận.
2. Khởi tạo map, manager, spawn point, capture point, MP và hai phe.
3. Bắt đầu lượt của phe hiện tại; reset trạng thái hành động của unit và tính thời gian lượt.
4. Người chơi nhận/quản lý MP, sau đó chọn một hoặc nhiều hành động hợp lệ:
   - Hóa Hình unit tại spawn point và trả MP.
   - Chọn unit, xem vùng đi, di chuyển.
   - Dùng normal attack miễn phí hoặc trả MP để dùng skill trên mục tiêu hợp lệ.
   - Chọn spell card, chọn mục tiêu, xác nhận và trả MP.
5. Capture point cập nhật khi unit chiếm vị trí liên quan.
6. Người chơi kết thúc lượt hoặc timer hết; buff/debuff, cooldown và trạng thái unit được cập nhật.
7. Đổi phe và lặp lại cho đến khi điều kiện thắng được kích hoạt.

### Trạng thái implementation

- **Hiện có**: turn state, timer, spawn, MP, movement, attack, skill, spell card, buff/debuff,
  capture point, endgame UI và AI cơ bản.
- **Một phần**: AI chỉ ưu tiên spawn ngẫu nhiên, tiến gần đối thủ và attack; chưa có utility score,
  spell usage hoặc ưu tiên capture point.
- **Cần kiểm chứng**: toàn bộ vòng lặp từ vào trận đến endgame trong Play Mode, đặc biệt target chết
  giữa effect, timeout và chuyển lượt liên tiếp.

## 5. Luật chiến đấu

### Unit

`UnitData` định nghĩa `Health`, `BaseDamage`, `spawnCost`, `moveRange`, `moveSpeed`, icon và mô tả.
`UnitRuntimeStats` giữ state theo trận như owner, HP, vị trí và cờ hành động.

Mỗi unit trong một lượt có state di chuyển và hành động. UI chỉ cho phép lệnh khi đúng phe, đúng lượt,
mục tiêu hợp lệ và còn tài nguyên/trạng thái tương ứng.

### Skill

Skill là `ScriptableObject` kế thừa `SkillBase`, có loại skill, target mask, range, cooldown, `mpCost`,
animation cue và VFX. Normal attack cũng đi qua skill pipeline nhưng có chi phí 0 MP. Các skill còn lại
chỉ được thi triển khi chủ sở hữu unit đủ MP; MP được trừ khi skill bắt đầu thi triển và không bị trừ khi
hành động không hợp lệ. Hiệu ứng có thể gây damage, heal, buff/debuff, dịch chuyển hoặc tạo hazard trên tile.

### Spell card

Spell card dùng MP và có thể bị loại khỏi hand sau khi dùng. Target hiện được mô hình hóa bằng
`SingleAlly`, `SingleEnemy`, `Self`, `AllAllies`, `AllEnemies`, `AnyUnit`. Effect hỗ trợ heal, shield,
damage buff, cleanse, root, damage, stun, freeze, bleed và composite effect.

`AllAllies` và `AllEnemies` là mục tiêu thiết kế nhưng validation/execution diện rộng chưa hoàn chỉnh.
`Slow` giảm move range theo phần trăm và `Weaken` giảm outgoing damage theo phần trăm. `Freeze` khóa
hành động 2 lượt; hit trực tiếp đầu tiên nhận thêm 20% damage, phá Freeze và chuyển số lượt còn lại
thành `Slow(20)`.

### Điều kiện thắng

Điều kiện hiện có dựa trên quyền sở hữu capture point và gọi `TurnManager.TriggerGameEnd`. Trước khi
release cần chốt rõ:

- Số capture point tối thiểu trên từng map.
- Chiếm ngay khi đứng lên hay giữ qua cuối lượt.
- Hòa hoặc hết thời gian trận xử lý thế nào.
- Unit chết hết có phải điều kiện thua độc lập hay không.

## 6. Nội dung và đội hình

Roster hiện có 7 hướng unit: Berserker, Knight, Magician, Smasher, Halberdier, Assassin và Archer.
Vai trò mục tiêu:

| Unit | Vai trò chính | Giá trị chiến thuật |
|---|---|---|
| Berserker | Bruiser | Đổi HP/rủi ro lấy sát thương |
| Knight | Tank/guard | Giữ vị trí và giảm tác động cưỡng chế |
| Magician | Ranged control | Sát thương phép và kiểm soát vùng |
| Smasher | Area disruption | Sát thương vùng, hazard hoặc đẩy mục tiêu |
| Halberdier | Melee reach | Kiểm soát tuyến và nhiều mục tiêu |
| Assassin | Mobile finisher | Tiếp cận, bleed và kết liễu |
| Archer | Ranged damage | Gây áp lực từ xa và multi-hit |

Chỉ số cuối cùng phải được cân bằng bằng trận test; giá trị trong asset hiện tại không phải cam kết
thiết kế cuối.

## 7. Map và môi trường

Map chủ đạo: **Đầm Sen Tàn**. Mục tiêu gameplay là có spawn zone rõ, ít nhất hai tuyến tiếp cận,
capture point dễ đọc và vật cản không che grid/unit. Hướng hình ảnh gồm nước tối, sen tàn, cọc gỗ,
sương và silhouette kiến trúc dân gian.

Map thứ hai có thể theo hướng **Cổ Loa** hoặc **Làng Tranh**, nhưng chỉ bắt đầu sau khi map đầu đạt
tiêu chí gameplay và camera readability. Chưa chốt map thứ hai là phạm vi bắt buộc.

## 8. UI/UX

### Ngoài trận

- Main Menu: vào trận, Prepare Battle, Inventory; Shop và Settings còn thiếu.
- Prepare Battle: chọn tối đa 6 unit và 4 spell, validation trước khi vào trận.
- Inventory: xem collection và chi tiết unit/spell.
- Shop: mục tiêu tương lai; chưa có screen/service hoàn chỉnh.

### Trong trận

HUD phải hiển thị phe/lượt, timer, MP, action, unit selection, spawn panel, skill button, spell hand,
target highlight, buff/debuff và kết quả trận. Hành động không hợp lệ phải có lý do rõ mà không trừ
tài nguyên.

## 9. Hình ảnh và âm thanh

### Art direction

- Hình khối low-poly, silhouette rõ ở camera isometric.
- Palette nâu gỗ, đen mực, đỏ son, vàng kim, xanh rêu; màu phe phải tách khỏi màu môi trường.
- Chất liệu tham chiếu: tranh Đông Hồ/Hàng Trống, giấy dó, gốm men rạn, sơn mài và rối nước.
- VFX ngắn, đọc được timing cast/release/impact; không che tile hoặc trạng thái mục tiêu.

### Audio direction

- Nhạc nền dùng nhạc cụ dân tộc theo hướng tiết chế, ưu tiên không gian và nhịp chiến thuật.
- SFX bắt buộc: chọn/hủy, gieo quẻ, spawn, move, attack, hit, death, skill cast/impact, spell,
  capture, đổi lượt, victory và defeat.

## 10. Phạm vi sản phẩm

### Bản local khả dụng

- Một map hoàn chỉnh, 7 unit có vai trò rõ, ít nhất 4 spell ổn định.
- AI chơi hết trận không soft-lock.
- Menu → chuẩn bị đội hình → battle → result → quay lại menu hoạt động liên tục.
- Có save/load profile và deck; có setting âm thanh/đồ họa cơ bản.
- Không có lỗi Console nghiêm trọng trong smoke test.

### Ngoài phạm vi hiện tại

- Multiplayer, reconnect, lobby và matchmaking chưa có kiến trúc runtime.
- IAP, Ads và store release chưa được thiết kế chi tiết.
- Live service, account backend và cloud save chưa được cam kết.

## 11. Chỉ số cần đo khi playtest

- Thời lượng trận và thời lượng trung bình mỗi lượt.
- Tỷ lệ MP dành cho spawn so với skill và spell.
- Pick rate, win rate và survival rate của từng unit.
- Tần suất người chơi gặp invalid action hoặc không hiểu target.
- Số lượt để chiếm/giành lại capture point.
- Số lần AI không tìm được hành động và số trận AI bị soft-lock.

## 12. Quyết định còn mở

- Luật thắng cuối cùng và xử lý hòa.
- Có giữ cơ chế Gieo Quẻ ngẫu nhiên hay cấp MP cố định.
- Chế độ multiplayer có còn thuộc roadmap phát hành đầu tiên hay không.
- Mô hình kinh doanh và phạm vi Shop.
- Nền tảng phát hành đầu tiên sau PC prototype.
