# Test map Kenney Town

## Nội dung

- Scene: `Assets/Scenes/SampleScene.unity`.
- Root: `Kenney Town Test Map`.
- Asset: `Assets/MyGame/Maps/KenneyTownTest/`.
- Tham chiếu cấu trúc `BattleMap_01_Map.asset` và `BattleMap_01_Definition.asset`; dùng model từ `kenney_fantasy-town-kit_2.0`.
- Grid Square trên XZ, 21 × 17 ô, `Edge = 2`, mặt di chuyển tại Y = 0.
- 357 ô: 294 ô đi được, 63 ô chặn; toàn bộ vùng đi được liên thông.
- 8 nhà, 4 quầy chợ, đài phun nước, xe đẩy, cây, đá, nền và viền map. Material tương thích URP.
- Player1: spawn tại `(-9, 0, -6)`, `(-9, 0, 6)`; Player2: `(9, 0, -6)`, `(9, 0, 6)`.
- Capture tại `(0, 0, -6)`, `(0, 0, 0)`, `(0, 0, 6)`. Đĩa xanh/đỏ đánh dấu spawn; đĩa vàng đánh dấu capture.

## Chỉnh sửa và sử dụng

`KenneyTownTest_Map.asset` có 4 preset: Grass, Paving, Blocked garden / building, Blocked plaza / market. Hai preset chặn dùng tag và quy tắc di chuyển của map mẫu. Mở asset bằng Red Bjorn Map Editor để chỉnh grid; cập nhật vùng chặn nếu di chuyển nhà hoặc props.

`KenneyTownTest_Definition.asset` tham chiếu map, spawn/capture và `Prefabs/KenneyTown_Environment.prefab`. Đã gán definition này vào `Map Manager > Editor Test Map` trong `Assets/Scenes/HUDScene.unity`, đồng thời cập nhật MapSettings, preview tile/môi trường và reference spawn/capture. Chưa thêm map vào catalog hoặc cấu hình multiplayer.

### Test gameplay trong HUDScene

- Mở trực tiếp `Assets/Scenes/HUDScene.unity`, nhấn Play để dùng map Kenney với hệ thống HUD, turn, spawn, skill/spell và capture hiện có.
- Khi vào từ luồng chọn map, `BattleLaunchContext.SelectedMap` được ưu tiên hơn `Editor Test Map`; cấu hình này dành cho chạy trực tiếp HUDScene trong Editor.
- Camera orthographic size 27, zoom từ 8 đến 40; giữ thao tác kéo chuột trái, WASD/phím mũi tên và cuộn chuột của camera hiện có.
- Preview nằm dưới MapView, được dọn khi runtime dựng map; spawn/capture cũng được tạo lại từ definition. Boat của map cũ được tắt.
- Đã kiểm tra reference manager trong GameMediator, missing script, spawn/capture và liên thông bằng validator của project; đã xem ảnh camera. Chưa chạy Play Mode hoặc xác minh gameplay runtime.
- Bản HUDScene trước thay đổi: `Artifacts/KenneyTownTest/HUDScene.before.unity`; ảnh preview: `Artifacts/KenneyTownTest/HUDScene-preview.png`. Khi cần khôi phục, giữ bản sao chỉnh sửa mới rồi thay nội dung HUDScene bằng bản backup, giữ nguyên `HUDScene.unity.meta`.

`SampleScene` là scene xem và kiểm tra bố cục map, có `MapManager`, `MapView`, `UnitSpawner`, `CapturePointManager` và các điểm gameplay. Scene chưa có toàn bộ battle bootstrap, turn flow, unit và HUD; không phải một trận đấu hoàn chỉnh. `MapManager` được gán definition để runtime dựng lại tile và môi trường. Bản preview môi trường nằm dưới `MapView`, nên được dọn trước khi dựng lại, tránh nhân đôi.

## Kiểm chứng

- Đã compile trong Unity: 0 lỗi; có warning `CS0414` sẵn có ở `GameplayTestTool._mpAmount`.
- Đã kiểm tra bằng `MapEntity`: 294 ô đi được liên thông, mọi spawn/capture hợp lệ và không trùng ô.
- Đã kiểm tra missing script, material và shader được hỗ trợ: 0 lỗi.
- Đã xem ảnh camera và lưu scene. Chưa chạy Play Mode, di chuyển unit, chiếm điểm hoặc kiểm tra cân bằng hai phe.
- Bản scene trước thay đổi: `Artifacts/KenneyTownTest/SampleScene.before.unity`; ảnh: `Artifacts/KenneyTownTest/preview.png`.
- Script dựng map được lưu ngoài Assets tại `Artifacts/KenneyTownTest/KenneyTownTestMapBuilder.cs`; script Editor tạm được gỡ sau khi tạo asset.

Muốn khôi phục scene trước thay đổi: thoát Play Mode, giữ bản sao scene hiện tại nếu cần, rồi thay `SampleScene.unity` bằng bản sao trên và mở lại scene. Giữ nguyên `SampleScene.unity.meta`. Thao tác này cũng loại bỏ mọi chỉnh sửa scene thực hiện sau thời điểm sao lưu.
