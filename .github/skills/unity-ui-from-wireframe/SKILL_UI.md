---
name: unity-ui-from-wireframe
description: 'Dựng UI trong Unity Editor từ wireframe hoặc ảnh mockup bằng MCP Unity tools. USE FOR: tạo UI layout, Canvas hierarchy, UGUI components từ ảnh thiết kế; setup Button, Panel, Text, Image, ScrollView theo bản vẽ; căn chỉnh anchor, RectTransform, LayoutGroup theo wireframe. TRIGGERS: "dựng UI theo wireframe", "setup UI từ ảnh", "tạo UI layout theo mockup", "implement UI design", "build UI from image", "mcp unity ui".'
argument-hint: 'Đường dẫn đến file ảnh wireframe hoặc mô tả màn hình UI cần dựng'
---

# Unity UI from Wireframe / Image

Dựng UI UGUI trong Unity Editor từ wireframe hoặc ảnh mockup bằng MCP Unity tools, không cần thao tác tay trong Editor.

## Khi nào dùng skill này

- Nhận được file ảnh wireframe / mockup / screenshot UI
- Cần dựng nhanh layout UI trong scene Unity
- Muốn tự động hoá việc tạo Canvas hierarchy từ thiết kế

## Công cụ MCP chính

| Công cụ | Mục đích |
|---|---|
| `view_image` | Phân tích ảnh wireframe |
| `mcp_unitymcp_manage_ui` | Tạo và cấu hình các UI element (Canvas, Panel, Button, Text…) |
| `mcp_unitymcp_manage_gameobject` | Tạo/đặt tên/đặt parent GameObject |
| `mcp_unitymcp_manage_components` | Thêm/chỉnh component (Image, Button, LayoutGroup, ContentSizeFitter…) |
| `mcp_unitymcp_manage_scene` | Kiểm tra scene hiện tại, lưu scene |
| `mcp_unitymcp_read_console` | Kiểm tra lỗi compilation / runtime sau mỗi thao tác |

## Quy trình từng bước

### Bước 1 — Phân tích wireframe

1. Dùng `view_image` để đọc file ảnh wireframe/mockup.
2. Xác định các **vùng chức năng** (header, body, footer, sidebar, popup…).
3. Liệt kê tất cả **UI element** có trong ảnh với loại tương ứng trong UGUI:

| Thấy trong ảnh | Unity UGUI component |
|---|---|
| Hộp chứa / nền | Panel (Image + RectTransform) |
| Nút bấm | Button (Button + Image + Text/TMP) |
| Văn bản | Text hoặc TextMeshProUGUI |
| Ảnh / icon | Image (Source Image) |
| Danh sách cuộn | ScrollView |
| Thanh HP/tiến trình | Slider hoặc custom Image Fill |
| Grid item | GridLayoutGroup |
| Danh sách dọc/ngang | VerticalLayoutGroup / HorizontalLayoutGroup |

4. Vạch **cây phân cấp** (hierarchy tree) dự kiến:

```
Canvas
└── [Screen Root Panel]
    ├── Header
    │   ├── BackButton
    │   └── TitleText
    ├── Body
    │   └── ...
    └── Footer
        └── ...
```

### Bước 2 — Kiểm tra scene hiện tại

```
mcp_unitymcp_manage_scene(action="get_hierarchy", page_size=50)
```

- Xem Canvas nào đang có trong scene.
- Xác định cần tạo Canvas mới hay ghép vào Canvas sẵn có.
- Ghi lại tên / instanceId của parent GameObject mục tiêu.

### Bước 3 — Tạo Canvas (nếu chưa có)

Dùng `mcp_unitymcp_manage_ui`:
- `render_mode`: `ScreenSpaceOverlay` cho HUD, `WorldSpace` cho in-world UI.
- Nên đặt `reference_resolution` phù hợp với project (VD: 1920×1080).
- Thêm `CanvasScaler` (Scale With Screen Size) và `GraphicRaycaster`.

### Bước 4 — Dựng hierarchy từ trên xuống

Theo thứ tự từ container lớn đến element nhỏ:

1. **Tạo Panel gốc** (full-screen hoặc popup):
   - Anchor: stretch-stretch (`anchorMin=(0,0)`, `anchorMax=(1,1)`, `offsetMin/Max=(0,0)`)
   
2. **Tạo các vùng layout chính** (header, body, footer):
   - Dùng `VerticalLayoutGroup` ở root panel nếu stacked theo chiều dọc.
   - Set `childForceExpandHeight=false`, dùng `LayoutElement` để khoá chiều cao cố định cho header/footer.

3. **Tạo từng UI element** trong từng vùng:
   - Gọi `mcp_unitymcp_manage_ui` với `element_type` phù hợp.
   - Đặt `parent` = instanceId của container vừa tạo.

4. **Cấu hình RectTransform** ngay sau khi tạo:
   - Anchor preset mapping:

| Vị trí mong muốn | anchorMin | anchorMax |
|---|---|---|
| Top-left | (0,1) | (0,1) |
| Top-center | (0.5,1) | (0.5,1) |
| Top-right | (1,1) | (1,1) |
| Center | (0.5,0.5) | (0.5,0.5) |
| Stretch ngang | (0,y) | (1,y) |
| Stretch toàn màn hình | (0,0) | (1,1) |

### Bước 5 — Thêm component và style

Sau khi tạo xong cây hierarchy:

- **Image**: Set `color` (RGBA hex), `sprite` nếu có asset.
- **Button**: Gán `onClick` listener nếu đã có EventHandler script.
- **TextMeshProUGUI**: Set `text`, `fontSize`, `color`, `alignment`.
- **LayoutGroup**: Chỉnh `spacing`, `padding`, `childAlignment`.
- **ContentSizeFitter**: Dùng khi chiều cao/rộng cần auto theo nội dung.

### Bước 6 — Xác minh

1. `mcp_unitymcp_read_console` — kiểm tra lỗi compilation / missing reference.
2. `mcp_unitymcp_manage_scene(action="get_hierarchy")` — xem lại cây UI đã đúng chưa.
3. Nếu dùng Play mode để test: `mcp_unitymcp_manage_editor(action="enter_play_mode")`, sau đó kiểm tra console.
4. `mcp_unitymcp_manage_scene(action="save")` — lưu scene.

## Quyết định thường gặp

**Dùng Text hay TextMeshPro?**
→ Ưu tiên `TextMeshProUGUI`. Chỉ dùng `Text` (legacy) nếu project cũ không có TMP.

**Canvas mới hay dùng lại Canvas sẵn?**
→ Mỗi màn hình (screen) nên có Canvas hoặc sub-canvas riêng nếu cần tối ưu batching.

**Absolute sizing hay Layout Groups?**
→ Layout Groups (VerticalLayoutGroup, GridLayoutGroup) giúp UI responsive hơn; dùng khi wireframe có nhiều item xếp đều.

**Chưa có sprite/font?**
→ Dùng màu placeholder (solid color Image), ghi chú tên asset cần gán sau.

## Checklist hoàn thành

- [ ] Tất cả element trong wireframe đã được tạo trong hierarchy
- [ ] Anchor/pivot đã set đúng theo vị trí trong wireframe
- [ ] Không có lỗi trong Console
- [ ] Scene đã được lưu
- [ ] Tên GameObject rõ ràng, nhất quán (PascalCase, có hậu tố: `Panel`, `Button`, `Text`, `Image`)

## Ví dụ lệnh minh hoạ

```
# Tạo Canvas mới
mcp_unitymcp_manage_ui(action="create_canvas", name="BattleHUD_Canvas", render_mode="ScreenSpaceOverlay")

# Tạo Panel full-screen
mcp_unitymcp_manage_ui(action="create_panel", name="BattleHUD_Root", parent="BattleHUD_Canvas")

# Tạo Button
mcp_unitymcp_manage_ui(action="create_button", name="AttackButton", parent="BattleHUD_Root/Footer")

# Tạo TextMeshPro label
mcp_unitymcp_manage_ui(action="create_text", name="PlayerHPText", parent="BattleHUD_Root/Header", text="HP: 100")
```

## Tài liệu tham khảo

- [Unity UGUI Layout References](./references/ugui-layout.md)
- [RectTransform Anchor Cheatsheet](./references/anchor-cheatsheet.md)
