# **TÀI LIỆU THIẾT KẾ GAME (GAME DESIGN DOCUMENT)**

## **TÊN GAME: The Summoners: Ordain and Abyss**

* **Phiên bản:** 0.1  
* **Ngày cập nhật cuối:** 11/02/2026

## 

## **PHẦN 1: TẦM NHÌN TỔNG QUAN**

### **1.1. Ý Tưởng Cốt Lõi (Elevator Pitch)**

- *Mô tả game của bạn trong 1-3 câu ngắn gọn.*  
  * \[Điền vào đây...\]

### **1.2. Thể Loại (Genre)**

- Roguelike Pvp turn based  
- Nền tảng Android & Steam

### **1.4. Đối Tượng Mục Tiêu (Target Audience)**

- Người chơi ưa thử thách, yêu thích chiến thuật, thích sự sáng tạo  
- Người chơi có tính kiên nhẫn  
- Có thể giới hạn từ độ tuổi 12 \~ 40  
- Có thể dành thời gian \~ 30p cho 1 ván game, \~60p cho 1 ngày để chơi game

### **1.5. Điểm Khác Biệt Độc Đáo (Unique Selling Points \- USPs)**

- Hệ thống **xúc xắc kết hợp quyết định chiến thuật** (spawn unit, dùng skill, hay giữ điểm MP) \=\> kết hợp giữa chiến thuật \+  xác suất \+ rủi ro có chủ đích  
- PvP chiến thuật tập trung vào chiếm cứ điểm \+ giữ vị thế, không chỉ tiêu diệt địch  
- Cơ chế thời gian turn theo số lượng unit, đây là một cơ chế **tự cân bằng (auto balance)** độc đáo, khi sức mạnh đi kèm với áp lực tư duy  
- Sự kết hợp giữa **deck-building** và **điều khiển unit** đem đến chiều sâu chiến thuật

## 

## **PHẦN 2: LỐI CHƠI & CƠ CHẾ**

### **2.1. Vòng Lặp Gameplay Cốt Lõi (Core Gameplay Loop)**

- **Chiến đấu:** Người chơi bắt đầu trận đấu 1 vs 1 với máy hoặc với người chơi khác   
- Khi đến turn của mình, người chơi tung xúc xắc/ spawn và điều khiển unit/ sử dụng kỹ năng đặc biệt,...  
- Hạ gục người chơi đối phương bằng cách sử dụng unit để chiếm hết cứ điểm trên map  
- **Phát triển:** người chơi nhận tiền và kinh nghiệm sau mỗi trận đấu, sau khi lên cấp sẽ mở khóa thêm kỹ năng (hoặc đơn vị lính) cao cấp hơn. Có mức rank khi thi đấu Pvp.

### 

### **2.2. Mục Tiêu & Điều Kiện Thắng/Thua**

- **Mục tiêu của người chơi:** Đưa người chơi vào một đấu trường căng thẳng, giúp người chơi phát huy tối đa khả năng sáng tạo và chiến thuật của mình, tăng tính khó đoán và may rủi bằng xúc xắc.  
- **Điều kiện thắng:** Người chơi chiếm được hết các cứ điểm trên map  
- **Điều kiện thua:** Ngược lại với thắng

### 

### **2.3. Cơ Chế Chi Tiết (Detailed Mechanics)**

     **2.3.1 Hành Động Của Người Chơi:** 

- Người chơi bắt đầu với 0 điểm MP cơ bản, điểm MP sẽ được tích bằng cách tung xúc xắc (tối đa 2 lượt tung mỗi turn).  
- Khi bắt đầu từ lượt, người chơi được quyền lựa chọn 2 trong 4 hành động: tung  xúc xắc số 1,  tung xúc xắc số 2, spawn unit, sử dụng spell đặc biệt. Tại mỗi lượt, người chơi đều có thể điều khiển tất cả unit trên màn chơi của mình (mỗi unit sẽ có phạm vi di chuyển và 1 số hành động đặc biệt ).  
- Khi không có điểm MP, nút spawn unit và play spell sẽ bị xám báo hiệu không thể sử dụng.

     **2.3.3. Hệ Thống Chiến Đấu:**

- **Môi trường chiến đấu:** grid map có chướng ngại vật, thành trì của 2  người chơi sẽ đối diện nhau, cứ điểm sẽ được đặt ngẫu nhiên trên map (\~4 cứ điểm) .  
- **Hệ thống unit:** mỗi unit sẽ có phạm vi di chuyển nhất định, có damage, hp và có thể có thêm 1 kỹ năng phụ (ví dụ tàng hình, đặt bẫy, …), có giới hạn số unit tối đa được phép xuất hiện trên màn chơi của 1 người chơi.  
- **Hệ thống spell phụ trợ:** (spell sẽ được thể hiện bằng thẻ bài) \- *nghiên cứu sau.*  
- **Hệ thống turn based:** mỗi turn của người chơi sẽ được giới hạn thời gian( giây)  \= số unit mà người chơi đang có trên map \* 10 \+ bonus 20s (cho việc sử dụng spell và spawn unit hoặc tính toán chiến thuật).  
- **Hệ thống cứ điểm:** khi bất kì unit nào đặt chân lên cứ điểm, cứ điểm đó sẽ được đánh dấu là bị chiếm (bởi người chơi sở hữu unit đó), khi tất cả cứ điểm bị chiếm bởi 1 người chơi duy nhất thì game sẽ kết thúc.

     ** 2.3.4. Hệ Thống Kinh Tế:**

- Người chơi sẽ nhận được gold và điểm exp sau mỗi trận đấu  
- Exp dùng để lên cấp, mở khóa unit mới và spell card mới  
- Gold dùng để mua vật phẩm trong shop  
- Hệ thống vật phẩm: skill card, card border, khung avatar, skin cho unit, rương, …  
- Bổ sung các gói IAP chứa vật phẩm và gold.

     **2.3.5. Một số hệ thống có thể có trong tương lai:**

- Thêm các class Summoner có những skill nội tại riêng biệt \=\> tăng tính chiến thuật

 

### **2.4. Các Chế Độ Chơi (Game Modes)**

- Chế độ chơi đơn với bot, và chơi multiplayer

## 

## **PHẦN 3: CỐT TRUYỆN, THẾ GIỚI & NHÂN VẬT** 

## **CHỦ ĐỀ MỚI: CÕI MỘNG & THUẬT TRẤN YỂM (The Realm of Reverie)**

### **3.1. Tóm Tắt Cốt Truyện (Story Synopsis)**

- ## **Tiền đề:** Thế giới này không được tạo ra từ Big Bang, mà được vẽ nên bởi "Cây Bút Tạo Hóa" trên nền giấy Dó cổ xưa. Thế giới được gọi là "Đại Nam Huyễn Cảnh".

- ## Sự cân bằng của thế giới dựa trên hai dòng chảy năng lượng: Thanh Khí (Trật tự, đại diện bởi Trống Đồng) và Trọc Khí (Hỗn mang, đại diện bởi Mực Tàu/Bóng Tối).

- ## **Xung đột:** Một vết nứt xuất hiện trên bầu trời (Vết Rách Hư Không), khiến các bức tranh cổ, tượng điêu khắc và các thần thú trong truyền thuyết nổi dậy, mất kiểm soát.

- ## **Vai trò người chơi:** Bạn là một Pháp Sư Tập Sự (The Mystic) thuộc phái *Trấn Yểm*. Bạn sử dụng bộ Xúc Xắc Ngũ Hành để gieo quẻ, điều khiển các linh thú và tái lập lại trật tự cho Huyễn Cảnh trước khi mực tàu nuốt chửng tất cả.

### **3.2. Bối Cảnh & Xây Dựng Thế Giới (Setting & World Building)**

####      **3.2.1. Phong Cách Mỹ Thuật (Visual Key)**

##      Thay vì Gothic phương Tây, thế giới sẽ mang đậm nét Mỹ thuật thời Lý \- Trần \- Lê kết hợp           Tranh Dân Gian (Đông Hồ, Hàng Trống):

- ## **Vật liệu chủ đạo:** Giấy dó, gỗ sơn son thếp vàng, gốm men rạn, đồng thau đen.

- ## **Họa tiết:** Hoa sen, rồng thời Lý (thân trơn, mềm mại), vân mây, sóng nước.

####      **3.2.2. Các Phe Phái (Thay thế Ordain & Abyss)**

- ## **Phe "Thiên Cơ" (The Celestial Order)** \- Thay cho Ordain:

  * ## Triết lý: Tin vào quy luật, sự bảo thủ và nghi lễ.

  * ## Biểu tượng: Mặt trời trên mặt Trống Đồng Đông Sơn.

  * ## Hình ảnh Unit: Các chiến binh mặc giáp trụ thời Trần, tượng Hộ Pháp (thường thấy ở chùa), Tiên nữ cưỡi Hạc.

  * ## Màu sắc: Vàng kim, Đỏ son, Nâu đất (Sơn mài).

- ## **Phe "U Linh" (The Void Spirits)** \- Thay cho Abyss:

  * ## Triết lý: Tin vào sự biến đổi, tự do và sức mạnh của bóng tối.

  * ## Biểu tượng: Con mắt vẽ bằng mực tàu loang lổ.

  * ## Hình ảnh Unit: Các con rối nước bị nguyền rủa, Quỷ Dạ Xoa, các hình nhân thế mạng (Vàng mã cách điệu), linh hồn trôi dạt.

  * ## Màu sắc: Đen mực tàu, Xanh chàm, Tím than.

####      **3.3.3. Địa Danh (Maps)**

- ## **Đầm Sen Tàn:** Một đầm sen khổng lồ với các lá sen là ô di chuyển, nước đen ngòm và sương khói mờ ảo.

- ## **Cổ Loa Thành:** Chiến đấu trên các vòng thành ốc xoắn, với nỏ thần là các tháp canh (cứ điểm).

- ## **Làng Tranh Ma Quái:** Một ngôi làng giấy dó nơi nhà cửa là các bức tranh dựng đứng, có thể bị xé rách hoặc đốt cháy.

### **3.3. Nhân Vật (Characters)**

####      **3.3.1. Nhân Vật Chính – "Thầy Pháp" (The Master)**

- ## **Ngoại hình**: Mặc áo giao lĩnh cách điệu, đeo chuỗi hạt, tay cầm một chiếc Ấn Triện (dùng để triệu hồi) và bộ Xúc Xắc Gỗ.

- ## **Cơ chế liên quan cốt truyện:**

## *Tung xúc xắc*: Được gọi là "Gieo Quẻ".

## *MP (Mana):* Gọi là "Linh Lực".

## *Spawn Unit*: Gọi là "Hóa Hình" (Vẽ ra hoặc triệu hồi từ giấy/gỗ).

####      **3.3.2. Hệ Thống Unit (Ví dụ minh họa)**

##      Thay vì Goblins/Orcs, hãy sử dụng các sinh vật huyền thoại Việt Nam:

- ## **Nghê Thần (Tanker):** Tượng đá hóa sinh, chịu đòn tốt.

- ## **Gà "Đại Cát" (Assassin):** Lấy cảm hứng từ tranh Đông Hồ, tấn công nhanh.

- ## **Xà Tinh/Thuồng Luồng (Mage):** Tấn công tầm xa bằng độc hoặc nước.

- ## **Tướng Lĩnh (Warrior):** Cầm khiên mây, đao kiếm.

####    

####        **3.3.3. Nhân Vật Phụ (NPC)**

- ## **Bà Lão Bán Trà (The Oracle):** Ngồi ở đầu làng (Menu chính), người hướng dẫn tân thủ và kể chuyện.

- ## **Ông Đồ Già:** Người bán các thẻ bài kỹ năng (được vẽ dưới dạng các chữ Nho hoặc Bùa chú).

## **PHẦN 4: HÌNH ẢNH & ÂM THANH**

### **4.1. Phong Cách Nghệ Thuật (Art Style)**

     **4.1.1. Tông màu & Chất liệu chủ đạo (Material & Palette):**

- **Chất liệu:** Lấy cảm hứng từ các vật liệu truyền thống Việt Nam:  
  * **Giấy Dó & Mực Tàu:** Dùng cho map nền, hiệu ứng sương mù và giao diện kể chuyện.  
  * **Sơn Mài (Lacquer):** Dùng cho các đơn vị lính (Unit) và các công trình kiến trúc, tạo độ bóng, sâu và sang trọng.  
  * **Gỗ Mộc & Gốm Men Rạn:** Dùng cho các vật thể môi trường (chướng ngại vật, thành trì).  
- **Bảng màu:**  
  * **Tông chính:** Màu Cánh Gián (Nâu bóng), Đỏ Son (Vermilion), Vàng Quỳ (Gold Leaf), Đen Then.  
  * **Tông phụ (Phe Thiên Cơ):** Trắng ngà, Vàng hoàng thổ, Xanh ngọc bích.  
  * **Tông phụ (Phe U Linh):** Tím than, Xanh chàm (Indigo), Xám tro.  
  * **Điểm nhấn:** Hiệu ứng phát sáng từ các lá bùa hoặc trận pháp (màu lục hoặc đỏ rực).

     **4.1.2. Thiết kế Nhân vật (Character Design):**

- **Phong cách:** Stylized 3D nhưng bề mặt (texture) được vẽ tay mô phỏng nét cọ tranh Hàng Trống hoặc điêu khắc gỗ đình làng (tỉ lệ cơ thể hơi cường điệu, nét mặt biểu cảm).  
- **Trang phục:**  
  * Áo Giao Lĩnh, Viên Lĩnh, khăn đóng, mũ cánh chuồn (cách điệu).  
  * Giáp trụ lấy cảm hứng từ thời Trần – Lê (vân vảy cá, hộ tâm phiến).  
  * Các linh thú/quái vật mang đặc điểm của Tứ Linh (Long, Lân, Quy, Phụng) hoặc các con vật dân gian (Trâu, Gà, Cóc, Cá Chép).

     **4.1.3. Thiết kế Môi trường (Environment):**

- **Bối cảnh:** Không gian huyền ảo, trôi nổi giữa hư không (như trong một giấc mơ).  
  * Map 1: **Đầm Sen Tàn** – Mặt nước đen loang lổ mực, lá sen khổng lồ làm ô di chuyển.  
  * Map 2: **Cổ Loa Thành** – Các vòng thành ốc xoắn, nỏ thần làm tháp canh.  
  * Map 3: **Làng Tranh** – Nhà cửa là các bức tranh giấy dựng đứng, cây cối là nét vẽ mực tàu.  
- **Hiệu ứng môi trường:**  
  * Lá tre rụng, cánh hoa sen bay, đom đóm lập lòe.  
  * Sương khói mờ ảo (như khói hương trầm).

    ** 4.1.4. Hiệu ứng Chiến đấu (VFX):**

- **Tấn công:** Thay vì tia laser hay lửa thông thường, hiệu ứng sẽ là:  
  * Vệt mực tạt mạnh (Ink splash).  
  * Các ký tự Hán/Nôm bay ra khi thi triển bùa chú.  
  * Bụi vàng (Gold dust) khi unit bị tiêu diệt hoặc spawn.  
- **Triệu hồi (Spawn):** Unit xuất hiện từ một tờ giấy cháy thành tro, hoặc từ một con rối gỗ được giật dây thả xuống.

     **4.1.5. Nguồn Cảm Hứng (Reference):**

- *Okami* (Phong cách mực tàu), *Black Myth: Wukong* (Chi tiết điêu khắc/kiến trúc), *Tranh Đông Hồ/Hàng Trống*.  
  ---

### **4.2. Giao Diện & Trải Nghiệm Người Dùng (UI/UX)**

    ** 4.2.1. Giao diện (UI):**

- **Phong cách tổng thể:**  
  * Giao diện mang phong cách **Cung Đình & Tín Ngưỡng**.  
  * Khung viền sử dụng họa tiết **Khảm Trai (Mother of Pearl)** trên nền gỗ tối màu, hoặc họa tiết **Hoa dây thời Lý**.  
  * Font chữ: Việt hóa mang phong cách Thư Pháp (Calligraphy) hoặc chữ có chân cổ điển, dễ đọc.  
- **Bố cục HUD (Head-Up Display):**  
  * **Thanh MP:** Cách điệu thành một **Thanh Hương (Nhang)** đang cháy dở hoặc một **Bình Mực** vơi dần.  
  * **Xúc xắc:** Hình dáng khối gỗ vuông, các mặt khắc chấm đỏ/đen theo kiểu Tài Xỉu hoặc ký tự Ngũ Hành (Kim, Mộc, Thủy, Hỏa, Thổ).  
  * **Turn Timer:** Hình ảnh **Đồng hồ mặt trời** hoặc **Vòng luân hồi** quay chậm.  
  * **Thẻ bài (Card):** Thiết kế như các lá **Bùa Chú (Taoist Talisman)** hoặc **Thẻ Tre (Bamboo Scroll)**.  
      
- **Menu chính:**  
  * Một bàn trà cũ kỹ, nơi có cuốn sách cổ (Grimoire) mở ra các chế độ chơi.  
  * Mỗi mục (Play, Shop, Deck) là một vật phẩm trên bàn: Cái ấn triện (Play), Hộp sơn mài (Shop), Bát hương (Deck).

     **4.2.2. Trải nghiệm (UX):**

- **Tương tác:**  
  * Khi bấm nút "Play/Confirm": Hiệu ứng đóng dấu **Ấn Triện** (Mộc đỏ) lên màn hình.  
  * Chuyển cảnh: Hiệu ứng cuộn tranh (Scroll) hoặc khói hương lan tỏa che phủ màn hình.  
  * Thông báo thắng/thua: Dòng chữ thư pháp "ĐẠI THẮNG" hoặc "BẠI TRẬN" hiện lên dứt khoát trên nền giấy dó nhàu nát.

  ---

### **4.3. Âm Nhạc (Music)**

- **Phong cách:** **Epic Folk (Dân gian hùng tráng)** pha trộn **Mystical Ambient**.  
- **Nhạc cụ chủ đạo:**  
  * **Đàn Tranh & Đàn Nguyệt:** Tạo giai điệu chính, lúc thánh thót (phe Thiên Cơ), lúc nỉ non, ma mị (phe U Linh).  
  * **Sáo Trúc/Tiêu:** Tạo không gian rộng lớn, cô liêu.  
  * **Bộ gõ:** Trống Cơm, Trống Cái (tạo nhịp dồn dập khi vào battle), Phách gỗ (giữ nhịp turn).  
- **Mood:**  
  * **Menu:** Tiếng sáo nhẹ nhàng, tiếng chuông gió leng keng, tiếng dế kêu đêm (tĩnh lặng, bí ẩn).  
  * **Trong trận (Battle):** Nhịp trống trận dồn dập kết hợp nhạc điện tử trầm (bass) để giữ độ căng thẳng nhưng không mất chất cổ trang.

  ---

### **4.4. Hiệu Ứng Âm Thanh (Sound Effects \- SFX)**

Ưu tiên các âm thanh **"Mộc" (Organic)** thay vì âm thanh điện tử/kim loại:

- **Giao diện:**  
  * Click nút: Tiếng gõ mõ (Hollow wood sound) hoặc tiếng đá lách cách.  
  * Mở menu: Tiếng giấy sột soạt, tiếng mài mực.  
  * Đóng dấu/Chọn unit: Tiếng "Cộp" chắc nịch của gỗ đập xuống giấy.  
- **Chiến đấu:**  
  * **Tung xúc xắc:** Tiếng gỗ lăn lốc cốc trên mặt bàn gỗ/bát sứ.  
  * **Di chuyển:** Tiếng bước chân trên sàn gỗ, tiếng nước bì bõm (nếu đi dưới nước), tiếng lá khô vỡ vụn.  
  * **Tấn công:** Tiếng vút của roi mây, tiếng xé gió của đao kiếm (nhưng trầm hơn), tiếng nổ "bùm" của pháo đất.  
  * **Unit chết:** Tiếng gốm vỡ tan tành, tiếng tượng đá đổ sập, hoặc tiếng giấy bị xé rách.  
  * **Chiếm cứ điểm:** Tiếng chuông chùa ngân vang hoặc tiếng tù và báo hiệu.  
- 

## 

## **PHẦN 5: KỸ THUẬT**

### **5.1. Engine & Công Nghệ**

- Unity Engine

### **5.2. Yêu Cầu Kỹ Thuật**

- Cấu hình tối thiểu & cấu hình khuyến nghị : *\<Bổ sung sau\>*

## 

## **PHẦN 6: KẾ HOẠCH KINH DOANH**

### **6.1. Mô Hình Kinh Doanh (Business Model)**

- ### Free to play (kèm IAP) hoặc trả phí 1 lần

### **6.2. Kế Hoạch Kiếm Tiền Chi Tiết (Monetization Strategy)**

- ### Vật phẩm trang trí, battle pass, skin unit, special skill card, …

### **6.3. Phân Tích Đối Thủ Cạnh Tranh**

- Slay the Spire  
- Clash Mini  
- Into the Breach