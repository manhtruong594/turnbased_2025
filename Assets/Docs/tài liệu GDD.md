# **TÀI LIỆU THIẾT KẾ GAME (GAME DESIGN DOCUMENT)**

## **TÊN GAME: \[Điền tên dự án game của bạn\]**

* **Phiên bản:** 0.1  
* **Ngày cập nhật cuối:** 

## 

## **PHẦN 1: TẦM NHÌN TỔNG QUAN**

### **1.1. Ý Tưởng Cốt Lõi (Elevator Pitch)**

* *Mô tả game của bạn trong 1-3 câu ngắn gọn.*  
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

- Hệ thống **xúc xắc kết hợp quyết định chiến thuật** (spawn unit, dùng skill, hay giữ điểm MP) \=\> kế hợp giữa chiến thuật \+  xác suất \+ rủi ro có chủ đích  
- PvP chiến thuật tập trung vào chiếm cứ điểm \+ giữ vị thế, không chỉ tiêu diệt địch  
- Cơ chế thời gian turn theo số lượng unit, đây là một cơ chế **tự cân bằng (auto balance)** độc đáo, khi sức mạnh đi kèm với áp lực tư duy  
- Sự kết hợp giữa **deck-building** và **điều khiển unit** đem đến chiều sâu chiến thuật

## 

## **PHẦN 2: LỐI CHƠI & CƠ CHẾ**

### **2.1. Vòng Lặp Gameplay Cốt Lõi (Core Gameplay Loop)**

- **Chiến đấu:** Người chơi bắt đầu trận đấu 1 vs 1 với máy hoặc với người chơi khác   
- Khi đến turn của mình, người chơi tung xúc xắc/ spawn và điều khiển quái/ sử dụng kỹ năng đặc biệt,...  
- Hạ gục người chơi đối phương bằng cách sử dụng quái để chiếm hết cứ điểm trên map  
- **Phát triển:** người chơi nhận tiền và kinh nghiệm sau mỗi trận đấu, sau khi lên cấp sẽ mở khóa thêm kỹ năng (hoặc đơn vị lính) cao cấp hơn

### 

### **2.2. Mục Tiêu & Điều Kiện Thắng/Thua**

- **Mục tiêu của người chơi:** Đưa người chơi vào một đấu trường căng thẳng, giúp người chơi phát huy tối đa khả năng sáng tạo và chiến thuật của mình, tăng tính khó đoán và may rủi bằng xúc xắc.  
- **Điều kiện thắng:** Người chơi chiếm được hết các cứ điểm trên map  
- **Điều kiện thua:** Ngược lại với thắng

### **2.3. Cơ Chế Chi Tiết (Detailed Mechanics)**

     **2.3.1 Hành Động Của Người Chơi:** 

- Khi bắt đầu lượt, nếu là lượt đầu, người chơi sẽ tung 2 xúc xắc cùng 1 lúc, số điểm tổng của xúc xắc sẽ được cộng vào thanh điểm MP (tối đa 20MP). Điểm MP sẽ dùng để spawn unit, hoặc sử dụng một số kỹ năng đặc biệt.   
- Bắt đầu từ lượt thứ 2 trở đi, người chơi được quyền lựa chọn 1 trong 4 hành động: tung 2 xúc xắc để cộng nhiều điểm hơn, tung 1 xúc xắc và spawn unit/ tung 1 xúc xắc và sử dụng skill đặc biệt/ ko tung xúc xắc để sử dụng cả 2 hành động là spawn unit và skill.  
- Tại mỗi lượt, người chơi đều có thể điều khiển tất cả unit trên màn chơi của mình (mỗi unit sẽ có phạm vi di chuyển và 1 số hành động đặc biệt )

     **2.3.3. Hệ Thống Chiến Đấu:**

- Môi trường chiến đấu: grid map có chướng ngại vật, thành trì của 2  người chơi sẽ đối diện nhau, cứ điểm sẽ được đặt ngẫu nhiên trên map (\~4 cứ điểm) .  
- Hệ thống unit: mỗi unit sẽ có phạm vi di chuyển nhất định, có damage, hp và có thể có thêm 1 kỹ năng phụ (ví dụ tàng hình, đặt bẫy, …).  
- Hệ thống skill: (skill sẽ được thể hiện bằng thẻ bài) \- nghiên cứu sau.  
- Hệ thống turn based: mỗi turn của người chơi sẽ được giới hạn thời gian( giây)  \= số unit mà người chơi đang có trên map \* 10 \+ bonus 20s (cho việc sử dụng skill và spawn unit hoặc tính toán chiến thuật).  
- Hệ thống cứ điểm: khi bất kì unit nào đặt chân lên cứ điểm, cứ điểm đó sẽ được đánh dấu là bị chiếm (bởi người chơi sở hữu unit đó), khi tất cả cứ điểm bị chiếm bởi 1 người chơi duy nhất thì game sẽ kết thúc.

      **2.3.4. Hệ Thống Kinh Tế:**

- Người chơi sẽ nhận được gold và điểm exp sau mỗi trận đấu  
- Exp dùng để lên cấp, mở khóa unit mới và skill card mới  
- Gold dùng để mua vật phẩm trong shop  
- Hệ thống vật phẩm: skill card, card border, khung avatar, skin cho unit, rương, …

 

### **2.4. Các Chế Độ Chơi (Game Modes)**

- Chế độ chơi đơn với bot, và chơi multiplayer

## 

## **PHẦN 3: CỐT TRUYỆN, THẾ GIỚI & NHÂN VẬT (CHAT GPT)**

### **3.1. Tóm Tắt Cốt Truyện (Story Synopsis)**

- Trong thế giới ***Shardrealm*** – nơi mọi sinh vật tồn tại đều được sinh ra từ năng lượng ma thuật của *Mảnh Vỡ Thực Tại (**Reality Shards**)*, hai thế lực cổ đại – ***Ordain*** (Trật Tự) và ***Abyss*** (Hỗn Mang) – không ngừng tranh giành quyền kiểm soát các Mảnh Vỡ này để định hình lại thế giới theo ý mình.  
- Người chơi vào vai *một Triệu Hồi Sư* (***Summoner***), kẻ sở hữu năng lực hiếm hoi có thể điều khiển các Shard để gọi ra sinh vật từ các chiều không gian khác. Trong mỗi trận chiến, hai Triệu Hồi Sư bước vào *đấu trường **Shard Nexus*** – nơi các Mảnh Vỡ hội tụ, tạo nên những bản đồ luôn thay đổi. Mỗi chiến thắng giúp người chơi chiếm giữ Mảnh Vỡ mới, từng bước mở khóa bí mật về nguồn gốc thật sự của thế giới này – và chính bản thân họ.


### **3.2. Bối Cảnh & Xây Dựng Thế Giới (Setting & World Building)**

### **Lịch Sử Thế Giới**

- **Thời Kỳ Hỗn Nguyên**: Thế giới Shardrealm hình thành từ vô số chiều không gian đổ sập vào nhau sau “Đại Sụp Đổ”. Các Mảnh Vỡ Thực Tại trôi nổi, mang trong mình ký ức, sinh vật và quy luật riêng.  
- **Thời Kỳ Triệu Hồi**: Những cá nhân đầu tiên khám phá khả năng kết nối và điều khiển các Mảnh Vỡ được gọi là “Triệu Hồi Sư”. Họ tạo ra các đấu trường để thử nghiệm sức mạnh – và rồi biến nó thành nghi lễ chiến tranh.  
- **Hiện Tại**: Cả thế giới bị chia cắt thành nhiều vùng lãnh địa Shard, mỗi vùng bị chi phối bởi một phe phái hoặc Triệu Hồi Sư quyền lực. Người chơi là một tân binh tham gia cuộc chiến nhằm giành quyền tái tạo lại “Thực Tại Hoàn Mỹ”.

  ### **Địa Lý & Môi Trường**

- **Nexus Field**: Đấu trường trung tâm, nơi mọi Shard giao thoa, địa hình thay đổi mỗi trận đấu.  
- **Citadel of Ordain**: Pháo đài ánh sáng – nơi bảo vệ trật tự, công nghệ và ma thuật nguyên sơ.  
- **Abyss Rift**: Vực thẳm vô tận, nơi sinh ra sinh vật hỗn loạn và quái thú đột biến.  
- **Neutral Shards**: Các khu vực trung lập chứa tài nguyên hiếm – phần lớn là chiến trường khốc liệt nhất.

  ### **Phe Phái & Văn Hóa**

- **Ordain Order** – tin vào kiểm soát, kỷ luật và trật tự ma thuật.  
- **Abyssal Legion** – tin rằng sức mạnh chỉ sinh ra trong hỗn mang và tiến hóa tự do.  
- **The Seekers** – nhóm trung lập, truy tìm “Chân Lý Thực Tại” bằng cách hấp thụ tất cả Mảnh Vỡ.

Mỗi phe có biểu tượng, phong cách triệu hồi và kỹ năng riêng – ảnh hưởng trực tiếp đến **bộ bài kỹ năng** và **unit** mà người chơi có thể sở hữu.

### **3.3. Nhân Vật (Characters)**

###       **3.3.1 Nhân Vật Chính – “The Summoner”**

- **Tên:** Tuỳ người chơi đặt (mặc định: Aris/Kael).  
- **Tiểu sử:** Một học giả trẻ từng nghiên cứu năng lượng Shard tại học viện trung tâm. Sau một thí nghiệm thất bại, nhân cách của họ bị tách làm đôi – nửa còn lại trở thành đối thủ của chính họ trong thế giới Shardrealm.  
- **Mục tiêu:** Thu thập đủ Mảnh Vỡ để hợp nhất thế giới và tìm lại “bản ngã hoàn chỉnh”.

###      **3.3.3. Nhân Vật Phụ Quan Trọng**

- **Lyra – Guardian of Order:** Một chiến binh cổ đại canh giữ Shard của Trật Tự, hướng dẫn người chơi trong giai đoạn đầu.  
- **Ryn – Abyssal Broker:** Thương nhân nửa quái nửa người, cung cấp vật phẩm hiếm và thông tin bí ẩn.  
- **Eidolon – The Voice in the Shard:** Một thực thể không có hình dạng, xuất hiện trong giấc mơ của người chơi, dần tiết lộ bí mật về nguồn gốc thật.

## 

## **PHẦN 4: HÌNH ẢNH & ÂM THANH**

### **4.1. Phong Cách Nghệ Thuật (Art Style)**

* *Mô tả phong cách đồ họa tổng thể. Chèn hình ảnh tham khảo nếu có thể.*  
  * \[Ví dụ: Realistic, Cartoon, Pixel Art, Anime, Low-poly...\]  
  * **Bảng màu chủ đạo:** \[Mô tả các màu sắc chính...\]  
  * **Nguồn cảm hứng:** \[Các game, phim ảnh, tác phẩm nghệ thuật truyền cảm hứng...\]

### **4.2. Giao Diện & Trải Nghiệm Người Dùng (UI/UX)**

* **4.2.1. Giao diện (UI):**  
  * \[Mô tả cảm quan về HUD, menu, các biểu tượng. Phác thảo wireframe nếu có.\]  
* **4.2.2. Trải nghiệm (UX):**  
  * \[Mô tả luồng tương tác của người chơi, sự tiện lợi, dễ sử dụng.\]

### **4.3. Âm Nhạc & Nhạc Nền (Music & Score)**

* *Mô tả thể loại và cảm xúc âm nhạc cho các bối cảnh khác nhau.*  
  * \[Ví dụ: Nhạc menu (huyền bí, mời gọi), Nhạc chiến đấu (dồn dập, hùng tráng)...\]

### **4.4. Hiệu Ứng Âm Thanh (Sound Effects \- SFX)**

* *Liệt kê các loại âm thanh quan trọng.*  
  * \[Ví dụ: Âm thanh vũ khí, tiếng bước chân, âm thanh tương tác UI, tiếng môi trường...\]

## 

## **PHẦN 5: KỸ THUẬT**

### **5.1. Engine & Công Nghệ**

- Unity Engine

### **5.2. Yêu Cầu Kỹ Thuật**

* *Cấu hình tối thiểu và đề nghị (đối với PC) hoặc phiên bản HĐH (đối với mobile).*  
  * **Cấu hình tối thiểu:** \[Điền vào đây...\]  
  * **Cấu hình đề nghị:** \[Điền vào đây...\]

## 

## **PHẦN 6: KẾ HOẠCH KINH DOANH**

### **6.1. Mô Hình Kinh Doanh (Business Model)**

- ### Free to play hoặc trả phí 1 lần

### **6.2. Kế Hoạch Kiếm Tiền Chi Tiết (Monetization Strategy)**

- ### Vật phẩm trang trí, battle pass, skin unit, special skill card, …

### **6.3. Phân Tích Đối Thủ Cạnh Tranh**

- Slay the Spire  
- Clash Mini  
- Into the Breach