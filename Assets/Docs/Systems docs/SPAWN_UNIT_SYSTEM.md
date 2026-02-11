# Hệ thống Spawn Unit - Sprint 2

## 📋 Tổng quan

Hệ thống spawn unit cho phép người chơi tạo ra các đơn vị chiến đấu bằng cách tiêu tốn MP (Mana Points). Đây là tính năng cốt lõi của Sprint 2, mục 2 theo kế hoạch phát triển.

## ✅ Các tính năng đã hoàn thành

### 1. **UnitData System** (`UnitData.cs`)
- ScriptableObject để lưu trữ cấu hình unit
- Chứa thông tin: tên, mô tả, cost MP, move range, speed, prefab, icon
- Dễ dàng tạo nhiều loại unit khác nhau

### 2. **Unit Component** (`Unit.cs`)
- Component chính gắn vào mỗi unit prefab
- Quản lý thông tin runtime: owner, grid position, selection state
- Hỗ trợ events: OnSelected, OnDeselected
- Visual feedback theo team (màu sắc)

### 3. **SpawnPoint System** (`SpawnPoint.cs`)
- Đánh dấu vị trí spawn hợp lệ trên map
- Phân biệt spawn point của từng người chơi
- Quản lý trạng thái: Available/Occupied
- Visual indicator với màu sắc (green = available, red = occupied)

### 4. **UnitSpawner** (`UnitSpawner.cs`)
- Singleton pattern quản lý toàn bộ việc spawn
- Kiểm tra điều kiện:
  - Đủ MP để spawn
  - Spawn point khả dụng
  - Spawn point thuộc đúng người chơi
- Tự động phân loại spawn points theo người chơi
- Events: OnUnitSpawned, OnUnitDestroyed
- Helper methods: GetPlayerUnits, GetUnitCount, etc.

### 5. **UI System**

#### `SpawnUnitButton.cs`
- Button UI để spawn từng loại unit
- Tự động update state (enabled/disabled) theo MP
- Visual feedback: màu sắc thay đổi khi đủ/không đủ MP
- Tích hợp với MPManager và TurnManager

#### `SpawnPanel.cs`
- Panel chứa danh sách các unit có thể spawn
- Tự động generate buttons từ danh sách UnitData
- Quản lý hiển thị/ẩn panel
- Hỗ trợ thêm/xóa unit động

### 6. **Tích hợp với các hệ thống khác**
- **MPManager**: Kiểm tra và tiêu tốn MP khi spawn
- **TurnManager**: Chỉ cho phép spawn vào lượt của mình
- **UIManager**: Hiển thị log và cập nhật UI

## 🎯 Luồng hoạt động

```
1. Người chơi click Spawn Button
   ↓
2. SpawnUnitButton kiểm tra:
   - Có phải lượt của mình?
   - Đủ MP không?
   ↓
3. Gọi UnitSpawner.SpawnUnit()
   ↓
4. UnitSpawner kiểm tra:
   - Có spawn point available?
   - Spawn point thuộc đúng người chơi?
   ↓
5. MPManager.SpendMP() - Trừ MP
   ↓
6. Instantiate unit prefab
   ↓
7. Initialize Unit component
   ↓
8. Đánh dấu spawn point = occupied
   ↓
9. Trigger OnUnitSpawned event
   ↓
10. UI cập nhật (MP giảm, button state)
```

## 📂 Cấu trúc Code

```
Assets/Scripts/
├── Unit/
│   ├── UnitData.cs          - ScriptableObject cho unit config
│   ├── Unit.cs              - Component cho unit runtime
│   ├── UnitSpawner.cs       - Manager spawn logic
│   └── SpawnPoint.cs        - Đánh dấu vị trí spawn
└── UI/
    ├── SpawnUnitButton.cs   - UI button spawn từng unit
    └── SpawnPanel.cs        - Panel chứa danh sách buttons
```

## 🔧 API chính

### UnitSpawner

```csharp
// Spawn unit
bool SpawnUnit(UnitData unitData, PlayerID owner, SpawnPoint spawnPoint = null)

// Lấy spawn points available
SpawnPoint GetAvailableSpawnPoint(PlayerID player)
List<SpawnPoint> GetAvailableSpawnPoints(PlayerID player)

// Lấy units của người chơi
List<Unit> GetPlayerUnits(PlayerID player)
int GetUnitCount(PlayerID player)

// Hủy unit
void DestroyUnit(Unit unit)
void ClearAllUnits()
```

### Unit

```csharp
// Initialize
void Initialize(UnitData data, PlayerID owner, Vector3Int gridPos)

// Selection
void Select()
void Deselect()

// Queries
bool BelongsTo(PlayerID player)
```

### SpawnPoint

```csharp
// Initialize
void Initialize(PlayerID playerOwner, Vector3Int gridPos)

// State management
void MarkAsOccupied()
void MarkAsAvailable()
bool IsAvailable { get; }

// Queries
bool BelongsTo(PlayerID player)
```

## 🎨 Thiết kế tuân thủ

### SOLID Principles

- **Single Responsibility**: Mỗi class có một nhiệm vụ rõ ràng
  - `UnitData`: Chỉ lưu config
  - `Unit`: Chỉ quản lý runtime state
  - `UnitSpawner`: Chỉ xử lý spawn logic
  - `SpawnPoint`: Chỉ quản lý vị trí spawn

- **Open/Closed**: Dễ mở rộng
  - Thêm unit mới chỉ cần tạo UnitData mới
  - Không cần sửa code hiện tại

- **Dependency Inversion**: Sử dụng interfaces và events
  - Các component giao tiếp qua events
  - Loose coupling giữa các hệ thống

### DRY (Don't Repeat Yourself)

- Logic kiểm tra MP được tập trung ở `MPManager`
- Logic spawn được tập trung ở `UnitSpawner`
- Không duplicate code kiểm tra điều kiện

### KISS (Keep It Simple, Stupid)

- Code ngắn gọn, dễ hiểu
- Methods không quá dài (< 30 dòng)
- Tên biến/hàm rõ ràng, tự giải thích

## 📊 Performance

- **Singleton pattern**: Tránh duplicate managers
- **Event-based**: Chỉ update khi cần thiết
- **Caching**: SpawnPoints được cache và phân loại sẵn
- **Object pooling ready**: Cấu trúc sẵn sàng để implement pooling sau

## 🧪 Testing Checklist

- [x] Spawn unit tiêu tốn đúng MP
- [x] Không thể spawn khi không đủ MP
- [x] Không thể spawn khi hết spawn point
- [x] Spawn point đổi màu khi occupied
- [x] Button disabled khi không đủ MP
- [x] Button enabled khi đủ MP
- [x] Unit được tạo đúng vị trí
- [x] Unit có đúng owner
- [x] Events được trigger đúng
- [x] UI update đúng sau spawn

## 📖 Documentation

- **Setup Guide**: `HUONG_DAN_SETUP_SPAWN_UNIT.md` - Hướng dẫn chi tiết cách setup trong Unity
- **Code Comments**: Tất cả public methods đều có XML comments
- **Inline Comments**: Giải thích logic phức tạp

## 🚀 Next Steps (Sprint 2, Mục 3)

Để hoàn thiện Sprint 2, cần implement tiếp:

1. **Thêm HP và Damage cho Unit**
   - Thêm fields vào UnitData
   - Component HealthSystem cho Unit
   - UI hiển thị HP bar

2. **Combat System**
   - Unit attack logic
   - Damage calculation
   - Death/destroy khi HP <= 0

3. **Unit Movement trên Grid**
   - Tích hợp với ProtoTiles grid system
   - Click để di chuyển unit
   - Hiển thị move range
   - Pathfinding

## 💡 Notes

- Code được viết theo chuẩn C# conventions
- Tuân thủ nguyên tắc từ `Senior.instructions.md`
- Sẵn sàng tích hợp với Grid System (ProtoTiles)
- Events cho phép dễ dàng mở rộng (VFX, SFX, etc.)

## 🐛 Known Issues

- Visual indicator của spawn point chưa có animation
- Chưa có sound effects khi spawn
- Chưa có particle effects khi spawn
- Unit prefab hiện tại dùng Cube placeholder

## 📝 Changelog

### Version 1.0 - Sprint 2.2 Implementation
- ✅ Tạo hệ thống UnitData (ScriptableObject)
- ✅ Tạo Unit component với owner và grid position
- ✅ Implement SpawnPoint system với visual feedback
- ✅ Tạo UnitSpawner với đầy đủ validation
- ✅ UI system với SpawnPanel và SpawnUnitButton
- ✅ Tích hợp với MPManager và TurnManager
- ✅ Events và state management
- ✅ Documentation đầy đủ

---

**Tác giả**: AI Assistant  
**Ngày tạo**: 2025-10-24  
**Sprint**: Sprint 2 - Vòng lặp Chiến đấu Cốt lõi (Phần 1)  
**Mục tiêu**: Hiện thực hành động "spawn unit" tiêu tốn MP
