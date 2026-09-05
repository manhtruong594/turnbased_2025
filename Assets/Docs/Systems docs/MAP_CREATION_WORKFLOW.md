# Quy trình tạo battle map

## Công cụ

Mở `Tools > Turn Based > Battle Map Creator`.

Công cụ tạo mỗi map dưới dạng một thư mục riêng gồm:

- Một bản sao của scene mẫu.
- Một `MapSettings` riêng, không dùng chung dữ liệu tile với map khác.
- Layout `GridType.Square` đối xứng theo trục X.
- Spawn point cân bằng cho `Player1` và `Player2`.
- Capture point trên trục giữa.
- Tile prefab được dựng lại trong một `MapView` duy nhất.

Có thể tạo tối đa 30 map trong một lần. Khi tạo nhiều map, tên có hậu tố `_01`, `_02`, ... và seed tăng dần từ `Seed đầu tiên`.

## Scene mẫu

Scene mẫu cần có đúng một `MapManager`, một `UnitSpawner`, một `CapturePointManager` và ít nhất một `MapView`. Giữ camera, ánh sáng, manager, UI và các container dùng chung trong scene mẫu.

`MapSettings nguồn` cần có:

- `GridType.Square`.
- Một preset nền thỏa `MapRules.IsMovable`.
- Tùy chọn một preset chướng ngại không thỏa `MapRules.IsMovable`.
- Prefab tile đã gán trong từng preset cần dùng.

Công cụ chỉ thay đổi bản sao được tạo. Scene mẫu và `MapSettings nguồn` không bị sửa.

## Tạo map

1. Chọn scene mẫu và `MapSettings nguồn`.
2. Đặt thư mục đầu ra, tên, số lượng và kích thước lẻ.
3. Chọn số spawn, capture point, seed và mật độ chướng ngại.
4. Chọn preset nền và preset chướng ngại.
5. Nhấn `Tạo map`.

Các hàng chứa spawn/capture point và hành lang giữa luôn được giữ trống. Chướng ngại được sinh theo cặp đối xứng. Công cụ chạy `MapUtils.MarkAreas`, dựng prefab tile, gán `MapManager.Map`, cập nhật danh sách point của manager và kiểm tra kết nối trước khi hoàn tất.

## Chỉnh tay và kiểm tra

Sau khi tạo, có thể chỉnh `MapSettings` bằng `Tools > Red Bjorn > Editors > Map`. Khi thay đổi tile bằng Map Editor, chạy `Areas Mark` rồi `Place Prefabs`.

Chạy `Tools > Turn Based > Validate Active Map` trước khi dùng map. Validation kiểm tra:

- Đúng một `MapManager` và một `MapView` đang active.
- `MapSettings` có tile.
- Cả hai người chơi có spawn point.
- Có capture point.
- Mọi gameplay point nằm trên tile có thể di chuyển và không trùng nhau.
- Tất cả spawn/capture point thuộc cùng một vùng có thể đi tới.

Không thêm scene mới vào Build Settings tự động. Chỉ thêm những map đã duyệt vào luồng chọn map/build của game.
