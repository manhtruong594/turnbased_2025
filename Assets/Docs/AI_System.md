# Hệ Thống AI - Hướng Dẫn Sử Dụng

## Tổng Quan
Hệ thống AI cho phép opponent tự động chơi bằng cách spawn unit, di chuyển và tấn công player.

## Các File Chính
- **AIController.cs**: Xử lý toàn bộ logic AI (spawn, di chuyển, tấn công)
- **PlayerController.cs**: Đã được cập nhật để hỗ trợ cả người chơi và AI

## Cách Sử Dụng

### 1. Setup trong Unity Editor

#### Bước 1: Thêm AI cho Player 2 (Opponent)
1. Chọn GameObject chứa `PlayerController` của Player 2 trong Hierarchy
2. Trong Inspector panel:
   - Tick vào checkbox **"Is AI"** 
   - (Optional) Kéo thả `AIController` component vào field **"Ai Controller"** nếu muốn tự cấu hình

#### Bước 2: Cấu hình Available Units cho AI
1. Trong `AIController` component:
   - Mở mục **"AI Settings"**
   - Set **"Ai Player ID"** = Player2
   - Thêm các UnitData vào list **"Available Units"** (các unit mà AI có thể spawn)
   - Điều chỉnh **"Action Delay"** để thay đổi tốc độ hành động của AI (mặc định: 1 giây)

### 2. Logic Hoạt Động của AI

Khi đến lượt AI, hệ thống sẽ tự động:

1. **Spawn Unit**: 
   - Chọn ngẫu nhiên một unit từ danh sách `availableUnits`
   - Spawn tại spawn point khả dụng (nếu đủ MP)

2. **Di Chuyển và Tấn Công**:
   - Tìm enemy gần nhất
   - Nếu trong tầm đánh: tấn công ngay
   - Nếu ngoài tầm: di chuyển về phía enemy, sau đó tấn công nếu có thể

3. **Kết Thúc Lượt**:
   - Tự động end turn sau khi hoàn thành tất cả hành động

### 3. Tùy Chỉnh AI

#### Thay đổi danh sách units AI có thể spawn:
```csharp
// Trong code hoặc từ Inspector
aiController.SetAvailableUnits(newUnitList);
aiController.AddAvailableUnit(specificUnitData);
```

#### Điều chỉnh thời gian giữa các hành động:
- Thay đổi giá trị **"Action Delay"** trong Inspector (tính bằng giây)
- Giá trị nhỏ = AI hành động nhanh hơn
- Giá trị lớn = AI hành động chậm hơn, dễ quan sát

## Lưu Ý Kỹ Thuật

### Yêu Cầu Hệ Thống
- **UnitSpawner**: Phải có trong scene để spawn units
- **MapManager**: Phải có để tính toán pathfinding
- **TurnManager**: Phải có để quản lý lượt chơi
- **MPManager**: Phải có để kiểm tra và tiêu tốn MP

### Các Thành Phần Unit Cần Thiết
Mỗi unit phải có:
- `Unit` component
- `UnitMove` component (di chuyển)
- `UnitAttack` component (tấn công)

## Troubleshooting

### AI không spawn unit
- Kiểm tra list **"Available Units"** có rỗng không
- Kiểm tra AI có đủ MP không (xem MPManager)
- Kiểm tra có spawn point khả dụng không

### AI không di chuyển/tấn công
- Kiểm tra units có đầy đủ components không (Unit, UnitMove, UnitAttack)
- Kiểm tra MapManager đã khởi tạo đúng chưa
- Xem Console log để debug

### AI hành động quá nhanh/chậm
- Điều chỉnh **"Action Delay"** trong AIController
- Điều chỉnh thời gian giới hạn lượt trong TurnManager (hiện tại: 60 giây cho AI)

## Code Example

### Bật AI cho một PlayerController bằng code:
```csharp
PlayerController player2 = GetComponent<PlayerController>();
// Không thể thay đổi isAI runtime vì nó là SerializeField
// Phải set từ Inspector hoặc tạo mới GameObject với AI setup sẵn
```

### Tùy chỉnh logic AI:
Mở file `AIController.cs` và chỉnh sửa các method:
- `SpawnRandomUnit()`: Logic spawn unit
- `ProcessUnitAction()`: Logic quyết định hành động
- `FindNearestEnemy()`: Thuật toán chọn target

## Performance

- AI chỉ hoạt động khi đến lượt của mình
- Các tính toán pathfinding được cache bởi MapEntity
- Độ phức tạp: O(n*m) với n = số units AI, m = số enemies

## Tương Lai

Các cải tiến có thể thêm:
- [ ] Nhiều mức độ khó (Easy, Medium, Hard)
- [ ] AI ưu tiên target yếu hơn
- [ ] AI biết phòng thủ khi HP thấp
- [ ] Chiến thuật nhóm (group tactics)
- [ ] Machine Learning integration
