# Hướng dẫn Setup - Hệ thống Spawn Unit

## Mục lục
1. [Tổng quan](#tổng-quan)
2. [Cấu trúc Files](#cấu-trúc-files)
3. [Setup trong Unity Editor](#setup-trong-unity-editor)
4. [Tạo Unit Data](#tạo-unit-data)
5. [Tạo Unit Prefab](#tạo-unit-prefab)
6. [Setup Spawn Points](#setup-spawn-points)
7. [Setup UI](#setup-ui)
8. [Testing](#testing)

---

## Tổng quan

Hệ thống Spawn Unit cho phép người chơi tạo ra các unit trong trận đấu bằng cách tiêu tốn MP.

### Các thành phần chính:
- **UnitData**: ScriptableObject chứa cấu hình unit
- **Unit**: Component gắn vào prefab unit
- **UnitSpawner**: Quản lý việc spawn unit
- **SpawnPoint**: Đánh dấu vị trí spawn hợp lệ
- **SpawnUnitButton**: UI button để spawn unit
- **SpawnPanel**: Panel chứa danh sách unit có thể spawn

---

## Cấu trúc Files

```
Assets/
├── Scripts/
│   ├── Unit/
│   │   ├── UnitData.cs          ✅ Đã tạo
│   │   ├── Unit.cs              ✅ Đã tạo
│   │   ├── UnitSpawner.cs       ✅ Đã tạo
│   │   └── SpawnPoint.cs        ✅ Đã tạo
│   └── UI/
│       ├── SpawnUnitButton.cs   ✅ Đã tạo
│       └── SpawnPanel.cs        ✅ Đã tạo
├── Data/
│   └── Units/                   📁 Cần tạo
│       ├── BasicSoldier.asset
│       ├── Archer.asset
│       └── Knight.asset
└── Prefabs/
    └── Units/                   📁 Cần tạo
        ├── BasicSoldier.prefab
        ├── Archer.prefab
        └── Knight.prefab
```

---

## Setup trong Unity Editor

### 1. Tạo thư mục

```
1. Tạo folder: Assets/Data/Units
2. Tạo folder: Assets/Prefabs/Units
```

### 2. Setup Scene

Trong Scene Hierarchy:

```
GameManager
├── TurnManager          (đã có)
├── MPManager            (đã có)
└── UnitSpawner          ⭐ MỚI - Tạo Empty GameObject
    └── UnitsContainer   ⭐ MỚI - Child GameObject

SpawnPoints              ⭐ MỚI - Empty GameObject
├── Player1_Spawn_1      ⭐ MỚI
├── Player1_Spawn_2      ⭐ MỚI
├── Player2_Spawn_1      ⭐ MỚI
└── Player2_Spawn_2      ⭐ MỚI
```

---

## Tạo Unit Data

### Bước 1: Tạo ScriptableObject

1. **Right-click** trong folder `Assets/Data/Units`
2. Chọn **Create > TurnBased > Unit Data**
3. Đặt tên: `BasicSoldier`

### Bước 2: Cấu hình Unit Data

Chọn file `BasicSoldier.asset`, cấu hình trong Inspector:

```yaml
Basic Info:
  Unit Name: "Basic Soldier"
  Description: "Đơn vị chiến binh cơ bản"

Cost:
  Spawn Cost: 3

Movement:
  Move Range: 3
  Move Speed: 5

Visual:
  Unit Prefab: [Sẽ gán sau]
  Icon: [Sprite icon của unit]

Owner:
  Owner: Player1  # hoặc Player2
```

### Ví dụ các Unit khác nhau:

```
BasicSoldier:
- Spawn Cost: 3 MP
- Move Range: 3
- Move Speed: 5

Archer:
- Spawn Cost: 4 MP
- Move Range: 2
- Move Speed: 4

Knight:
- Spawn Cost: 5 MP
- Move Range: 2
- Move Speed: 6
```

---

## Tạo Unit Prefab

### Bước 1: Tạo GameObject

1. Trong Hierarchy, **Create > 3D Object > Cube** (hoặc model của bạn)
2. Đặt tên: `BasicSoldier`
3. Scale: (1, 1, 1)

### Bước 2: Thêm Component Unit

1. Chọn GameObject `BasicSoldier`
2. **Add Component > Unit** (TurnBasedGame.Unit)
3. Không cần assign UnitData ở đây (sẽ được set runtime)

### Bước 3: Tạo Prefab

1. Drag `BasicSoldier` GameObject vào folder `Assets/Prefabs/Units/`
2. Xóa GameObject khỏi scene
3. Chọn prefab `BasicSoldier.prefab`

### Bước 4: Link Prefab với UnitData

1. Mở `BasicSoldier.asset` (UnitData)
2. Trong Inspector, tại **Unit Prefab**
3. Drag `BasicSoldier.prefab` vào slot này

---

## Setup Spawn Points

### Bước 1: Tạo Spawn Points cho Player 1

1. Tạo Empty GameObject trong scene
2. Đặt tên: `Player1_Spawn_1`
3. Di chuyển đến vị trí gần thành trì Player 1 (ví dụ: position `(2, 0, 2)`)
4. **Add Component > Spawn Point**

Trong Inspector của `Player1_Spawn_1`:

```yaml
Owner: Player1
Grid Position: (2, 0, 2)  # Sẽ tự động tính từ world position
Visual Indicator: [Auto-generated]
Available Color: Green (0, 1, 0, 0.3)
Unavailable Color: Red (1, 0, 0, 0.3)
```

5. Lặp lại cho `Player1_Spawn_2`, `Player1_Spawn_3`, etc.

### Bước 2: Tạo Spawn Points cho Player 2

Làm tương tự nhưng đặt ở phía thành trì Player 2 và set **Owner: Player2**

### Bước 3: Gộp vào SpawnPoints Container

Move tất cả spawn points vào GameObject `SpawnPoints` để dễ quản lý

```
SpawnPoints
├── Player1_Spawn_1  (Owner: Player1)
├── Player1_Spawn_2  (Owner: Player1)
├── Player2_Spawn_1  (Owner: Player2)
└── Player2_Spawn_2  (Owner: Player2)
```

---

## Setup UnitSpawner

### Bước 1: Configure Component

Chọn GameObject `UnitSpawner` trong scene:

1. **Add Component > Unit Spawner**
2. Trong Inspector:

```yaml
Spawn Settings:
  Units Container: [Drag UnitsContainer GameObject vào đây]

Spawn Points:
  Size: 4  # hoặc số spawn points bạn có
  Element 0: Player1_Spawn_1
  Element 1: Player1_Spawn_2
  Element 2: Player2_Spawn_1
  Element 3: Player2_Spawn_2
```

**Lưu ý**: Nếu không assign Spawn Points ở đây, UnitSpawner sẽ tự động tìm tất cả SpawnPoint trong scene khi Start()

---

## Setup UI

### Bước 1: Tạo Spawn Panel

Trong Canvas UI:

1. **Right-click Canvas > UI > Panel**
2. Đặt tên: `SpawnPanel`
3. Position ở vị trí phù hợp (ví dụ: bottom-center)

### Bước 2: Add Component SpawnPanel

Chọn `SpawnPanel`:
1. **Add Component > Spawn Panel**
2. Tạo child **Vertical Layout Group** hoặc **Horizontal Layout Group**
3. Đặt tên child: `ButtonContainer`

Inspector của SpawnPanel:

```yaml
Configuration:
  Available Units:
    Size: 3
    Element 0: BasicSoldier (UnitData)
    Element 1: Archer (UnitData)
    Element 2: Knight (UnitData)

UI References:
  Button Container: ButtonContainer
  Spawn Button Prefab: [Tạo ở bước tiếp theo]

Settings:
  Show On Start: true
```

### Bước 3: Tạo Spawn Button Prefab

1. **Right-click Canvas > UI > Button**
2. Đặt tên: `SpawnUnitButton`
3. Thêm các child elements:
   - **Text (TMP)**: Tên unit
   - **Text (TMP)**: Cost
   - **Image**: Icon unit

4. **Add Component > Spawn Unit Button**

Inspector của SpawnUnitButton:

```yaml
Unit Configuration:
  Unit Data: [Để trống - sẽ set runtime]

UI References:
  Spawn Button: [Auto-assign]
  Unit Name Text: [Drag Text TMP vào]
  Cost Text: [Drag Text TMP vào]
  Unit Icon: [Drag Image vào]
  Background Image: [Drag Button Image vào]

Colors:
  Affordable Color: White
  Unaffordable Color: Gray
```

5. Drag `SpawnUnitButton` vào `Assets/Prefabs/UI/SpawnUnitButton.prefab`
6. Xóa khỏi Canvas
7. Quay lại `SpawnPanel`, assign prefab này vào **Spawn Button Prefab**

### Bước 4: Link SpawnPanel với UIManager

Chọn `UIManager` trong scene:

```yaml
Spawn Panel: [Drag SpawnPanel vào đây]
```

---

## Testing

### Test Flow:

1. **Play Scene**
2. Kiểm tra Console log:
   ```
   Initialized spawn points - P1: 2, P2: 2
   ```

3. **Kiểm tra MP**:
   - Tung xúc xắc để cộng MP
   - Quan sát thanh MP hiển thị

4. **Test Spawn Button State**:
   - Khi MP < spawn cost → Button disabled (màu xám)
   - Khi MP >= spawn cost → Button enabled (màu trắng)

5. **Click Spawn Button**:
   - Unit được tạo tại spawn point
   - MP bị trừ đúng số lượng
   - Console log:
     ```
     Player 1 spent 3 MP. Current MP: X/20
     Successfully spawned Basic Soldier for Player1 at (2, 0, 2)
     Unit spawned: Basic Soldier for Player 1
     ```

6. **Kiểm tra Spawn Point**:
   - Spawn point indicator đổi màu từ green → red
   - Không thể spawn thêm unit vào spawn point đó

7. **Test với nhiều spawn**:
   - Spawn nhiều unit
   - Kiểm tra MP giảm dần
   - Kiểm tra spawn points hết dần

### Debug Tips:

**Nếu không spawn được:**

1. Kiểm tra Console có error gì không
2. Kiểm tra:
   - MPManager có đủ MP không?
   - UnitSpawner đã được khởi tạo?
   - SpawnPoint có available không?
   - UnitData có prefab không?

**Sử dụng Debug:**
```csharp
// Trong Console, filter theo:
"Successfully spawned"
"Not enough MP"
"No available spawn point"
```

---

## Mở rộng

### Thêm Unit mới:

1. Tạo UnitData mới (Right-click > Create > TurnBased > Unit Data)
2. Tạo Prefab mới
3. Link Prefab với UnitData
4. Thêm vào Available Units trong SpawnPanel

### Tuỳ chỉnh Visual:

- Đổi màu team trong `Unit.cs > ApplyOwnerVisual()`
- Thêm particle effect khi spawn
- Thêm animation cho button

### Tối ưu:

- Object Pooling cho units (để tái sử dụng thay vì Destroy/Instantiate)
- Caching spawn point lookups
- Event-based UI updates thay vì polling

---

## Hoàn thành! ✅

Hệ thống Spawn Unit đã được setup hoàn chỉnh với:
- ✅ Tạo unit bằng cách tiêu tốn MP
- ✅ Kiểm tra vị trí spawn hợp lệ
- ✅ UI button với state management
- ✅ Visual feedback cho người chơi
- ✅ Tích hợp với Turn System và MP System

**Next Steps (Sprint 2, Mục 3):**
- Thêm HP và Damage cho Unit
- Implement hệ thống Combat
- Unit attack và destroy logic
