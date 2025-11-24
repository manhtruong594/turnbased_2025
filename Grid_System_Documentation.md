# Tài liệu Hệ thống Grid - ProtoTiles

## Tổng quan
Hệ thống ProtoTiles là một framework grid-based mạnh mẽ cho Unity, hỗ trợ nhiều loại lưới khác nhau và các tính năng pathfinding tiên tiến.

## Kiến trúc Tổng thể

```
┌─────────────────────────────────────┐
│            ProtoTiles               │
│         Grid System                 │
├─────────────────────────────────────┤
│  ┌─────────────────────────────┐   │
│  │        MapEntity            │   │
│  │    (Core Management)        │   │
│  └─────────────────────────────┘   │
│           │                         │
│  ┌─────────┴─────────┐              │
│  │                   │              │
│  ▼                   ▼              │
│ ┌─────────┐    ┌─────────────┐     │
│ │MapView  │    │MapSettings  │     │
│ │(Render) │    │(Config)     │     │
│ └─────────┘    └─────────────┘     │
│                       │              │
│              ┌────────┴────────┐    │
│              │                  │    │
│              ▼                  ▼    │
│         ┌─────────┐        ┌─────────┐│
│         │TileData │        │TileEntity││
│         │(Static) │        │(Runtime)││
│         └─────────┘        └─────────┘│
└─────────────────────────────────────┘
```

## Các loại Grid được hỗ trợ

### 1. GridType Enum
```csharp
public enum GridType
{
    Square = 0,      // Lưới vuông
    HexFlat = 1,     // Lưới hex nằm ngang  
    HexPointy = 2,   // Lưới hex đứng dọc
}
```

### 2. GridAxis Enum
Xác định mặt phẳng cho grid:
- **XZ**: Mặt phẳng ngang (Y = up)
- **XY**: Mặt phẳng dọc (Z = forward) 
- **ZY**: Mặt phẳng nghiêng (X = right)

## Sơ đồ Hệ thống Grid Chi tiết

### Square Grid (Lưới Vuông)
```
┌─────┬─────┬─────┐
│(0,2)│(1,2)│(2,2)│
├─────┼─────┼─────┤
│(0,1)│(1,1)│(2,1)│
├─────┼─────┼─────┤
│(0,0)│(1,0)│(2,0)│
└─────┴─────┴─────┘

Neighbors (4 hướng):
    ↑ (0,1)
    ←(-1,0) ● (1,0)→  
    ↓ (0,-1)
```

### Hexagonal Grid (Lưới Hex)
```
Hex Flat (nằm ngang):
    ╱ ╲     ╱ ╲     ╱ ╲
   ╱   ╲   ╱   ╲   ╱   ╲
  ╱(0,1)╲ ╱(1,0)╲ ╱(2,-1)╲
  ╲     ╱ ╲     ╱ ╲     ╱
   ╲   ╱   ╲   ╱   ╲   ╱
    ╲ ╱     ╲ ╱     ╲ ╱
     ╲(0,0) ╱ ╲(1,-1)╱
      ╲     ╱   ╲   ╱
       ╲   ╱     ╲ ╱

Hex Neighbors (6 hướng):
     (0,1,-1)   (1,0,-1)
         ╱ ╲     ╱
(-1,1,0)╱   ╲   ╱(1,-1,0)  
       ╱  ●  ╲ ╱
      ╱       ╲
(-1,0,1)╲     ╱(0,-1,1)
         ╲   ╱
```

## Kiến trúc Class chính

### MapEntity (Lõi hệ thống)
```csharp
public partial class MapEntity : IMapNode, IMapDirectional
{
    // Core Properties
    public GridType Type { get; }
    public MapSettings Settings { get; }
    public MapRules Rules { get; }
    
    // Grid Functions
    Func<Vector3, float, Vector3Int> WorldPosToTile;
    Func<Vector3Int, float, Vector3> TilePosToWorld;
    Func<Vector3Int, Vector3Int, float> DistanceFunc;
    Func<Vector3Int, float, List<Vector3Int>> AreaFunc;
    
    // Key Methods
    TileEntity Tile(Vector3 worldPos);
    TileEntity Tile(Vector3Int tilePos);
    List<TileEntity> PathTiles(Vector3 start, Vector3 end, float range);
    List<Vector3Int> WalkableBorder(Vector3 center, float range);
}
```

### TileEntity (Đơn vị Tile)
```csharp
public partial class TileEntity : INode
{
    public Vector3Int Position { get; }
    public bool Vacant { get; }         // Có thể di chuyển được
    public bool Visited { get; set; }   // Đã thăm (pathfinding)
    public float Depth { get; set; }    // Độ sâu (pathfinding)
    public TileData Data { get; }       // Dữ liệu tĩnh
    public TilePreset Preset { get; }   // Template
}
```

### TileData (Dữ liệu Tile)
```csharp
public class TileData
{
    public Vector3Int TilePos;          // Vị trí trong grid
    public string Id;                   // ID loại tile
    public int PrefabIndex;             // Index prefab để render
    public int MovableArea;             // Vùng di chuyển được
    public float[] SideHeight;          // Độ cao các cạnh (6 cạnh)
}
```

## Pathfinding System

### Sơ đồ Pathfinding
```
┌─────────────────────────────────────┐
│           Pathfinding               │
├─────────────────────────────────────┤
│  ┌─────────────────────────────┐   │
│  │      NodePathFinder         │   │
│  │    (A* Algorithm)           │   │
│  └─────────────────────────────┘   │
│           │                         │
│  ┌────────┴────────┐               │
│  │                 │                │
│  ▼                 ▼                │
│ ┌─────────┐    ┌─────────────┐     │
│ │IMapNode │    │INode        │     │
│ │Interface│    │Interface    │     │
│ └─────────┘    └─────────────┘     │
│      │               │              │
│      ▼               ▼              │
│ ┌─────────┐    ┌─────────────┐     │
│ │MapEntity│    │TileEntity   │     │
│ │(Map)    │    │(Node)       │     │
│ └─────────┘    └─────────────┘     │
└─────────────────────────────────────┘
```

### Quy trình Pathfinding
1. **Khởi tạo**: `Reset(range, startNode)`
2. **Tìm đường**: `PathTiles(start, end, range)`
3. **Kiểm tra láng giềng**: `Neighbours(node)` / `NeighborsMovable(node)`
4. **Tính khoảng cách**: `Distance(nodeA, nodeB)`

## Unit Movement System

### UnitMove Class Flow
```
┌─────────────────────────────────────┐
│           UnitMove                  │
├─────────────────────────────────────┤
│         Properties:                 │
│  • Speed: float                     │
│  • Range: float                     │
│  • Map: MapEntity                   │
│  • Area: AreaOutline               │
│  • Path: PathDrawer                │
└─────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│        Update Loop                  │
├─────────────────────────────────────┤
│  1. HandleWorldClick()             │
│     • Lấy vị trí click            │
│     • Kiểm tra tile hợp lệ        │
│     • Tìm đường đi                │
│     • Thực hiện di chuyển         │
│                                    │
│  2. PathUpdate()                   │
│     • Cập nhật hiển thị path      │
│     • Đổi màu path/area           │
└─────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│      Movement Coroutine             │
├─────────────────────────────────────┤
│  • Duyệt qua từng tile trong path  │
│  • Tính toán hướng di chuyển       │
│  • Xoay unit (LookAt/Flip)        │
│  • Di chuyển từng frame            │
│  • Gọi callback khi hoàn thành     │
└─────────────────────────────────────┘
```

## Visual System

### MapView & Rendering
```csharp
public class MapView : MonoBehaviour
{
    // Render Components
    public MeshFilter TileMeshFilter;
    public MeshRenderer TileMeshRenderer;
    public MeshFilter BorderMeshFilter;
    public MeshRenderer BorderMeshRenderer;
    
    // Core Methods
    void Init(MapEntity mapEntity);
    void GenerateMesh();
    void GridToggle();
}
```

### Area & Path Visualization
- **AreaOutline**: Hiển thị vùng di chuyển được
- **PathDrawer**: Vẽ đường đi với LineRenderer
- **Color States**: Active/Inactive cho feedback

## Input System Integration

### MyInput Class
```csharp
public static class MyInput
{
    // World position detection
    static Vector3 GroundPosition(Plane plane);
    static bool GetOnWorldUp(Plane plane);
    
    // Ray casting for tile selection
    static Ray ScreenPointToRay();
}
```

## Configuration & Rules

### MapRules
- **IsMovable**: Điều kiện tile có thể di chuyển
- **TileConditions**: Các quy tắc cho từng loại tile
- **Movement constraints**: Ràng buộc di chuyển

### TilePreset System
- **Id**: Định danh duy nhất
- **Visual properties**: Material, prefab, etc.
- **Gameplay properties**: Walkable, height, etc.

## Performance Optimization

### Key Features
1. **Object Pooling**: Spawner system cho prefabs
2. **Non-Alloc Methods**: `LineCastNonAlloc()`, etc.
3. **Caching**: Distance functions, vertices, etc.
4. **Efficient Data Structures**: TileDictionary

### Memory Management
```csharp
// Dictionary tối ưu cho tile lookup
public class TileDictionary : Dictionary<Vector3Int, TileEntity>
{
    public TileEntity TryGetOrDefault(Vector3Int key);
}
```

## Extension Points

### Custom Grid Types
1. Thêm vào `GridType` enum
2. Implement conversion functions trong `MapSettings`
3. Tạo preset class tương ứng (như `Hex.cs`, `Square.cs`)

### Custom Tile Conditions
```csharp
[Serializable]
public abstract class TileCondition
{
    public abstract bool IsMet(TileEntity tile);
}
```

## Workflow tích hợp

### 1. Setup Map
```csharp
// Tạo MapSettings asset
MapSettings mapSettings = CreateInstance<MapSettings>();
mapSettings.Type = GridType.Square;
mapSettings.Axis = GridAxis.XZ;

// Khởi tạo Map Entity
MapEntity map = new MapEntity(mapSettings, mapView);
```

### 2. Unit Integration
```csharp
// Khởi tạo unit
unitMove.Init(mapEntity);

// Di chuyển
var path = map.PathTiles(startPos, endPos, maxRange);
unitMove.Move(path, onComplete);
```

### 3. Visual Feedback
```csharp
// Hiển thị vùng di chuyển
area.Show(map.WalkableBorder(unitPos, range), map);

// Hiển thị đường đi
pathDrawer.Show(map.PathPoints(start, end, range), map);
```

## Kết luận

Hệ thống ProtoTiles cung cấp:
- **Flexible Grid Support**: Square, Hex Flat, Hex Pointy
- **Advanced Pathfinding**: A* algorithm tối ưu
- **Visual Integration**: Mesh generation, area highlighting
- **Performance Optimized**: Caching, pooling, non-alloc methods
- **Extensible Architecture**: Easy to add new grid types và conditions

Framework này phù hợp cho các game turn-based, strategy, roguelike với yêu cầu grid movement phức tạp.