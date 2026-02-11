Dưới đây là lộ trình chi tiết chia làm **6 Giai đoạn (Phases)**:

---

### **GIAI ĐOẠN 1: PROTOTYPE (NGUYÊN MẪU THÔ) - 2~3 Tuần**

**Mục tiêu:** Chứng minh gameplay "vui" trước khi làm đẹp. Chỉ dùng hình khối (Cube, Capsule) đại diện cho unit. Chưa làm Multiplayer online (chỉ làm Local PvP trên cùng 1 máy).

1. **Thiết lập dự án:**
* Cài đặt Unity, thiết lập Github/Plastic SCM để quản lý source code (Rất quan trọng cho team 2 người).
* Chọn thư viện Networking (Photon Fusion, Mirror hoặc Unity Netcode) nhưng chưa code vội, chỉ cài sẵn.


2. **Hệ thống Grid & Map (Coder):**
* Tạo Grid Map (lưới lục giác hoặc ô vuông) cơ bản.
* Logic xác định vật cản, cứ điểm.


3. **Core Mechanics (Coder):**
* Hệ thống Turn-based: Chuyển lượt giữa Player A và Player B.
* Hệ thống Xúc xắc: Code logic random ra MP.
* Hệ thống Spawn Unit: Trừ MP -> Sinh ra một khối Cube trên map.
* Hệ thống Di chuyển & Tấn công cơ bản.


4. **Định hình Art Style (Artist):**
* Vẽ Concept Art cho 1 Unit (Ví dụ: Nghê Thần) và 1 Môi trường (Đầm Sen).
* Thử nghiệm Shader trong Unity: Làm sao để model 3D trông như tranh Đông Hồ/Sơn Mài? (Đây là key visual của game).



---

### **GIAI ĐOẠN 2: VERTICAL SLICE (LÁT CẮT DỌC) - 4~6 Tuần**

**Mục tiêu:** Một trận đấu hoàn chỉnh (Local) với đồ họa sơ khởi. Có đủ thắng/thua.

1. **Hoàn thiện Gameplay Loop (Coder):**
* Cơ chế chiếm cứ điểm (đứng lên đổi màu).
* Điều kiện thắng (chiếm hết cứ điểm) / Thua.
* Cơ chế tính giờ (Time limit dựa trên số Unit - *USPs của game*).


2. **Sản xuất Unit đầu tiên (Artist):**
* Model & Texture cho 4 Unit cơ bản: Nghê (Tank), Gà (Assassin), Xà Tinh (Mage), Tướng (Warrior).
* Animation đơn giản: Idle, Move, Attack. (Nên làm kiểu Rối nước - cử động cứng cáp để đỡ tốn công animate mượt).


3. **UI/UX Cơ bản (Artist + Coder):**
* Làm HUD: Thanh MP (hình nén nhang), Nút bấm, Đồng hồ đếm ngược.
* Hiển thị lưới di chuyển rõ ràng.



---

### **GIAI ĐOẠN 3: MULTIPLAYER & SYSTEM (XƯƠNG SỐNG) - 4~6 Tuần**

**Mục tiêu:** Hai người chơi được với nhau qua mạng (Internet/LAN). Đây là giai đoạn khó nhất của team 2 người.

1. **Networking (Coder):**
* Đồng bộ hóa (Sync) vị trí Unit, máu, MP giữa 2 máy.
* Đồng bộ hóa kết quả tung xúc xắc (Chống hack/cheat cơ bản).
* Xử lý ngắt kết nối (Reconnect).


2. **Lobby & Matchmaking (Coder):**
* Màn hình tạo phòng, tìm phòng.
* Đặt tên người chơi.


3. **VFX & SFX (Artist):**
* Làm hiệu ứng kỹ năng: Vệt mực tàu, bụi vàng, khói hương.
* Thêm âm thanh: Tiếng xúc xắc, tiếng di chuyển, nhạc nền (Đàn tranh).



---

### **GIAI ĐOẠN 4: CONTENT EXPANSION (ĐẮP THỊT) - 4 Tuần**

**Mục tiêu:** Thêm chiều sâu chiến thuật và nội dung.

1. **Hệ thống Spell/Thẻ bài (Coder):**
* Code logic sử dụng thẻ bài (Buff máu, tăng giáp, trói chân).
* Hệ thống Shop trong game (nếu có).


2. **Mở rộng Môi trường (Artist):**
* Hoàn thiện Map 1: Đầm Sen Tàn (Nước, lá sen, sương mù).
* Làm thêm Map 2: Cổ Loa hoặc Làng Tranh (nếu kịp tiến độ).


3. **Hệ thống Bot/AI (Coder):**
* Làm Bot đơn giản (Random hành động hoặc tìm đường ngắn nhất) để người chơi có thể test một mình.



---

### **GIAI ĐOẠN 5: META GAME & POLISH (ĐÁNH BÓNG) - 3 Tuần**

**Mục tiêu:** Tạo động lực chơi lại và làm game "sướng" hơn.

1. **Hệ thống Meta (Coder):**
* Lưu dữ liệu người chơi (Gold, Exp, Level).
* Màn hình Inventory, chọn bộ bài/đội hình trước khi vào trận.


2. **Polish (Artist + Coder):**
* **Juice:** Thêm độ rung màn hình (Screen shake) khi tấn công mạnh.
* Hiệu ứng chuyển cảnh (Màn sương/Cuộn tranh).
* Tối ưu hóa (Optimize) cho Android (Giảm dung lượng texture, giảm polygon).


3. **Cân bằng Game (Balancing):**
* Test xem Gà có quá mạnh không? Nghê có quá trâu không?
* Chỉnh sửa chỉ số MP cost, Damage, HP.



---

### **GIAI ĐOẠN 6: RELEASE (PHÁT HÀNH) - 2 Tuần**

**Mục tiêu:** Đưa game lên Store.

1. **Đóng gói & Build:**
* Build bản PC (Steam) và Android (APK/AAB).


2. **Marketing Assets (Artist):**
* Chụp ảnh màn hình (Screenshot), quay Trailer, làm Icon game.


3. **Store Setup (Coder):**
* Tích hợp Google Play Services, IAP (nếu có bán đồ), Ads (nếu có).



---

### **LỜI KHUYÊN CHO TEAM 2 NGƯỜI:**

1. **Giảm tải phần Art:** Vì chỉ có 1 Artist, hãy tận dụng **Asset Store** cho các vật thể môi trường (cây cối, đá, nước) và dùng Shader để biến chúng thành phong cách tranh vẽ. Chỉ nên tự model các Unit đặc thù (Nghê, Gà...).
2. **Đơn giản hóa Animation:** GDD nhắc đến "Rối nước/Hình nhân", hãy tận dụng điều này. Không cần làm animation nhuyễn như người thật. Làm kiểu giật cục, bay lơ lửng sẽ vừa đúng chất tâm linh, vừa đỡ tốn công.
3. **Logic "Auto Balance":** Cơ chế thời gian turn dựa trên số unit là con dao hai lưỡi. Coder cần test kỹ ngay từ giai đoạn 2. Nếu quá nhiều unit => thời gian quá dài => trận đấu bị lê thê.
4. **Cắt bỏ nếu cần:** Nếu thấy trễ tiến độ, hãy mạnh dạn cắt bỏ phần "Chế độ cốt truyện" và tập trung vào PvP. Cốt truyện có thể kể qua mô tả thẻ bài (như Dark Souls).