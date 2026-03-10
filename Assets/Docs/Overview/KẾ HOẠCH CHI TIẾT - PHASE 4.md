# KẾ HOẠCH TRIỂN KHAI GIAI ĐOẠN 4: CONTENT EXPANSION (ĐẮP THỊT)
**Thời gian dự kiến:** 4 Tuần (~24-28 ngày)  
**Mục tiêu cốt lõi:** Mở rộng chiều sâu chiến thuật cho trận đấu bằng hệ thống Spell/Card, hoàn thiện ít nhất 1 bản đồ có bản sắc rõ ràng, và bổ sung Bot/AI đủ dùng để người chơi có thể tự test gameplay ngay cả khi không có đối thủ Local/Online.

---

## 🎯 MỤC TIÊU CỦA PHASE 4
1. **Tăng chiều sâu chiến thuật:** Người chơi không chỉ Spawn - Move - Attack mà còn có thêm lớp quyết định từ Spell/Card.
2. **Tăng giá trị chơi lại:** Mỗi trận có thêm biến số từ deck, buff/debuff và khác biệt địa hình map.
3. **Tăng tốc độ test nội bộ:** Có Bot/AI để coder và artist tự test flow game mà không cần luôn có người chơi thứ hai.
4. **Chuẩn bị nền cho Phase 5:** Các hệ thống Phase 4 phải đủ sạch để về sau nối vào Meta Game, cân bằng chỉ số và polish mà không phải đập đi làm lại.

---

## 📦 PHẠM VI BÀN GIAO BẮT BUỘC

### 🧑‍💻 CODER
1. **Hệ thống Spell Card tối thiểu có thể chơi được:**
   - [ ] Có cấu trúc data cho Spell Card bằng `ScriptableObject`.
   - [ ] Có UI để chọn và sử dụng Spell Card trong lượt.
   - [ ] Có tối thiểu **3 hiệu ứng cơ bản**:
     - [ ] Buff máu hoặc hồi máu.
     - [ ] Buff giáp hoặc giảm sát thương nhận vào.
     - [ ] Trói chân / cấm di chuyển / debuff control đơn giản.
   - [ ] Có hệ thống giới hạn sử dụng: MP cost, sau khi sử dụng, spell card sẽ bị hủy.
2. **Map Content Expansion:**
   - [ ] Hoàn thiện map `Đầm Sen Tàn` ở mức gameplay + thẩm mỹ cơ bản.
   - [ ] Có ít nhất **1 yếu tố gameplay đọc được từ môi trường**: vật cản, choke point, vùng kiểm soát, hoặc đường vòng chiến thuật.
   - [ ] Nếu kịp tiến độ, dựng blockout cho map thứ 2 (`Cổ Loa` hoặc `Làng Tranh`).
3. **Bot/AI cơ bản:**
   - [ ] Bot tự kết thúc được 1 lượt hoàn chỉnh.
   - [ ] Bot biết ưu tiên hành động cơ bản: Spawn, Move, Attack.
   - [ ] Bot biết dùng ít nhất 1 Spell/Card đơn giản khi có lợi.
   - [ ] Có chế độ debug để quan sát quyết định của AI.

### 🎨 ARTIST
1. **Map 1 hoàn thiện thẩm mỹ:**
   - [ ] Nước, lá sen, sương mù, vật thể trang trí, silhouette hậu cảnh.
   - [ ] Phân lớp vật thể để camera nhìn rõ ô đi và unit.
2. **FX cho Spell/Card:**
   - [ ] VFX hồi máu.
   - [ ] VFX buff giáp.
   - [ ] VFX khống chế / trói chân.
3. **Icon/UI hỗ trợ hệ Spell/Card:**
   - [ ] Icon thẻ bài hoặc kỹ năng.
   - [ ] Khung bài / tooltip / trạng thái buff-debuff.
4. **Nếu còn thời gian:**
   - [ ] Concept hoặc blockout art cho map 2.

---

## 📅 SPRINT 1: THIẾT KẾ KHUNG HỆ SPELL/CARD (Ngày 1 - 7)
**Mục tiêu:** Xây nền data + logic đủ tốt để từ Sprint 2 trở đi chỉ việc đổ content vào, tránh hard-code từng spell riêng lẻ.

### 🧑‍💻 CODER (Unity Logic)
1. **Chốt luật vận hành Spell/Card:**
   - [ ] Xác định rõ dùng mô hình nào: `Spell trực tiếp`, `Card rút từ deck`, hoặc `Card đại diện cho skill tiêu hao`.
   - [ ] Chốt tài nguyên tiêu hao: dùng `MP`, `charge`, `cooldown`, hoặc kết hợp.
   - [ ] Chốt thời điểm được dùng: đầu lượt, giữa lượt, sau di chuyển, hoặc thay thế hành động tấn công.
2. **Thiết kế data-driven architecture:**
   - [ ] Tạo `SpellCardData` hoặc mở rộng từ hệ `SkillBase` đang có.
   - [ ] Tách riêng các thuộc tính: tên, icon, mô tả, cost, cooldown, range, target type, effect type.
   - [ ] Chuẩn hóa enum hoặc strategy cho nhóm hiệu ứng: `Heal`, `Shield`, `Root`, `DamageBuff`, `Cleanse`.
3. **Luồng sử dụng Spell/Card:**
   - [ ] Chọn unit hoặc player caster.
   - [ ] Chọn spell/card từ UI.
   - [ ] Highlight target hợp lệ trên grid.
   - [ ] Xác nhận dùng spell.
   - [ ] Trừ tài nguyên, kích hoạt hiệu ứng, cập nhật UI/effect log.
4. **Hệ thống Buff/Debuff runtime:**
   - [ ] Có container lưu trạng thái effect theo unit.
   - [ ] Có thời gian tồn tại theo turn.
   - [ ] Có hook rõ ràng cho `OnTurnStart`, `OnTurnEnd`, `BeforeTakeDamage`, `CanMove`.

### 🎨 ARTIST (UX + Visual Direction)
1. **Định hình ngôn ngữ hình ảnh cho Spell/Card:**
   - [ ] Chốt form hiển thị: thẻ bài giấy dó, bùa chú, ấn triện, hay ô kỹ năng dạng khay.
   - [ ] Chốt mã màu cho 3 nhóm chính:
     - [ ] Hồi phục / hỗ trợ.
     - [ ] Phòng thủ / bảo hộ.
     - [ ] Khống chế / nguyền chú.
2. **UI draft cho Spell/Card panel:**
   - [ ] Vẽ layout panel kỹ năng/thẻ bài.
   - [ ] Vẽ tooltip hiển thị cost, tầm dùng, cooldown.
   - [ ] Thiết kế icon trạng thái buff/debuff hiển thị trên đầu unit hoặc góc HUD.

### ✅ Kết quả cần đạt cuối Sprint 1
1. Có tài liệu mini-spec cho luật Spell/Card.
2. Có data structure đủ để tạo spell mới mà không sửa sâu gameplay loop.
3. Có mock UI hoặc prefab UI cơ bản để bắt đầu gắn content thật.

---

## 📅 SPRINT 2: HIỆN THỰC SPELL/CARD & TÍCH HỢP HUD (Ngày 8 - 14)
**Mục tiêu:** Người chơi dùng được spell thật trong trận, có feedback rõ ràng, và hệ thống đủ ổn định để bắt đầu cân bằng.

### 🧑‍💻 CODER (Unity Logic)
1. **Implement 3 Spell/Card lõi của Phase 4:**
   - [ ] `Hồi Sinh Khí` hoặc tương đương: Hồi HP cho 1 unit hoặc vùng nhỏ.
   - [ ] `Linh Giáp` hoặc tương đương: Tăng giáp / giảm sát thương trong X turn.
   - [ ] `Trói Chân` hoặc tương đương: Unit mục tiêu không được di chuyển trong 1 turn.
2. **Tích hợp vào turn flow hiện tại:**
   - [ ] Dùng spell không phá vỡ logic `ActionLefts`, `EndTurn`, `Timer`.
   - [ ] Giảm cooldown đúng ở đầu hoặc cuối lượt theo luật đã chốt.
   - [ ] Nếu target không hợp lệ thì không bị mất tài nguyên.
3. **Hiển thị trạng thái trong UI:**
   - [ ] Icon buff/debuff hiển thị trên unit.
   - [ ] Tooltip hoặc combat log báo spell vừa dùng.
   - [ ] Disable nút spell nếu không đủ MP hoặc chưa hết cooldown.
4. **Debug & balancing tools:**
   - [ ] Có log để biết spell nào đã dùng, lên ai, còn bao nhiêu turn.
   - [ ] Có inspector debug hoặc panel test để force add spell/card nhanh.

### 🎨 ARTIST (VFX + UI Assets)
1. **Sản xuất asset trực quan cho 3 spell/card lõi:**
   - [ ] Icon riêng cho từng spell.
   - [ ] Hiệu ứng cast và hiệu ứng impact.
   - [ ] Marker highlight tile khi chọn mục tiêu.
2. **Hoàn thiện cảm giác UI:**
   - [ ] Thẻ bài/nút kỹ năng có trạng thái sáng/tối rõ ràng.
   - [ ] Có visual cho cooldown hoặc unavailable state.
   - [ ] Tooltip đủ dễ đọc ở độ phân giải PC.

### ✅ Kết quả cần đạt cuối Sprint 2
1. Trận đấu có thể dùng spell/card thật từ đầu đến cuối.
2. 3 spell/card lõi hoạt động ổn định, đọc được hiệu ứng bằng mắt.
3. UI đủ rõ để tester không cần hỏi lại cách dùng.

---

## 📅 SPRINT 3: MỞ RỘNG MÔI TRƯỜNG & TẠO ÁP LỰC CHIẾN THUẬT (Ngày 15 - 21)
**Mục tiêu:** Map không chỉ đẹp hơn mà còn hỗ trợ gameplay, tạo tình huống buộc người chơi phải suy nghĩ vị trí, tuyến tiến công và kiểm soát không gian.

### 🧑‍💻 CODER (Gameplay + Level Logic)
1. **Hoàn thiện map gameplay `Đầm Sen Tàn`:**
   - [ ] Rà soát lại spawn point, choke point, đường tiếp cận cứ điểm.
   - [ ] Tạo ít nhất 2 tuyến chiến thuật rõ ràng: đường an toàn và đường rủi ro.
   - [ ] Điều chỉnh vật cản để unit melee và ranged đều có đất diễn.
2. **Tích hợp logic địa hình nếu cần thiết:**
   - [ ] Ô chặn di chuyển.
   - [ ] Ô khó tiếp cận hoặc phải đi đường vòng.
   - [ ] Nếu còn thời gian: ô có hiệu ứng nhẹ như tăng phòng thủ hoặc giảm tầm nhìn giả lập.
3. **Chuẩn bị map 2 ở mức blockout:**
   - [ ] Chọn 1 trong 2 hướng: `Cổ Loa` hoặc `Làng Tranh`.
   - [ ] Làm greybox để test flow di chuyển, chiếm điểm, spawn.
   - [ ] Kiểm tra map có khác biệt gameplay với `Đầm Sen Tàn`, tránh chỉ đổi skin.

### 🎨 ARTIST (Environment Art)
1. **Hoàn thiện visual map `Đầm Sen Tàn`:**
   - [ ] Nước mang cảm giác âm u, tĩnh, có chiều sâu.
   - [ ] Lá sen, đài sen, cọc gỗ, sương và silhouette nền hỗ trợ không khí dân gian - huyền dị.
   - [ ] Phân biệt foreground / midground / background để tránh rối hình.
2. **Gameplay readability:**
   - [ ] Không để mesh hoặc FX che mất unit, ô lưới, vùng di chuyển.
   - [ ] Vật cản gameplay phải nhìn phát hiểu ngay.
3. **Nếu còn thời gian:**
   - [ ] Làm concept nhanh cho map 2.
   - [ ] Xác định palette và key props đặc trưng cho map 2.

### ✅ Kết quả cần đạt cuối Sprint 3
1. Có 1 map đủ đẹp để đem đi test hoặc quay footage nội bộ.
2. Bản đồ tạo khác biệt chiến thuật rõ ràng, không còn cảm giác sân test phẳng.
3. Có blockout map 2 để chuẩn bị cho Phase sau nếu đội ngũ còn sức.

---

## 📅 SPRINT 4: BOT/AI TESTABLE + TINH CHỈNH HỆ THỐNG (Ngày 22 - 28)
**Mục tiêu:** Có Bot đủ dùng để tự chơi thử, giúp phát hiện lỗi gameplay và hỗ trợ cân bằng mà không lệ thuộc hoàn toàn vào test thủ công 2 người.

### 🧑‍💻 CODER (AI Logic)
1. **Thiết kế Bot ở mức đơn giản nhưng hữu ích:**
   - [ ] Bot không cần thông minh như người chơi thật, nhưng phải luôn đưa ra quyết định hợp lệ.
   - [ ] Ưu tiên cấu trúc `Utility Score` hoặc `Priority Rule` thay vì AI quá phức tạp.
2. **Thứ tự ra quyết định cơ bản:**
   - [ ] Nếu đủ MP và thiếu quân tuyến đầu -> ưu tiên spawn.
   - [ ] Nếu có thể kết liễu mục tiêu -> ưu tiên attack.
   - [ ] Nếu có thể dùng spell mang lợi cao -> dùng spell.
   - [ ] Nếu không tấn công được -> di chuyển về ô có lợi hơn.
   - [ ] Nếu không còn hành động tốt -> end turn.
3. **Các heuristic tối thiểu:**
   - [ ] Ưu tiên tiến gần cứ điểm chưa chiếm.
   - [ ] Ưu tiên đánh unit máu thấp trong tầm.
   - [ ] Không đứng vào ô bị kẹt hoàn toàn nếu có lựa chọn khác.
   - [ ] Không dùng spell buff lên unit sắp chết nếu không có giá trị thực tế.
4. **Debugability:**
   - [ ] In log lý do chọn hành động.
   - [ ] Có delay giữa các quyết định để quan sát.
   - [ ] Có toggle `Fast AI` cho test regression nhanh.
5. **Stability pass:**
   - [ ] AI không bị treo lượt.
   - [ ] AI không spam action vô nghĩa vô hạn.
   - [ ] AI xử lý được trường hợp không còn target hợp lệ.

### 🎨 ARTIST (Hỗ trợ đọc Bot/AI)
1. **Feedback trực quan cho hành động AI:**
   - [ ] Indicator đơn giản khi AI đang chọn mục tiêu.
   - [ ] Highlight spell hoặc ô đích AI sắp thực hiện.
2. **Hoàn thiện các trạng thái còn thiếu:**
   - [ ] Icon buff/debuff còn thiếu.
   - [ ] Visual state cho unit bị root / shield / heal.

### ✅ Kết quả cần đạt cuối Sprint 4
1. Có thể chạy trận đấu 1 người vs Bot.
2. Bot hoàn thành được toàn bộ lượt mà không kẹt.
3. Spell/Card, map, combat loop và AI đã nối thành một vòng test hoàn chỉnh.

---

## 🧪 CHECKLIST NGHIỆM THU CUỐI PHASE 4

### Gameplay
- [ ] Người chơi dùng được spell/card trong trận mà không phá flow turn-based.
- [ ] Buff/Debuff tồn tại đúng số turn và tự hết hiệu lực.
- [ ] Không có lỗi mất lượt khi dùng spell lên target sai hoặc target chết giữa chừng.
- [ ] Map `Đầm Sen Tàn` hỗ trợ combat và chiếm điểm tốt hơn bản prototype cũ.
- [ ] Bot chơi hết một trận cơ bản mà không bị soft-lock.

### UI/UX
- [ ] Người chơi nhìn vào là biết spell nào dùng được, spell nào đang cooldown.
- [ ] Trạng thái root / shield / heal được đọc rõ bằng icon hoặc VFX.
- [ ] Combat log hoặc feedback đủ để debug khi có bug gameplay.

### Content
- [ ] Có ít nhất 3 spell/card lõi hoàn chỉnh.
- [ ] Có 1 map hoàn chỉnh và 1 map blockout nếu kịp.
- [ ] Có bộ icon và VFX đủ cho gameplay hiện tại.

---

## ⚠️ RỦI RO & PHƯƠNG ÁN CẮT SCOPE
1. **Rủi ro lớn nhất: Spell/Card làm vỡ gameplay loop hiện có.**
   - Giải pháp: Không thêm quá nhiều loại effect ngay. Phase 4 chỉ nên tập trung vào 3 nhóm effect dễ kiểm soát: heal, shield, root.
2. **Rủi ro thứ hai: AI ngốn quá nhiều thời gian.**
   - Giải pháp: Làm Bot theo rule-based trước. Không viết AI chiến thuật sâu ở Phase này.
3. **Rủi ro thứ ba: Artist quá tải vì vừa map vừa UI vừa VFX.**
   - Giải pháp: Ưu tiên theo thứ tự `Map 1 readability` -> `Spell VFX cốt lõi` -> `Map 2 concept/blockout`.
4. **Nếu trễ tiến độ:**
   - [ ] Cắt bỏ map 2 hoàn chỉnh, chỉ giữ greybox.
   - [ ] Giữ Bot chỉ biết Spawn/Move/Attack, chưa cần dùng spell thông minh.
   - [ ] Giảm số lượng spell/card xuống còn 2 nhưng phải polish tốt.

---

## 📝 ĐỀ XUẤT THỨ TỰ ƯU TIÊN CHO TEAM 2 NGƯỜI
1. **Tuần 1 - 2:** Coder khóa hệ Spell/Card trước, Artist làm song song UI draft + icon + FX cơ bản.
2. **Tuần 3:** Artist dồn lực cho `Đầm Sen Tàn`, Coder tích hợp gameplay readability và kiểm tra layout map.
3. **Tuần 4:** Coder tập trung AI + sửa bug tích hợp, Artist vá các feedback trực quan còn thiếu.

---

## 🏁 TỔNG KẾT MILESTONE CUỐI PHASE 4
**Kết quả bàn giao mong muốn:**
1. Một bản build có thêm lớp chiến thuật từ Spell/Card.
2. Một map đủ chất lượng để dùng làm chuẩn hình ảnh và test gameplay nghiêm túc.
3. Một Bot cơ bản giúp team tự test mọi lúc.
4. Một nền hệ thống sạch để bước sang giai đoạn cân bằng, polish và meta progression ở Phase 5.

**Lưu ý cho team:**
* Đừng biến Phase 4 thành giai đoạn “nhồi content” thiếu kiểm soát. Mỗi spell mới đều làm tăng gánh nặng UI, VFX, QA và AI.
* Nếu phải chọn giữa “nhiều content” và “content ít nhưng chạy ổn”, hãy chọn phương án thứ hai.
* Bot ở giai đoạn này là công cụ test trước khi là tính năng bán hàng. Mục tiêu chính là hỗ trợ phát triển, không phải đánh bại người chơi.