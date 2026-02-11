# Map Editor Tool - Phân Tích Chức Năng

## Tổng Quan
Map Editor là một công cụ Unity Editor dùng để tạo và chỉnh sửa bản đồ dựa trên lưới (grid-based map) với hỗ trợ nhiều loại lưới khác nhau (Hex Flat, Hex Pointy, Square).

---

## Cấu Trúc Scripts

### 1. **MapWindow.cs** (742 dòng)
**Chức năng chính**: Editor Window chính của tool, cung cấp giao diện người dùng

#### Các tính năng:
- **Quản lý Map Asset**
  - Tạo mới Map Asset với các tùy chọn Grid Type (HexFlat, HexPointy, Square)
  - Chọn Grid Axis (XZ, XY, ZY)
  - Load/Save map settings

- **Vùng Creator**
  - Tạo Map Asset mới với cấu hình grid tùy chỉnh
  - Chọn loại grid và trục tọa độ

- **Vùng Rules**
  - Cấu hình rotation type cho tiles
  - Thiết lập quy tắc cho map

- **Vùng Painter (Công cụ vẽ)**
  - **Tile Brush**: Vẽ tiles lên map
  - **Tile Eraser**: Xóa tiles khỏi map
  - **Edge Brush**: Vẽ các cạnh/rào cản giữa tiles
  - **Edge Eraser**: Xóa các cạnh/rào cản
  - Toggle hiển thị Grid và Walkable areas
  - Chức năng Clear map
  - Chức năng Place Prefabs (đặt prefabs vào scene)
  - Chức năng Areas Mark (đánh dấu các vùng có thể di chuyển)

- **Vùng Presets (Quản lý Tile Types)**
  - Thêm/xóa tile presets
  - Cấu hình màu sắc cho từng preset trên editor
  - Quản lý prefabs cho mỗi tile type
  - Cấu hình Grid Offset
  - Quản lý Tags cho tiles
  - Preview prefabs
  - Chọn prefab cụ thể cho mỗi tile

#### Các phương thức quan trọng:
- `DoShow()`: Mở Map Editor window
- `MapCreateAsset()`: Tạo Map Asset mới
- `PlacePrefabs()`: Đặt prefabs của tiles vào scene
- `SceneEditingStart/Finish()`: Bắt đầu/kết thúc chế độ edit trong Scene View
- `GetMapHolder()`: Tạo/lấy MapView container trong scene

---

### 2. **MapSceneDrawer.cs** (663 dòng)
**Chức năng chính**: Xử lý việc vẽ và tương tác với map trong Scene View

#### Các tính năng:
- **Vẽ Map trong Scene View**
  - Vẽ lưới (grid) dựa trên loại grid
  - Hiển thị màu sắc cho từng tile preset
  - Hiển thị label cho walkable areas
  - Vẽ cursor theo vị trí chuột

- **Tile Brush System**
  - Xử lý sự kiện chuột (MouseDown, MouseUp, MouseMove)
  - Vẽ tiles theo brush
  - Xóa tiles theo eraser
  - Preview tiles trước khi đặt
  - Hỗ trợ vẽ liên tục (drag to paint)
  - Hiển thị draft tiles với preview prefab hoặc màu

- **Edge Brush System**
  - Vẽ các cạnh/rào cản giữa tiles
  - Xóa các cạnh đã vẽ
  - Hiển thị cursor cho edge editing
  - Tự động cập nhật cạnh đối diện của tile liền kề

- **Cursor System**
  - Hiển thị cursor dạng prefab hoặc hình học
  - Thay đổi màu cursor theo tool (Brush/Eraser)
  - Material riêng cho 2D và 3D

- **Undo/Redo Support**
  - Tích hợp với Unity Undo system
  - Ghi lại trạng thái trước khi thay đổi

#### Các cấu trúc dữ liệu:
- `TilePresetDictionary`: Lưu trữ tiles đã đặt
- `TileColorsDictionary`: Lưu màu sắc cho từng preset
- `SideInfo`: Thông tin về cạnh của tile (vị trí, chỉ số neighbor)
- `TilesPotential`: Tiles đang được hover
- `TilesDraft`: Tiles đang được vẽ tạm thời
- `EdgesDraft`: Edges đang được vẽ tạm thời

#### Các phương thức quan trọng:
- `Draw()`: Vẽ toàn bộ map và tools trong Scene View
- `TileBrushDraw()`: Xử lý vẽ/xóa tiles
- `EdgesBrushDraw()`: Xử lý vẽ/xóa edges
- `EditStart/Update/Finish()`: Quản lý flow của việc edit
- `CursorUpdate()`: Cập nhật vị trí và hiển thị cursor
- `Redraw()`: Vẽ lại toàn bộ map
- `Clear()`: Xóa toàn bộ dữ liệu vẽ

---

### 3. **GuiStyles.cs** (150 dòng)
**Chức năng chính**: Cung cấp các hàm vẽ hình học và style cho GUI

#### Các tính năng:
- **GUI Styles**
  - `HorizontalLine`: Style cho đường phân cách ngang
  - `CenterAlignment`: Style căn giữa text

- **Các hàm vẽ hình học**
  - `DrawHexFlat()`: Vẽ hexagon phẳng
  - `DrawHexPointy()`: Vẽ hexagon nhọn
  - `DrawSquare()`: Vẽ hình vuông
  - `DrawX()`: Vẽ hình X
  - `DrawCircle()`: Vẽ hình tròn
  - `DrawRect()`: Vẽ hình chữ nhật (với rotation và hỗ trợ nhiều axis)
  - `DrawLabel()`: Vẽ label text
  - `DrawHorizontal()`: Vẽ đường ngang với màu

#### Đặc điểm:
- Hỗ trợ vẽ trên 3 trục khác nhau (XZ, XY, ZY)
- Sử dụng Unity Handles API để vẽ trong Scene View
- Tự động tính toán vertices cho các hình học

---

### 4. **MapWindowSettings.cs** (83 dòng)
**Chức năng chính**: ScriptableObject lưu trữ cấu hình của Map Editor

#### Các settings:
- **Theme Settings**
  - Light theme colors
  - Dark theme colors
  - Tự động chọn theme theo Editor theme

- **Layout Settings**
  - Border spacing
  - Foldout height
  - Kích thước các vùng (Creators, Rules, Painter)
  - Label widths

- **Color Settings**
  - Label color
  - Edge colors (normal, cursor, paint, erase)
  - Tile cursor colors (brush, erase)

- **Resources**
  - Brush và Erase icons
  - GUI Skin
  - Shaders cho 2D và 3D drawing
  - Cell border material
  - Map rules

- **Feature Toggles**
  - `AreasAutoMark`: Tự động đánh dấu areas khi edit
  - `ShowWalkable`: Hiển thị walkable info
  - `ShowGrid`: Hiển thị grid
  - `DrawSideTool`: Hiển thị edge tools

#### Singleton Pattern:
- `Instance`: Property để lấy instance duy nhất của settings
- Tự động tìm và load settings từ AssetDatabase

---

## Workflow Sử Dụng

### 1. Tạo Map Mới
1. Mở Map Editor: `Tools > Red Bjorn > Editors > Map`
2. Chọn Grid Type và Grid Axis
3. Click "Create" để tạo Map Asset

### 2. Tạo Tile Presets
1. Trong vùng Presets, click "+" để thêm preset
2. Đặt tên và chọn màu cho preset
3. Thêm prefabs cho preset
4. Có thể thêm tags nếu cần

### 3. Vẽ Map
1. Chọn preset trong danh sách
2. Chọn tool (Tile Brush/Eraser)
3. Click và drag trong Scene View để vẽ
4. Sử dụng Edge tools để thêm rào cản giữa tiles

### 4. Đặt Prefabs vào Scene
1. Click "Place Prefabs" trong vùng Painter
2. Tool sẽ tự động tạo GameObjects từ prefabs đã chọn

### 5. Đánh Dấu Areas
1. Click "Areas Mark" để tự động tính walkable areas
2. Hoặc bật "AreasAutoMark" để tự động mark khi edit

---

## Design Patterns Sử Dụng

### 1. **Editor Window Pattern**
- `MapWindow` extends `EditorWindowExtended`
- Quản lý UI và user interactions

### 2. **Serializable Dictionary Pattern**
- Custom serializable dictionaries cho Unity serialization
- `TilePresetDictionary`, `TileColorsDictionary`

### 3. **Observer Pattern**
- Event `OnBeforeChanged` để thông báo trước khi có thay đổi
- Tích hợp với Unity Undo system

### 4. **Singleton Pattern**
- `MapWindowSettings.Instance` cho settings management

### 5. **State Pattern**
- `IsEditing` state để quản lý edit mode
- `ToolType`, `BrushType` để quản lý tool states

### 6. **Strategy Pattern**
- Các hàm vẽ khác nhau (DrawHexFlat, DrawHexPointy, DrawSquare)
- Được chọn động dựa trên GridType

---

## Tối Ưu & Best Practices

### 1. **Performance**
- Sử dụng HashSet cho TilesPotential và TilesDraft (O(1) lookup)
- Cache materials và shaders
- Chỉ redraw khi cần thiết

### 2. **Memory Management**
- Proper cleanup trong `Release()` và `OnDisable()`
- DestroyImmediate cho temporary objects
- Clear collections khi không dùng

### 3. **Code Organization**
- Separation of concerns: UI logic riêng, Drawing logic riêng
- Single Responsibility: Mỗi class có nhiệm vụ rõ ràng
- Clean Code: Tên biến/hàm có ý nghĩa, code dễ đọc

### 4. **Editor Integration**
- Proper Undo/Redo support
- Scene marking dirty khi có thay đổi
- Asset database integration

---

## Mở Rộng Tương Lai

### Có thể thêm:
1. **Multi-tile Selection**: Chọn nhiều tiles cùng lúc
2. **Copy/Paste**: Sao chép vùng tiles
3. **Rotate/Flip Tools**: Xoay hoặc lật tiles
4. **Layer System**: Nhiều layer cho map
5. **Prefab Variants**: Hỗ trợ random prefabs
6. **Height Map**: Hỗ trợ tiles ở nhiều độ cao khác nhau
7. **Import/Export**: Import map từ file external (JSON, XML)
8. **Minimap View**: Hiển thị toàn bộ map nhỏ gọn
9. **Snap to Grid**: Các tùy chọn snap nâng cao
10. **Custom Brushes**: Người dùng tự tạo brush patterns

---

## Kết Luận

Map Editor là một tool hoàn chỉnh và mạnh mẽ cho việc tạo grid-based maps trong Unity. Tool được thiết kế tốt với:
- **Code rõ ràng và dễ maintain**
- **UI trực quan và dễ sử dụng**
- **Tích hợp tốt với Unity Editor**
- **Hỗ trợ đầy đủ Undo/Redo**
- **Flexible với nhiều loại grid và axis**
- **Performance tốt với caching và optimization**

Tool tuân thủ các nguyên tắc Clean Code và SOLID, dễ dàng mở rộng và bảo trì.
