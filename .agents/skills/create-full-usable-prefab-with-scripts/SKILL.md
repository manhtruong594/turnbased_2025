---
name: create-full-usable-prefab-with-scripts
description: 'Tạo prefab hoàn chỉnh trong Unity Editor với đầy đủ C# scripts được viết, compile, và gán lên prefab bằng MCP Unity tools. USE FOR: tạo prefab mới kèm MonoBehaviour scripts; tạo ScriptableObject data assets cho prefab; setup component hierarchy hoàn chỉnh sẵn sàng dùng; tạo hệ thống mới (unit, spell, UI element) từ mô tả. TRIGGERS: "tạo prefab", "create prefab with script", "tạo GO có script", "setup prefab hoàn chỉnh", "mcp unity prefab", "tạo hệ thống mới với prefab".'
argument-hint: 'Mô tả prefab cần tạo: tên, chức năng, components, scripts cần có'
---

# Create Full Usable Prefab with Scripts

Tạo prefab hoàn chỉnh trong Unity Editor — bao gồm viết C# scripts, chờ compile, tạo GameObject, gán components, và lưu thành prefab — hoàn toàn qua MCP Unity tools.

## Khi nào dùng skill này

- Cần tạo một prefab mới với custom MonoBehaviour scripts
- Tạo hệ thống gameplay mới (unit type, spell effect, UI widget…)
- Muốn tự động hoá toàn bộ flow: viết code → compile → assemble → save prefab
- Cần tạo ScriptableObject data assets đi kèm prefab

## Công cụ MCP chính

| Công cụ | Mục đích |
|---|---|
| `mcp_unitymcp_create_script` | Tạo file C# script mới |
| `mcp_unitymcp_validate_script` | Kiểm tra syntax script trước khi tạo |
| `mcp_unitymcp_read_console` | Kiểm tra lỗi compilation sau khi tạo script |
| `mcp_unitymcp_refresh_unity` | Trigger domain reload / recompile |
| `mcp_unitymcp_manage_gameobject` | Tạo GameObject, đặt tên, set parent |
| `mcp_unitymcp_manage_components` | Thêm/cấu hình components (kể cả custom scripts) |
| `mcp_unitymcp_manage_prefabs` | Tạo prefab từ GameObject hoặc modify prefab asset |
| `mcp_unitymcp_manage_asset` | Tạo ScriptableObject instances, tìm assets |
| `mcp_unitymcp_manage_scene` | Kiểm tra hierarchy, lưu scene |
| `mcp_unitymcp_unity_reflect` | Xác minh Unity API trước khi viết code |

## Quy ước của project

Tuân thủ nghiêm ngặt các quy ước sau khi viết code:

| Quy ước | Chi tiết |
|---|---|
| Namespace | `TurnBasedGame.{SystemName}` (VD: `TurnBasedGame.Unit`, `TurnBasedGame.SpellCard`) |
| Script location | `Assets/Scripts/{SystemName}/` |
| Prefab location | `Assets/MyGame/Prefabs/{Category}/` |
| Naming | PascalCase cho class, camelCase cho private fields |
| SerializeField | Dùng `[SerializeField]` với `[Header("...")]` để nhóm trong Inspector |
| Documentation | XML doc comments trên public classes và key methods |
| Patterns | Singleton (có lazy init), Mediator, Event-driven, Interface-based |
| ScriptableObject | `[CreateAssetMenu]` cho data assets, đặt trong `Assets/Scripts/Data/` |
| Code quality | Clean Code, SOLID, KISS. Class không quá 500 dòng |

## Quy trình từng bước

### Bước 1 — Phân tích yêu cầu

Trước khi viết bất kỳ code nào:

1. **Xác định chức năng** của prefab: nó làm gì, tương tác với hệ thống nào?
2. **Liệt kê scripts cần viết**:
   - MonoBehaviour scripts (gán lên GameObject)
   - ScriptableObject data classes (nếu cần data-driven)
   - Interface / abstract class (nếu cần mở rộng)
3. **Liệt kê Unity built-in components** cần thêm (Rigidbody, Collider, Animator…)
4. **Xác định hierarchy** của prefab:

```
[PrefabRoot]
├── Model (MeshRenderer, Animator)
├── Colliders (BoxCollider)
├── VFX (ParticleSystem)
└── UI (Canvas → HealthBar)
```

### Bước 2 — Verify Unity API (BẮT BUỘC)

Trước khi viết script, **phải xác minh API** bằng `unity_reflect`:

```
# Tìm API cần dùng
mcp_unitymcp_unity_reflect(action="search", search_term="<ClassName>")

# Xem chi tiết type
mcp_unitymcp_unity_reflect(action="get_type", type_name="<FullTypeName>")

# Xem chi tiết member
mcp_unitymcp_unity_reflect(action="get_member", type_name="<FullTypeName>", member_name="<Method>")
```

**KHÔNG** dựa vào training data cho Unity API — luôn verify trước.

### Bước 3 — Viết và tạo scripts

**Thứ tự tạo script quan trọng**: Interface/Base → Data (SO) → Logic (MonoBehaviour)

Với mỗi script:

1. **Validate trước khi tạo**:
```
mcp_unitymcp_validate_script(script_content="<full_code>")
```

2. **Tạo script file**:
```
mcp_unitymcp_create_script(
    script_name="MyComponent",
    script_content="<full_code>",
    directory="Assets/Scripts/{SystemName}"
)
```

3. **Kiểm tra compilation** sau MỖI script:
```
mcp_unitymcp_read_console(filter_type="Error")
```

> **CRITICAL**: Phải đợi compilation thành công trước khi tạo script tiếp theo. Nếu có lỗi, sửa ngay bằng `mcp_unitymcp_manage_script(action="edit")` hoặc `mcp_unitymcp_script_apply_edits`.

#### Template MonoBehaviour chuẩn

```csharp
using UnityEngine;

namespace TurnBasedGame.{SystemName}
{
    /// <summary>
    /// Mô tả ngắn gọn chức năng.
    /// </summary>
    public class MyComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform targetTransform;

        [Header("Settings")]
        [SerializeField] private float speed = 5f;

        private void Awake()
        {
            // Cache references
        }
    }
}
```

#### Template ScriptableObject chuẩn

```csharp
using UnityEngine;

namespace TurnBasedGame.Data
{
    /// <summary>
    /// Mô tả data asset.
    /// </summary>
    [CreateAssetMenu(fileName = "New{Name}", menuName = "TurnBasedGame/{Category}/{Name}")]
    public class MyDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
    }
}
```

### Bước 4 — Tạo GameObject và gán components

Sau khi TẤT CẢ scripts compile thành công:

1. **Tạo root GameObject**:
```
mcp_unitymcp_manage_gameobject(action="create", name="MyPrefab")
```

2. **Tạo child objects** (nếu cần hierarchy):
```
mcp_unitymcp_manage_gameobject(action="create", name="Model", parent="MyPrefab")
```

3. **Thêm built-in components**:
```
mcp_unitymcp_manage_components(
    action="add",
    target="MyPrefab",
    component_type="BoxCollider"
)
```

4. **Thêm custom scripts** (dùng full namespace):
```
mcp_unitymcp_manage_components(
    action="add",
    target="MyPrefab",
    component_type="TurnBasedGame.{SystemName}.MyComponent"
)
```

5. **Cấu hình component values** (nếu cần set giá trị mặc định):
```
mcp_unitymcp_manage_components(
    action="modify",
    target="MyPrefab",
    component_type="TurnBasedGame.{SystemName}.MyComponent",
    properties={"speed": 10.0}
)
```

### Bước 5 — Lưu thành Prefab

```
mcp_unitymcp_manage_prefabs(
    action="create",
    source_object="MyPrefab",
    prefab_path="Assets/MyGame/Prefabs/{Category}/MyPrefab.prefab"
)
```

Sau khi lưu prefab, **xoá GameObject trong scene** (tuỳ yêu cầu):
```
mcp_unitymcp_manage_gameobject(action="delete", target="MyPrefab")
```

### Bước 6 — Tạo ScriptableObject instances (nếu cần)

Nếu prefab dùng data assets:

```
mcp_unitymcp_manage_asset(
    action="create_scriptable_object",
    type_name="TurnBasedGame.Data.MyDataSO",
    asset_path="Assets/MyGame/Data/MyData_Default.asset"
)
```

### Bước 7 — Xác minh toàn bộ

1. **Console sạch**:
```
mcp_unitymcp_read_console(filter_type="Error")
```

2. **Prefab asset tồn tại**:
```
mcp_unitymcp_manage_asset(action="search", search_pattern="MyPrefab", filter_type="Prefab")
```

3. **Prefab có đủ components**:
```
mcp_unitymcp_manage_prefabs(action="get_info", prefab_path="Assets/MyGame/Prefabs/{Category}/MyPrefab.prefab")
```
hoặc
```
mcp_unitymcp_manage_gameobject(action="get_components", target="MyPrefab", include_properties=true, page_size=10)
```

4. **Test nhanh** (tuỳ chọn): instantiate prefab và chạy Play mode:
```
mcp_unitymcp_manage_prefabs(action="instantiate", prefab_path="Assets/MyGame/Prefabs/{Category}/MyPrefab.prefab")
mcp_unitymcp_manage_editor(action="enter_play_mode")
mcp_unitymcp_read_console(filter_type="Error")
mcp_unitymcp_manage_editor(action="exit_play_mode")
```

## Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|
| Script compilation failed | Sai syntax, thiếu using | Đọc console error, sửa script bằng `script_apply_edits` |
| Component type not found | Script chưa compile xong hoặc sai namespace | Đợi compile, dùng full qualified name |
| Missing reference trên prefab | SerializeField chưa được gán | Dùng `manage_components(action="modify")` để gán reference |
| Prefab save failed | Path không hợp lệ hoặc folder chưa có | Kiểm tra path, tạo folder trước bằng `manage_asset` |
| Domain reload chưa xong | Script mới chưa available | Gọi `refresh_unity`, đợi, rồi `read_console` lại |

## Quyết định thường gặp

**MonoBehaviour hay ScriptableObject?**
→ Cần gắn vào GameObject và chạy runtime logic → MonoBehaviour.
→ Cần lưu data tĩnh, config, dùng chung → ScriptableObject.

**Một script hay nhiều script?**
→ Theo Single Responsibility Principle. VD: `UnitMovement`, `UnitHealth`, `UnitCombat` thay vì 1 script `Unit` khổng lồ.

**Flat hierarchy hay nested children?**
→ Nếu prefab có nhiều visual parts (model, VFX, UI) → nested.
→ Nếu đơn giản (1 mesh + 1 script) → flat.

**Đặt script ở đâu?**
→ Hệ thống mới: `Assets/Scripts/{NewSystemName}/`
→ Mở rộng hệ thống có sẵn: cùng folder với scripts hiện tại.

## Checklist hoàn thành

- [ ] Tất cả scripts đã compile thành công (0 errors trong Console)
- [ ] Prefab asset đã được tạo tại `Assets/MyGame/Prefabs/{Category}/`
- [ ] Tất cả custom scripts đã được gán lên đúng GameObject trong prefab
- [ ] SerializeField references đã được gán (hoặc ghi chú cần gán thủ công)
- [ ] ScriptableObject data assets đã tạo (nếu cần)
- [ ] Namespace đúng quy ước `TurnBasedGame.{SystemName}`
- [ ] Code tuân thủ Clean Code, SOLID, KISS
- [ ] Hierarchy / naming rõ ràng, nhất quán