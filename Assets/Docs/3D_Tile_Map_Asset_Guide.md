# Tổng hợp: tạo nhanh map tile 3D và nguồn tải asset

Ngày tổng hợp: 2026-10-08.

Tài liệu tổng hợp cuộc trao đổi về cách tạo map 3D đầy đủ, sống động cho The Summoners: Ordain and Abyss, khả năng dùng AI và nguồn tải asset ngoài Unity Asset Store. Đây là hướng dẫn và đề xuất; chưa tải/import asset, sửa hệ thống map hoặc kiểm chứng trong Unity.

## 1. Hướng làm đề xuất

Dùng một bộ môi trường 3D modular làm nền, tạo layout bằng công cụ có sẵn trong project, dùng AI bổ sung asset đặc trưng nếu cần. Hoàn thiện một map mẫu trước khi tạo nhiều map.

- Giữ mặt sân chiến đấu rõ ràng ở giữa; đặt cảnh quan phong phú ở rìa.
- Chọn một chủ đề trước, ví dụ phế tích đền triệu hồi giữa rừng.
- Dùng khoảng 20–30 prefab tái sử dụng làm bộ khởi đầu.
- Thêm ánh sáng, chuyển động nhẹ và âm thanh để tạo sức sống.
- Chọn phong cách môi trường khớp với nhân vật hiện có.

## 2. Hệ thống map hiện có

Nội dung đã được đối chiếu trong cuộc trao đổi với [MAP_CREATION_WORKFLOW.md](<Systems docs/MAP_CREATION_WORKFLOW.md>) và các file:

- `Assets/Scripts/Data/Map/BattleMapDefinitionSO.cs`
- `Assets/Scripts/Manager/RuntimeMapBuilder.cs`
- `Assets/Scripts/Editor/BattleMapCreatorWindow.cs`

| Thành phần | Vai trò |
|---|---|
| `MapSettings` | Grid, loại tile, prefab nền và chướng ngại |
| `BattleMapDefinitionSO` | Spawn point, capture point, preview và environment prefab |
| `EnvironmentPrefab` | Cảnh quan, cây, đá, công trình và hiệu ứng trang trí; runtime đã hỗ trợ instantiate |

Vật cản ảnh hưởng gameplay phải được thể hiện trong `MapSettings`. Chỉ đặt cây hoặc đá trong `EnvironmentPrefab` không tự biến tile thành ô bị chặn.

Generator hiện tạo tile tại `y = 0`. Bản đầu nên giữ mặt gameplay phẳng; địa hình cao thấp ảnh hưởng di chuyển cần thiết kế và kiểm chứng riêng.

## 3. Nguồn tải asset ngoài Unity Asset Store

Thông tin dưới đây được tra cứu từ trang chính thức trong cuộc trao đổi. Nội dung gói, giá và điều kiện tải có thể thay đổi; kiểm tra phiên bản cụ thể trước khi tải hoặc mua.

| Nguồn / bộ asset | Chi phí / giấy phép được công bố | Nội dung và ứng dụng |
|---|---|---|
| [KayKit – Dungeon Remastered](https://kaylousberg.com/game-assets/dungeon-remastered) | Miễn phí, CC0 | Hơn 200 asset stylized: sàn, tường, cầu thang, cửa, rương, cờ và props. Modular, có FBX, GLTF, OBJ. Ưu tiên cho map dungeon hoặc đền cổ theo ô. |
| [Quaternius – Medieval Village MegaKit](https://quaternius.com/packs/medievalvillagemegakit.html) | Có bản miễn phí và trả phí; trang công bố CC0 | Bộ đầy đủ hơn 300 module ghép grid: nhà, tường, mái, sàn, cây leo. Bản Source có thiết lập Unity URP. Phù hợp map làng và cảnh quan fantasy. |
| [Kenney – Fantasy Town Kit](https://kenney.nl/assets/fantasy-town-kit) | Miễn phí, CC0 | 160 file asset 3D, phong cách đơn giản. Phù hợp map mẫu, thị trấn và cảnh quan phụ. |
| [Synty Store](https://syntystore.com/) | Trả phí; kiểm tra license của gói | Mua trực tiếp các bộ POLYGON; có danh mục Unity và Map Expansions. Phù hợp khi cần bộ môi trường lớn, đồng nhất phong cách. |
| [KayKit – danh mục asset](https://kaylousberg.com/game-assets) | Tùy bộ | Có Forest Nature Pack, Dungeon và nhiều bộ props để mở rộng cảnh quan. |

### Chọn nhanh

- **Dungeon/đền cổ:** KayKit Dungeon Remastered.
- **Ngoài trời/làng fantasy:** Quaternius Medieval Village MegaKit.
- **Map mẫu đơn giản, miễn phí:** Kenney Fantasy Town Kit.
- **Đầu tư bộ hình ảnh đồng nhất:** Synty, chọn bộ phù hợp với nhân vật.

Ưu tiên FBX kèm texture hoặc package hỗ trợ Unity URP. Quaternius phân biệt bản miễn phí, bản mở rộng và bản Source; không phải bản nào cũng có toàn bộ asset và cấu hình Unity. Việc công bố hỗ trợ Unity/URP chưa thay thế kiểm tra trên Unity `6000.3.9f1`, URP `17.3.0` của project.

Các bộ này cung cấp hình ảnh 3D; vẫn cần gán prefab vào grid gameplay hiện có.

## 4. Bộ asset khởi đầu

Các số lượng dưới đây là gợi ý, không phải yêu cầu của hệ thống.

| Nhóm | Số lượng đề xuất | Ví dụ |
|---|---:|---|
| Nền | 4–6 | Cỏ, đất, đá lát, đá nứt, rêu |
| Vật cản | 4–6 | Đá lớn, cột gãy, tường đổ, gốc cây |
| Viền map | 3–4 | Mép đất, góc đất, vách đá |
| Trang trí nhỏ | 6–10 | Cỏ, hoa, nấm, đá vụn, bình vỡ |
| Điểm nhấn | 1–2 | Cổng triệu hồi, tượng cổ |
| Hiệu ứng | 3–4 | Bụi sáng, sương nhẹ, lửa, lá bay |

### Chuẩn hóa prefab

- Kích thước khớp khoảng cách ô thực tế của `MapSettings`; không mặc định mỗi ô là 1 mét.
- Pivot thống nhất tại tâm ô, trên mặt tiếp xúc.
- Mặt nền cùng cao độ; cạnh tile ghép kín.
- Material tương thích URP.
- Trang trí không che unit, highlight hoặc cản raycast chọn ô.
- Prefab nền/vật cản gán vào preset của `MapSettings`; cảnh quan trang trí đưa vào `EnvironmentPrefab`.

## 5. Quy trình ráp map

1. Chuẩn bị prefab nền và vật cản, gán vào preset của `MapSettings` nguồn. Công cụ hiện yêu cầu `GridType.Square`.
2. Mở `Tools > Turn Based > Battle Map Creator`.
3. Tạo một map thử: **15 × 11**, **4 spawn mỗi bên**, **3 capture point**, mật độ vật cản khoảng **0.10–0.15**. Đây là cấu hình khởi đầu để thử, chưa phải thông số cân bằng đã xác minh.
4. Chỉnh bố cục bằng `Tools > Red Bjorn > Editors > Map`, rồi chạy `Areas Mark`.
5. Không cần chạy `Place Prefabs`; tile visual được dựng lúc runtime.
6. Dựng cảnh quan thành prefab, gán vào `EnvironmentPrefab` của definition.
7. Để thử trực tiếp: mở `HUDScene`, gán definition vào `Map Manager > Editor Test Map`, rồi kiểm tra trong Play Mode khi chủ động thực hiện bước thử.

`Editor Test Map` chỉ dùng trong Unity Editor khi `BattleLaunchContext` chưa có map được chọn. Map chọn từ màn hình chuẩn bị trận luôn được ưu tiên.

### Giới hạn cần biết

- Generator hiện gán `PrefabIndex = 0` cho mọi tile. Thêm nhiều prefab vào preset chưa làm map tự có biến thể.
- Đề xuất cải tiến sau: chọn biến thể prefab theo seed ngay lúc sinh dữ liệu, rồi bổ sung trang trí theo vùng. Các cải tiến này chưa được triển khai trong cuộc trao đổi.
- Tile được instantiate đồng bộ khi vào trận; cần đo thời gian tải trước khi tăng mạnh số tile.
- Camera chưa tự căn theo kích thước map theo tài liệu workflow hiện có.
- `Tools > Turn Based > Validate Active Map` chỉ kiểm tra map fallback đang đặt trong battle scene, không thay thế kiểm chứng map definition được chọn ở runtime.

## 6. Dùng AI khi cần

| Công việc | Cách dùng đề xuất |
|---|---|
| Chốt phong cách | Tạo concept toàn cảnh với góc camera gần gameplay |
| Tile nền, mép và góc cần ghép chính xác | Dùng bộ modular có sẵn hoặc dựng mesh đơn giản trong Blender |
| Tượng, đá lạ, cổng, công trình đặc trưng | Tạo từng model riêng bằng AI |
| Hoàn thiện model | Chỉnh scale, pivot, mesh, UV, material và collider trước khi làm prefab |

[Meshy Image to 3D](https://www.meshy.ai/features/image-to-3d/) hỗ trợ ảnh → model 3D và xuất FBX/OBJ/GLB. Xem [workflow chính thức](https://docs.meshy.ai/en/webapp/getting-started) về remesh, texture và export.

Nên tạo từng prop riêng. Sinh cả map thành một mesh sẽ khó chỉnh ô, đường đi, collider và tái sử dụng. Ảnh preview đẹp chưa chứng minh model phù hợp với camera và hiệu năng của game.

Prompt mẫu tạo ảnh tham chiếu:

> Một cột đá cổ bị gãy dành cho game chiến thuật fantasy stylized, hình khối rõ khi nhìn từ camera trên cao, đá xám phủ rêu xanh, chi tiết lớn đơn giản, đáy phẳng, một vật thể duy nhất, nền trơn, ánh sáng trung tính, không chữ, không cảnh nền.

## 7. Tạo sức sống cho map

- **Ánh sáng:** một hướng sáng chính rõ; điểm triệu hồi có màu nhấn.
- **Chuyển động:** cỏ/cành rung nhẹ, lửa và bụi sáng tại vài vị trí.
- **Không khí:** sương mỏng ở rìa, âm thanh gió/rừng và âm thanh cục bộ gần cổng.
- **Phản hồi gameplay:** capture point đổi màu hoặc phát sáng theo chủ sở hữu; đây là mục tiêu trình bày cần đối chiếu implementation khi thực hiện.
- **Bố cục:** vật thể cao ở rìa hoặc phía sau theo camera; giữ khu giao tranh thông thoáng.

## 8. Kiểm chứng trước khi nhân rộng

- Spawn và capture point nằm trên tile đi được, không trùng tile.
- Đường đi nối các vùng spawn với capture point.
- Decoration không cản chọn ô hoặc che unit/highlight.
- Không có missing script/reference hoặc material lỗi.
- Camera hiển thị được vùng chiến đấu cần thiết.
- Hiệu năng và thời gian tải chấp nhận được trên thiết bị mục tiêu.

Các bước trên là danh sách cần kiểm tra, chưa được xác nhận hoàn thành. Cuộc trao đổi mới dừng ở tư vấn, đọc tài liệu/code và tra cứu nguồn asset.
