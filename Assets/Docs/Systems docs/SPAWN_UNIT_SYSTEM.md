# Spawn Unit System

## 1. Mục tiêu

Cho phép một phe dùng MP để tạo unit tại spawn point hợp lệ, đăng ký unit với map và cập nhật UI/event
mà không tạo state trùng.

## 2. Thành phần

| Thành phần | Trách nhiệm |
|---|---|
| `UnitData` | Tên, mô tả, HP, damage, spawn cost, move range/speed và icon |
| `UnitController` prefab | Template được tạo, sau đó nhận owner và grid position runtime |
| `SpawnPoint` | Owner, grid position, occupied/available và visual state |
| `UnitSpawner` | Validation, MP transaction, instantiate và danh sách unit theo phe |
| `SpawnPanel` | Tạo danh sách lựa chọn unit từ selected deck |
| `SpawnUnitButton` | Hiển thị data, interactable state và gửi yêu cầu spawn |
| `MPManager` | Kiểm tra/trừ MP và phát thay đổi |
| `MapManager` | Đăng ký unit tại grid position |
| `GameMediator` | Phát `NotifySpawnUnit` và MP-related event |

## 3. Luồng spawn

```text
Chọn unit trong SpawnPanel
    └── SpawnUnitButton gửi yêu cầu
            └── UnitSpawner kiểm tra
                    ├── sai lượt / thiếu MP / không có spawn point → thất bại
                    └── hợp lệ
                          ├── tạo unit
                          ├── Init(owner, grid position)
                          ├── đánh dấu spawn point occupied
                          ├── đăng ký với map
                          ├── trừ MP
                          └── phát event + cập nhật UI
```

Transaction phải có tính nguyên tử ở mức gameplay: nếu không tạo/khởi tạo/đăng ký được unit thì không
được để MP đã trừ hoặc spawn point bị chiếm dở.

## 4. API chính

### `UnitSpawner`

- `SpawnUnit(UnitController unit, PlayerID owner, SpawnPoint spawnPoint = null)` trả `bool`.
- `GetAvailableSpawnPoint(PlayerID player)` lấy một điểm trống.
- `GetAvailableSpawnPoints(PlayerID player)` lấy toàn bộ điểm trống của phe.
- `GetPlayerUnits(PlayerID player)` lấy unit runtime của phe.

### `SpawnPoint`

- `Initialize(PlayerID playerOwner, Vector3Int gridPos)`.
- `MarkAsOccupied()` và `MarkAsAvailable()`.
- `BelongsTo(PlayerID player)`.
- `SetVisible(bool visible)`.

## 5. Quy tắc

- Chỉ spawn trong lượt của phe sở hữu nếu UI/controller áp dụng luật turn.
- Unit phải tồn tại, có `UnitData` và prefab reference hợp lệ.
- Phe phải đủ `spawnCost`.
- Spawn point phải đúng owner và chưa occupied.
- Tile đích không được có unit khác trong `MapManager`.
- Owner, runtime stats và grid position phải được gán trước khi unit nhận action.
- MP/UI/event chỉ cập nhật sau khi spawn thành công.

## 6. Trạng thái

| Hạng mục | Trạng thái |
|---|---|
| Unit data và prefab-based spawn | Hiện có |
| Spawn point theo phe | Hiện có |
| MP validation/payment | Hiện có |
| Map registration | Hiện có |
| Spawn panel/button | Hiện có |
| Button phản ứng theo MP | Hiện có, cần kiểm chứng |
| Tooltip và failure reason | Một phần |
| Spawn animation/SFX/VFX | Một phần |
| Automated spawn tests | Chưa có |

## 7. Trường hợp lỗi cần xử lý

- Không có `UnitSpawner`, `MPManager`, `MapManager` hoặc mediator.
- Deck chứa null/missing prefab.
- Không có spawn point đúng phe hoặc tất cả đã occupied.
- Spawn point báo trống nhưng tile map đã có unit.
- Unit bị hủy ngay sau instantiate.
- UI click lặp trong cùng frame hoặc khi transition turn.
- AI và player cùng yêu cầu spawn do owner/turn setup sai.

## 8. Kiểm chứng

1. Đủ MP và có spawn point: tạo đúng prefab, owner, position; MP giảm đúng cost.
2. Thiếu MP: không tạo object, không đổi spawn point/map và có feedback.
3. Hết spawn point: không trừ MP.
4. Tile đã có unit: spawn thất bại an toàn.
5. Sau unit death/removal, luật có cho tái sử dụng spawn point hay không phải hoạt động đúng thiết kế.
6. Button cập nhật sau MP change, turn change và spawn thành công.
7. Event spawn chỉ phát một lần cho mỗi unit thành công.

## 9. Việc còn lại

1. Chốt và hiển thị failure reason thay cho log-only.
2. Thêm feedback success/failure, tooltip và audio/visual còn thiếu.
3. Bảo vệ thao tác click lặp và transaction dở.
4. Thêm EditMode test cho validation và PlayMode test cho map/scene integration.
