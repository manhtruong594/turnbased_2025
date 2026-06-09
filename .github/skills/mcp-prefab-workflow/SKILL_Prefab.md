---
name: mcp-prefab-workflow
description: 'Thao tác với prefab Unity đã có sẵn bằng MCP Unity tools — phân tích ngữ cảnh, chỉnh sửa, add/remove components, gán scripts, modify properties. USE FOR: inspect prefab hierarchy và components; thêm/xoá/sửa component trên prefab; thêm script mới vào prefab có sẵn; chỉnh property values trên components; sửa cấu trúc hierarchy của prefab; so sánh và đồng bộ prefab variants. TRIGGERS: "sửa prefab", "thêm component vào prefab", "modify prefab", "edit prefab", "inspect prefab", "add script to prefab", "prefab component", "chỉnh prefab", "mcp prefab edit", "phân tích prefab".'
argument-hint: 'Tên hoặc đường dẫn prefab cần thao tác + mô tả thay đổi mong muốn'
---

# MCP Prefab Workflow — Thao tác Prefab có sẵn

Phân tích, chỉnh sửa, thêm/xoá components và scripts trên prefab Unity đã tồn tại — hoàn toàn qua MCP Unity tools.

## Khi nào dùng skill này

- Cần xem thông tin chi tiết (hierarchy, components, properties) của prefab có sẵn
- Thêm / xoá / sửa component trên prefab
- Viết script mới rồi gán vào prefab đã có
- Chỉnh property values (SerializeField, public fields) trên components
- Thêm / xoá / đổi tên child GameObjects trong prefab hierarchy
- Cần phân tích prefab trước khi quyết định sửa gì

> **Khác với skill `create-full-usable-prefab-with-scripts`**: Skill này bắt đầu từ prefab ĐÃ CÓ và sửa đổi nó, không tạo mới từ đầu.

## Công cụ MCP chính

| Công cụ | Mục đích |
|---|---|
| `mcp_unitymcp_manage_prefabs` | Xem info, instantiate, apply overrides, unpack prefab |
| `mcp_unitymcp_manage_gameobject` | Tạo/sửa/xoá child objects trong prefab instance |
| `mcp_unitymcp_manage_components` | Add / remove / modify components trên GameObjects |
| `mcp_unitymcp_manage_asset` | Tìm prefab asset, tìm scripts/materials trong project |
| `mcp_unitymcp_manage_scene` | Xem hierarchy scene, lưu scene |
| `mcp_unitymcp_create_script` | Tạo C# script mới |
| `mcp_unitymcp_validate_script` | Validate syntax trước khi tạo |
| `mcp_unitymcp_script_apply_edits` | Sửa script đã có |
| `mcp_unitymcp_manage_script` | Đọc nội dung script hiện tại |
| `mcp_unitymcp_read_console` | Kiểm tra lỗi compilation / runtime |
| `mcp_unitymcp_refresh_unity` | Trigger domain reload sau khi sửa script |
| `mcp_unitymcp_unity_reflect` | Xác minh Unity API trước khi viết code |

## Quy trình từng bước

### Bước 1 — Xác định prefab mục tiêu

Tìm prefab trong project:

```
mcp_unitymcp_manage_asset(action="search", search_pattern="<TênPrefab>", filter_type="Prefab", page_size=25)
```

Ghi lại **đường dẫn prefab** (VD: `Assets/MyGame/Prefabs/Units/Warrior.prefab`).

### Bước 2 — Phân tích prefab (BẮT BUỘC)

**Luôn phân tích trước khi sửa.** Dùng 2 bước:

#### 2a. Xem cấu trúc tổng quan

```
mcp_unitymcp_manage_prefabs(action="get_info", prefab_path="Assets/MyGame/Prefabs/Units/Warrior.prefab")
```

Kết quả cho biết: root name, child count, nested prefab references.

#### 2b. Instantiate để inspect chi tiết

Nếu cần xem components / properties chi tiết, instantiate vào scene:

```
mcp_unitymcp_manage_prefabs(action="instantiate", prefab_path="Assets/MyGame/Prefabs/Units/Warrior.prefab")
```

Sau đó kiểm tra components trên từng GameObject:

```
mcp_unitymcp_manage_components(
    action="get",
    target="<GameObjectName>",
    include_properties=false,
    page_size=25
)
```

> **Paging**: Bắt đầu với `include_properties=false` để xem danh sách components. Chỉ request `include_properties=true` với `page_size` nhỏ (3-10) khi cần xem giá trị cụ thể.

#### 2c. Xem properties chi tiết của component cụ thể

```
mcp_unitymcp_manage_components(
    action="get",
    target="<GameObjectName>",
    component_type="<ComponentType>",
    include_properties=true,
    page_size=5
)
```

### Bước 3 — Thực hiện thay đổi

Tuỳ loại thay đổi, chọn workflow phù hợp:

---

#### 3A. Thêm component có sẵn (built-in hoặc script đã compile)

```
mcp_unitymcp_manage_components(
    action="add",
    target="<GameObjectName>",
    component_type="<FullTypeName>"
)
```

Ví dụ:
- Built-in: `"BoxCollider"`, `"Rigidbody"`, `"Animator"`
- Custom script: `"TurnBasedGame.Unit.UnitMovement"` (full namespace)

---

#### 3B. Viết script mới rồi gán vào prefab

**Thứ tự bắt buộc**: Viết → Compile thành công → Gán component

1. **Xác minh API** (nếu dùng Unity API chưa chắc chắn):
```
mcp_unitymcp_unity_reflect(action="search", search_term="<APIKeyword>")
```

2. **Validate syntax**:
```
mcp_unitymcp_validate_script(script_content="<full_code>")
```

3. **Tạo script**:
```
mcp_unitymcp_create_script(
    script_name="NewComponent",
    script_content="<full_code>",
    directory="Assets/Scripts/{SystemName}"
)
```

4. **Đợi compile, kiểm tra lỗi**:
```
mcp_unitymcp_read_console(filter_type="Error")
```
Nếu có lỗi → sửa bằng `mcp_unitymcp_script_apply_edits` → kiểm tra lại.

5. **Gán script lên prefab instance**:
```
mcp_unitymcp_manage_components(
    action="add",
    target="<GameObjectName>",
    component_type="TurnBasedGame.{SystemName}.NewComponent"
)
```

---

#### 3C. Sửa property values trên component

```
mcp_unitymcp_manage_components(
    action="modify",
    target="<GameObjectName>",
    component_type="<ComponentType>",
    properties={"fieldName": newValue}
)
```

Các loại giá trị hỗ trợ:
- Số: `{"speed": 10.0, "maxHP": 100}`
- String: `{"displayName": "Warrior"}`
- Bool: `{"isActive": true}`
- Vector3: `{"position": {"x": 0, "y": 1, "z": 0}}`
- Object reference: dùng instanceId hoặc asset path

---

#### 3D. Xoá component

```
mcp_unitymcp_manage_components(
    action="remove",
    target="<GameObjectName>",
    component_type="<ComponentType>"
)
```

---

#### 3E. Thêm / xoá child GameObject

**Thêm child**:
```
mcp_unitymcp_manage_gameobject(action="create", name="VFX_Slot", parent="<PrefabRootName>")
```

**Xoá child**:
```
mcp_unitymcp_manage_gameobject(action="delete", target="<ChildName>")
```

---

#### 3F. Sửa script đã có trên prefab

1. **Đọc script hiện tại**:
```
mcp_unitymcp_manage_script(action="read", script_path="Assets/Scripts/{Path}/MyScript.cs")
```

2. **Áp dụng thay đổi**:
```
mcp_unitymcp_script_apply_edits(
    script_path="Assets/Scripts/{Path}/MyScript.cs",
    edits=[{"old_text": "...", "new_text": "..."}]
)
```

3. **Kiểm tra compile**:
```
mcp_unitymcp_read_console(filter_type="Error")
```

### Bước 4 — Apply overrides về prefab asset

Sau khi sửa xong trên prefab instance trong scene, **PHẢI apply** để lưu thay đổi vào prefab asset:

```
mcp_unitymcp_manage_prefabs(
    action="apply_overrides",
    target="<PrefabInstanceRootName>"
)
```

> **CRITICAL**: Nếu bỏ qua bước này, tất cả thay đổi sẽ mất khi xoá instance hoặc reload scene.

### Bước 5 — Xác minh

1. **Console sạch**:
```
mcp_unitymcp_read_console(filter_type="Error")
```

2. **Kiểm tra prefab asset đã cập nhật**:
```
mcp_unitymcp_manage_prefabs(action="get_info", prefab_path="<prefab_path>")
```

3. **Xác nhận components đầy đủ** (nếu cần):
```
mcp_unitymcp_manage_components(
    action="get",
    target="<GameObjectName>",
    include_properties=false,
    page_size=25
)
```

4. **Dọn dẹp** — xoá instance khỏi scene (nếu chỉ cần sửa prefab, không cần giữ trong scene):
```
mcp_unitymcp_manage_gameobject(action="delete", target="<PrefabInstanceRootName>")
```

## Quy ước project

| Quy ước | Chi tiết |
|---|---|
| Namespace | `TurnBasedGame.{SystemName}` |
| Script location | `Assets/Scripts/{SystemName}/` |
| Prefab location | `Assets/MyGame/Prefabs/{Category}/` |
| Naming | PascalCase cho class/GameObject, camelCase cho private fields |
| SerializeField | `[SerializeField]` + `[Header("...")]` |
| Code quality | Clean Code, SOLID, KISS |

## Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|
| Component type not found | Script chưa compile xong hoặc sai namespace | Gọi `refresh_unity`, đợi compile, dùng full qualified name |
| Cannot modify prefab instance | Instance bị lock hoặc đang ở Prefab Mode | Thoát Prefab Mode, instantiate lại |
| Apply overrides failed | Prefab variant conflict hoặc missing parent | Kiểm tra prefab hierarchy, dùng `get_info` trước |
| Missing reference sau apply | Object reference trỏ tới scene object (bị mất) | Chỉ reference tới assets hoặc objects cùng prefab |
| Script compilation error | Sai syntax hoặc thiếu using | Đọc console error, sửa bằng `script_apply_edits` |
| Properties not saved | Chưa apply overrides | Gọi `apply_overrides` sau mỗi lần sửa xong |

## Quyết định thường gặp

**Sửa trực tiếp prefab asset hay instantiate rồi apply?**
→ Instantiate lên scene sau đó sửa trên instance → apply vào prefab;

**Thêm component lên root hay child?**
→ Logic scripts → root. Visual/physics → child tương ứng (Model, Colliders…).

**Dùng `manage_components(modify)` hay sửa script source?**
→ Sửa giá trị default inspector → `modify`. Sửa logic code → sửa script source.

**Sửa 1 prefab hay sửa nhiều prefab cùng lúc?**
→ Từng cái một.

## Checklist hoàn thành

- [ ] Đã phân tích prefab trước khi sửa (get_info / get components)
- [ ] Thay đổi đã được thực hiện đúng trên prefab instance
- [ ] Scripts mới (nếu có) đã compile thành công — 0 errors
- [ ] `apply_overrides` đã được gọi để lưu về prefab asset
- [ ] Prefab asset đã xác nhận có đủ components / properties mới
- [ ] Console sạch (không có error liên quan)
