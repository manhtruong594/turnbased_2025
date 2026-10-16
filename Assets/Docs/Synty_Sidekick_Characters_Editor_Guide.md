# Hướng Dẫn Sử Dụng Editor Tool - Synty Sidekick Characters

Tài liệu tóm tắt cấu trúc và cách sử dụng bộ công cụ Editor của package **Synty Sidekick Characters** (dựa trên source code tại `Assets/Synty/SidekickCharacters/Scripts`).

---

## 1. Tổng quan các công cụ Editor

Package cung cấp 3 công cụ chính trong Unity Editor:
1. **Sidekick Character Tool (`ModularCharacterWindow.cs`)**: Cửa sổ chính để thiết kế, xem trước, chỉnh sửa bộ phận, vóc dáng, màu sắc và xuất nhân vật thành FBX/Prefab.
2. **Sidekick Tool Downloader (`ToolDownloader.cs`)**: Cửa sổ kiểm tra và tải cập nhật phiên bản tool và database mới nhất từ GitHub.
3. **Character Blendshape Inspector (`CharacterBlendshapeEditor.cs`)**: Component gắn trên nhân vật để xem và tinh chỉnh vóc dáng/blendshape và các socket attachment trực tiếp trong Inspector.

Ngoài ra còn có:
- **`SkAssetOpenHandler.cs`**: Cho phép double-click file cấu hình nhân vật `.sk` trong Project window để mở thẳng trong Sidekick Character Tool.
- **`DatabaseBuildSync.cs`**: Tự động đồng bộ database SQLite sang `Resources/Database/Side_Kick_Data.bytes` trước mỗi lần build Player (`IPreprocessBuildWithReport`) để nạp runtime.

---

## 2. Cách mở công cụ

- **Sidekick Character Tool**:
  - Menu: `Synty > Sidekick Character Tool`.
  - Hoặc double-click vào bất kỳ file asset `.sk` nào trong cửa sổ Project.
  - Tự động mở khi khởi động Unity nếu bật tùy chọn *Open tool on startup*.
- **Sidekick Tool Downloader**:
  - Menu: `Synty > Sidekick Tool Downloader`.
  - Hỗ trợ tải trực tiếp `Sidekicks.unitypackage` và `SidekicksDatabase.unitypackage`.

---

## 3. Giao diện & Chức năng trong Sidekick Character Tool

Cửa sổ gồm thanh tiêu đề, thanh công cụ chia tab, khung xem trước Scene, và thanh hành động đáy (Bottom Action Bar).

### 3.1. Tab Presets (Tạo nhanh theo mẫu)
- **Select - Species**: Lựa chọn chủng tộc (ví dụ: *Human*, *Alien*, *Elf*, *Orc*, *Unrestricted*). Các danh sách part và màu sắc sẽ được lọc theo chủng tộc này.
- **Select - Preset Part Filter**: Bật/tắt các bộ lọc outfit (ví dụ: Medieval, Sci-Fi, Casual...) để thu hẹp phạm vi preset. Có nút *Select All* / *Select None*.
- **Randomize Character**: Nút bấm ngẫu nhiên toàn diện (parts, body sliders, colors) dựa theo bộ lọc chủng tộc đang chọn.
- **Presets Dropdowns**:
  - **Parts**: Chọn các bộ preset lắp ghép sẵn cho *Head*, *Upper Body*, *Lower Body*.
  - **Body**: Chọn nhanh vóc dáng mẫu (*Androgynous Medium*, *Masculine Heavy*, *Feminine Slim*...).
  - **Colors**: Áp dụng các bảng màu định sẵn theo *Species*, *Outfits*, *Attachments*, *Materials*.

### 3.2. Tab Parts (Tùy biến từng bộ phận)
- **Select - Outfit Filter**: Lọc các part thuộc outfit cụ thể trên toàn bộ thư viện.
- **Phân nhóm bộ phận (Part Groups)**: Gom nhóm theo *Head*, *Torso*, *Arms*, *Legs*, *Attachments*...
  - Mỗi nhóm có nút **Randomize [Group]** để random riêng nhóm đó.
- **Bộ điều khiển từng Part**:
  - **Dropdown**: Danh sách các part tương thích có trong thư viện.
  - **Lock (Ổ khóa)**: Khóa bộ phận lại. Khi bấm Randomize toàn bộ hoặc Randomize nhóm, các part bị khóa sẽ giữ nguyên.
  - **Remove (Gỡ)**: Đặt bộ phận về `None` (không gắn mesh).
  - **Previous / Next (Mũi tên)**: Chuyển nhanh qua lại giữa các part kế tiếp nhau.
  - **Random (Biểu tượng xí ngầu)**: Random riêng lẻ duy nhất bộ phận đó.

### 3.3. Tab Body (Vóc dáng & Blendshape)
Điều chỉnh hình thể nhân vật theo 3 thanh trượt slider (giá trị từ `-100` đến `+100`):
- **Body Type**: Masculine (`-100`) <---> Feminine (`+100`). Khi kéo qua mốc, hệ thống tự động hoán đổi trang phục phù hợp (nếu part hỗ trợ biến thể wrap nam/nữ).
- **Body Size**: Slim (`-100`) <---> Heavy (`+100`).
- **Musculature**: Lean (`-100`) <---> Muscular (`+100`).
- Các socket attachment xương (vũ khí, giáp vai, hông) tự động được tính toán bù trừ vị trí và góc xoay tương ứng khi kéo slider.

### 3.4. Tab Colors (Phối màu & Texture Palette)
- **Filter - Parts**: Lọc danh sách màu hiển thị theo từng vùng (ví dụ: *All Parts*, *Skin*, *Hair*, *Upper Body*, *Lower Body*...) để dễ quản lý.
- Hiển thị từng dòng thuộc tính màu sắc (Color Rows):
  - Cho phép chọn mã màu (Base Color), độ kim loại (Metallic), độ mịn (Smoothness), phát sáng (Emission).
  - Hệ thống tự động ánh xạ UV của các part vào đúng ô màu trong Color Map.

### 3.5. Tab Options (Cấu hình công cụ & Xuất file)
- **Base Assets**:
  - **Model**: Mesh gốc rig khung xương nhân vật (`SK_BaseModel`).
  - **Force Model**: Bật để luôn ép dùng Base Model mặc định; tắt để cho phép tự đổi sang rig đặc biệt nếu part đầu yêu cầu (ví dụ Alien).
  - **Material**: Material mẫu (`M_BaseMaterial`).
  - **Animation Controller**: Gán `AnimatorController` để nhân vật tự động chạy animation preview ngay trong Scene View lúc edit.
- **Update Part Library**: Quét lại toàn bộ asset và database khi vừa import thêm pack mới.
- **Prefab Options**:
  - **Combine Character Meshes**: Gộp tất cả các `SkinnedMeshRenderer` của từng part thành 1 mesh duy nhất (giảm draw call tối đa).
  - **Combine Body Blend Shapes**: Bật để giữ lại các blendshape body trên model xuất ra; tắt để bake chết vóc dáng vào mesh.
  - **Combine Facial Blend Shapes**: Bật để giữ facial blendshapes phục vụ animation nét mặt.
- **Tool Options**:
  - **Show all color properties**: Hiển thị toàn bộ ô màu trong database thay vì chỉ hiển thị các ô màu mà nhân vật hiện tại sử dụng.
  - **Open tool on startup**: Tự mở cửa sổ tool mỗi khi mở Unity.
  - **Remember character**: Tự động lưu/khôi phục nhân vật đang làm dở khi đóng/mở tool hoặc chuyển đổi Play Mode.
  - **Show missing parts warning**: Hiện cảnh báo nếu nạp preset bị thiếu part.
  - **Sync Runtime Database**: Copy `Side_Kick_Data.db` sang `Resources/Database/Side_Kick_Data.bytes` phục vụ truy vấn runtime.
- **Liên kết tài liệu**: Nút mở file PDF hướng dẫn gốc (`SidekickCharacters_UserGuide.pdf`), Synty Store và link hướng dẫn video.

---

## 4. Lưu trữ, Nạp và Xuất Nhân Vật (Bottom Bar)

Nằm ở dưới cùng của cửa sổ Sidekick Character Tool:
1. **Save Character**:
   - Lưu cấu hình nhân vật hiện tại ra file `.sk` (định dạng YAML lưu danh sách part, chỉ số body slider và bảng màu).
2. **Load Character**:
   - Chọn và nạp lại file `.sk` đã lưu trước đó để tiếp tục chỉnh sửa.
3. **Export Character as FBX**:
   - *Yêu cầu*: Cần cài đặt package **FBX Exporter** (`com.unity.formats.fbx`).
   - Mở hộp thoại chọn vị trí lưu. Tool sẽ tự động tạo một thư mục riêng gồm:
     - `Textures/`: Chứa file texture palette `ColorMap.png` (kích thước tối ưu 32x32 Point filter).
     - `Materials/`: Material mới đã gán texture tương ứng.
     - `Meshes/`: File `.fbx` chứa mô hình 3D (kèm Humanoid avatar chuẩn).
     - File `.sk`: Snapshot cấu hình gốc để có thể nạp lại sau này.
     - File `.prefab`: Prefab hoàn chỉnh gồm mô hình, `Animator`, `Avatar` và `Material` sẵn sàng kéo vào gameplay.

---

## 5. Sử Dụng `CharacterBlendshapeEditor` Trên Prefab/Scene

Nếu nhân vật được xuất với tùy chọn giữ blendshape (*Combine Body Blend Shapes* bật):
- Gắn component `CharacterBlendshapeEditor` lên GameObject chứa `SkinnedMeshRenderer`.
- Trong Inspector:
  - Tinh chỉnh trực tiếp 3 thanh trượt: **Body Type** (-1 đến 1), **Body Size** (-1 đến 1), **Musculature** (-1 đến 1).
  - Component sẽ tự tính toán nội suy vị trí/góc xoay các socket attachment (`_abac*`, `_ahpf*`, `_ahpb*`, `_ashl*`, `_ashr*`...) tương ứng với vóc dáng ngay trong Edit Mode.

