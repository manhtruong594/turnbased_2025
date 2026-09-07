# Quy trình tạo battle map

## Kiến trúc runtime

Game chỉ dùng một battle scene, hiện được cấu hình bằng `_battleSceneName` trên `PrepareBattleScreen`.
Mỗi map là dữ liệu gồm:

- Một `MapSettings` riêng.
- Một `BattleMapDefinitionSO` chứa spawn point, capture point và tùy chọn environment prefab.
- Một entry trong `Assets/Resources/BattleMapCatalog.asset`.

Khi người chơi xác nhận trận, `BattleLaunchContext` giữ map đã chọn. Battle scene được load một lần;
`MapManager` dùng `RuntimeMapBuilder` để dựng tile visual, spawn point và capture point trước khi
`GameMediator` khởi tạo gameplay.

Nếu vào battle scene trực tiếp trong Editor hoặc catalog chưa tồn tại, `MapManager.Map` và các point
có sẵn trong scene tiếp tục được dùng làm fallback.

### Test nhanh trực tiếp trong battle scene

1. Mở `HUDScene` và chọn GameObject `Map Manager`.
2. Kéo `BattleMapDefinitionSO` cần test vào `Editor Test Map`.
3. Play trực tiếp scene.

`Editor Test Map` chỉ được dùng trong Unity Editor khi `BattleLaunchContext` chưa có map được chọn.
Map được chọn từ màn hình chuẩn bị trận luôn được ưu tiên. Để trống field này nếu muốn dùng map
fallback có sẵn trong scene.

## Tạo map

Mở `Tools > Turn Based > Battle Map Creator`.

`MapSettings nguồn` cần có:

- `GridType.Square`.
- Một preset nền thỏa `MapRules.IsMovable`.
- Tùy chọn một preset chướng ngại không thỏa `MapRules.IsMovable`.
- Prefab tile trong từng preset cần dùng.

Các bước:

1. Chọn `MapSettings nguồn`.
2. Đặt thư mục đầu ra, tên, số lượng và kích thước lẻ.
3. Chọn số spawn, capture point, seed và mật độ chướng ngại.
4. Chọn preset nền và preset chướng ngại.
5. Nhấn `Tạo map`.

Công cụ có thể tạo tối đa 30 map mỗi lần. Mỗi map được lưu trong thư mục riêng:

```text
Assets/Maps/BattleMap_01/
├── BattleMap_01_Map.asset
└── BattleMap_01_Definition.asset
```

Catalog được tạo tự động tại `Assets/Resources/BattleMapCatalog.asset`. Công cụ không tạo hoặc copy
scene map và không thay đổi Build Settings.

## Chỉnh tay và kiểm tra

Có thể chỉnh tile bằng `Tools > Red Bjorn > Editors > Map`. Sau khi sửa tile, chạy `Areas Mark`.
Không cần chạy `Place Prefabs`, vì prefab được dựng lúc runtime.

Khi sửa `BattleMapDefinitionSO`, bảo đảm:

- Cả `Player1` và `Player2` có spawn point.
- Có ít nhất một capture point.
- Mọi gameplay point nằm trên tile có thể di chuyển.
- Spawn point và capture point không trùng tile.

`Tools > Turn Based > Validate Active Map` chỉ kiểm tra map fallback đang đặt trong battle scene.

## Giới hạn hiện tại

- Tile được instantiate đồng bộ khi vào trận; cần đo lại nếu map có hàng nghìn tile.
- Spawn point và capture point runtime dùng visual mặc định của component.
- Camera chưa tự căn theo kích thước map.
