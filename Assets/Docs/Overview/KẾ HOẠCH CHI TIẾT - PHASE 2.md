# KẾ HOẠCH TRIỂN KHAI GIAI ĐOẠN 2: VERTICAL SLICE (LÁT CẮT DỌC)
**Thời gian dự kiến:** 4 - 6 Tuần (~30-40 ngày)  
**Mục tiêu cốt lõi:** Một trận đấu Local PvP hoàn chỉnh, có đủ điều kiện Thắng/Thua, Cơ chế chiếm cứ điểm, và đồ họa ở mức định hình phong cách (chưa cần quá chi tiết).

---

## 📅 SPRINT 1: CƠ CHẾ CỐT LÕI & ĐỊNH HÌNH UNIT (Ngày 1 - 10)
**Mục tiêu:** Xây dựng xong logic "Chiếm điểm" và đưa 2 Unit đầu tiên vào game.

### 🧑‍💻 CODER (Unity Logic)
1. **Hệ thống Cứ Điểm (Capture Point):**
   - [ ] Tạo Prefab `CapturePoint` trên bản đồ.
   - [ ] Code logic `OnUnitEnter`: Khi Unit đứng lên điểm -> Đổi màu cờ/ánh sáng theo phe (Player 1/Player 2).
   - [ ] Lưu trạng thái sở hữu của từng điểm.
2. **Hệ thống Điều Kiện Thắng (Win Condition):**
   - [ ] Code logic kiểm tra cuối turn: Nếu một bên chiếm **tất cả** cứ điểm -> Gọi hàm `EndGame()`.
   - [ ] Tạo UI tạm thông báo "Player X Wins".
3. **Cơ chế Time Limit Động (Dynamic Timer):**
   - [ ] Viết hàm tính toán thời gian mỗi turn: `Time = (Số Unit hiện có * 10s) + 20s`.
   - [ ] Hiển thị đồng hồ đếm ngược (đơn giản, dạng số) lên màn hình.
   - [ ] Xử lý hết giờ -> Tự động chuyển Turn.

### 🎨 ARTIST (Asset Production)
1. **Model & Texture Unit 1: Nghê Thần (Tank):**
   - [ ] Dựng model Low-poly (Phong cách tượng đá/gốm).
   - [ ] Texture style: Gốm men rạn hoặc Đá xanh rêu.
2. **Model & Texture Unit 2: Tướng Lĩnh (Warrior):**
   - [ ] Dựng model, trang phục giáp trụ thời Trần/Lê.
   - [ ] Texture style: Sơn mài (Đỏ son + Vàng kim).
3. **Rigging & Animation Cơ bản:**
   - [ ] Rig khung xương đơn giản cho Nghê và Tướng.
   - [ ] Anim `Idle`: Đứng thở nhè nhẹ (như tượng sống).
   - [ ] Anim `Move`: Di chuyển kiểu "Rối nước" (lướt đi hoặc giật cục, không cần bước chân chuẩn).

---

## 📅 SPRINT 2: GAMEPLAY HOÀN CHỈNH & FULL ROSTER (Ngày 11 - 20)
**Mục tiêu:** Hoàn thiện 4 Unit cơ bản và lắp ghép Gameplay Loop (Spawn -> Move -> Capture -> Attack).

### 🧑‍💻 CODER (Unity Logic)
1. **Tích hợp Unit mới:**
   - [ ] Thay thế Cube/Capsule cũ bằng Model Nghê và Tướng đã xong ở Sprint 1.
   - [ ] Thiết lập chỉ số (HP, Damage, Move Range) riêng cho từng loại Unit.
2. **Hệ thống Tấn Công (Basic Combat):**
   - [ ] Code logic chọn mục tiêu trong tầm đánh -> Trừ HP.
   - [ ] Xử lý Unit chết: Play animation chết -> Xóa khỏi bàn cờ -> Cập nhật lại số lượng Unit để tính giờ.
3. **Hệ thống UI chọn hành động:**
   - [ ] Làm lại menu hành động góc màn hình: Nút Spawn, Nút End Turn.
   - [ ] Hiển thị thanh máu (Health Bar) trên đầu mỗi Unit.

### 🎨 ARTIST (Asset Production)
1. **Model & Texture Unit 3: Gà "Đại Cát" (Assassin):**
   - [ ] Style: Tranh Đông Hồ 3D (Viền nét to, màu tươi).
2. **Model & Texture Unit 4: Xà Tinh (Mage):**
   - [ ] Style: Rối nước, thân hình uốn lượn, màu tối (Phe U Linh).
3. **Animation Tấn công (Attack):**
   - [ ] Làm Anim tấn công cho cả 4 Unit (đơn giản 1 đòn duy nhất).
   - [ ] Anim `Die`: Vỡ vụn (như gốm) hoặc rũ xuống (như rối đứt dây).
4. **UI Assets (Draft 1):**
   - [ ] Vẽ icon cho nút Spawn, End Turn.
   - [ ] Vẽ khung viền chân dung 4 nhân vật.

---

## 📅 SPRINT 3: UI/UX & MÔI TRƯỜNG (Ngày 21 - 30)
**Mục tiêu:** Biến một bản Prototype thô thành "Vertical Slice" đẹp mắt, có không khí game.

### 🧑‍💻 CODER (Unity Logic)
1. **Hệ thống UI/UX (Theo GDD):**
   - [ ] Thay thế thanh MP tiêu chuẩn bằng asset **"Nén Hương"** hoặc **"Bình Mực"**.
   - [ ] Code logic thanh MP giảm dần/cháy dần.
   - [ ] Hiển thị lưới di chuyển (Grid) rõ ràng hơn (dùng Shader phát sáng thay vì đổi màu gạch đơn điệu).
2. **Tích hợp Model môi trường & VFX:**
   - [ ] Đưa map "Đầm Sen Tàn" vào game.
   - [ ] Gắn VFX đòn đánh và VFX Spawn vào đúng thời điểm animation.
3. **Camera & Polish:**
   - [ ] Camera tự động zoom/pan nhẹ vào Unit đang hoạt động.
   - [ ] Rung màn hình (Screen shake) nhẹ khi tấn công trúng.

### 🎨 ARTIST (Asset Production)
1. **Môi trường: Map 1 - Đầm Sen Tàn:**
   - [ ] Model Lá sen (Ô di chuyển).
   - [ ] Shader mặt nước: Màu đen mực tàu, có phản chiếu mờ.
   - [ ] Hậu cảnh: Sương mù, lau sậy (đơn giản).
2. **VFX (Hiệu ứng):**
   - [ ] VFX Spawn: Khói hương hoặc giấy cháy.
   - [ ] VFX Hit: Vệt mực bắn ra khi trúng đòn.
3. **Hoàn thiện UI:**
   - [ ] Asset Thanh MP (Nén hương cháy đỏ đầu, tàn tro rơi).
   - [ ] Icon Xúc xắc (Khối gỗ mộc).

---

## 📝 TỔNG KẾT MILESTONE (Cuối ngày 30)
**Kết quả bàn giao:**
1. Một file Build (PC/Android) chơi được mượt mà 2 người (Local).
2. Đầy đủ 4 Unit với Animation cơ bản.
3. Map Đầm Sen Tàn hoàn chỉnh.
4. Giao diện người dùng mang phong cách Dân gian Việt Nam.

**Lưu ý cho team:** 
* Nếu Artist làm không kịp, ưu tiên hoàn thiện model trước, animation có thể làm rất tối giản (chỉ cần nhảy giật cục).
* Coder cần test kỹ công thức tính giờ (Time Limit), nếu thấy quá nhanh hoặc quá chậm cần điều chỉnh tham số ngay.