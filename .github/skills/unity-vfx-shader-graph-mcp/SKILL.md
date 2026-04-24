---
name: unity-vfx-shader-graph-mcp
description: 'Tạo VFX trong Unity bằng Shader Graph qua MCP tools: kiểm tra render pipeline, tạo/chỉnh Shader Graph, tạo Material, gắn vào Mesh/Particle, tinh chỉnh tham số, đóng gói prefab, và kiểm thử. USE FOR: hiệu ứng hit/skill aura/trail/impact; glowing dissolve; shield; pulse; môi trường có hiệu ứng animate bằng shader. TRIGGERS: "tạo vfx shader graph", "unity mcp shader graph", "làm hiệu ứng skill", "shader vfx", "particle + shader graph", "glow dissolve unity".'
argument-hint: 'Mô tả hiệu ứng cần tạo: mục tiêu (mesh/particle), style hình ảnh, màu, tốc độ animate, có cần prefab tái sử dụng không'
---

# Unity VFX bằng Shader Graph (MCP Workflow)

Quy trình chuẩn để tạo hiệu ứng VFX bằng Shader Graph trong Unity Editor qua MCP tools, từ phân tích yêu cầu đến prefab hoàn chỉnh có thể tái sử dụng.

## Khi nào dùng skill này

- Cần tạo hiệu ứng VFX chạy bằng shader (không chỉ Particle mặc định)
- Muốn điều khiển tham số runtime qua Material properties
- Cần đóng gói hiệu ứng thành prefab để gọi lại cho nhiều skill/đòn đánh
- Cần workflow có checkpoint rõ ràng để tránh lỗi pipeline/shader/material

## Công cụ MCP chính

| Công cụ | Mục đích |
|---|---|
| `mcp_unitymcp_manage_graphics` | Kiểm tra render pipeline, cấu hình chất lượng/render settings |
| `mcp_unitymcp_manage_asset` | Tìm/tạo asset Shader Graph, Material, Prefab, Texture |
| `mcp_unitymcp_manage_material` | Gán shader, set màu/tham số float/vector/texture |
| `mcp_unitymcp_manage_texture` | Tạo texture noise/mask phục vụ shader |
| `mcp_unitymcp_manage_gameobject` | Tạo root VFX object, child emitters/mesh slots |
| `mcp_unitymcp_manage_vfx` | Thêm/cấu hình ParticleSystem, LineRenderer, TrailRenderer |
| `mcp_unitymcp_manage_prefabs` | Lưu VFX thành prefab tái sử dụng |
| `mcp_unitymcp_read_console` | Bắt lỗi compile/render/material reference |
| `mcp_unitymcp_unity_reflect` | Xác minh Unity API trước khi viết script điều khiển VFX |

## Quy trình từng bước

### Bước 1 - Chốt đặc tả hiệu ứng

Xác định trước khi thao tác:

1. Mục tiêu render: `Mesh`, `ParticleSystem`, hay `Hybrid`.
2. Hành vi animate: `pulse`, `dissolve`, `flow`, `rim glow`, `distortion`.
3. Input runtime cần expose: màu, cường độ, tốc độ, ngưỡng dissolve.
4. Tuổi thọ hiệu ứng: one-shot (impact) hay loop (aura/buff).
5. Có cần prefab tái sử dụng không.

### Bước 2 - Kiểm tra môi trường render (BẮT BUỘC)

1. Kiểm tra pipeline hiện tại:
   - `mcp_unitymcp_manage_graphics(action="pipeline_get_info")`
2. Nếu không phải URP/HDRP:
   - Dừng và xác nhận chiến lược (chuyển pipeline hoặc dùng shader không phải graph).
3. Tìm asset liên quan đang có:
   - `mcp_unitymcp_manage_asset(action="search", path="Assets", filter_type="Shader", page_size=25, page_number=1)`
   - `mcp_unitymcp_manage_asset(action="search", path="Assets", filter_type="Material", page_size=25, page_number=1)`

### Bước 3 - Quyết định tạo mới hay tái sử dụng

- Nếu đã có Shader Graph gần giống: clone/modify để giảm rủi ro.
- Nếu chưa có: tạo Shader Graph mới cho đúng mục tiêu (Unlit/Lit tùy hiệu ứng).
- Nếu cần noise/mask map: tạo texture procedural trước để tránh placeholder kéo dài.

### Bước 4 - Tạo/cập nhật Shader Graph asset

1. Tạo graph asset theo naming chuẩn, ví dụ:
   - `Assets/MyGame/VFX/Shaders/SG_SkillAura.shadergraph`
2. Thiết kế property tối thiểu nên có:
   - `_BaseColor` (Color)
   - `_Intensity` (Float)
   - `_NoiseTex` (Texture2D)
   - `_FlowSpeed` (Float)
   - `_Dissolve` (Float, nếu cần)
3. Bảo đảm mỗi property quan trọng được `Exposed` để điều khiển từ Material/script.
4. Dùng blend/surface phù hợp:
   - Additive cho glow/energy
   - Alpha cho dissolve/fade
   - Opaque khi cần khối rõ và không cần trong suốt

### Bước 5 - Tạo Material và gán shader

1. Tạo material:
   - Ví dụ: `Assets/MyGame/VFX/Materials/MAT_SkillAura.mat`
2. Gán shader graph cho material:
   - `mcp_unitymcp_manage_material(action="set_material_shader_property", material_path="...", shader="Shader Graphs/SG_SkillAura")`
3. Set giá trị mặc định:
   - màu chủ đạo, intensity, flow speed, dissolve threshold.
4. Nếu có texture hỗ trợ (noise/mask), gán ngay để test thị giác chuẩn.

### Bước 6 - Dựng VFX object trong scene

1. Tạo root object:
   - `VFX_SkillAura_Root`
2. Nhánh theo loại hiệu ứng:
   - Mesh-based: tạo child mesh renderer và gán material.
   - Particle-based: thêm `ParticleSystem`, gán material vào renderer module.
   - Hybrid: kết hợp mesh nền + particle highlight.
3. Tạo thêm LineRenderer/TrailRenderer khi cần streak/trail.

### Bước 7 - Kiểm thử trực quan và tinh chỉnh

1. Chạy test ở khoảng cách camera gameplay thật.
2. Tinh chỉnh theo vòng lặp ngắn:
   - độ sáng (Intensity)
   - tốc độ animate (FlowSpeed)
   - mật độ hạt (nếu particle)
   - mức độ overdraw và readability
3. Luôn kiểm tra console sau mỗi lần chỉnh lớn:
   - `mcp_unitymcp_read_console(action="get", types=["error"], count="20")`

### Bước 8 - Đóng gói prefab tái sử dụng

1. Lưu root object thành prefab:
   - `Assets/MyGame/VFX/Prefabs/PF_SkillAura.prefab`
2. Nếu có biến thể màu/độ mạnh:
   - tạo prefab variants hoặc material variants.
3. Chuẩn hóa naming:
   - `SG_` cho Shader Graph, `MAT_` cho Material, `PF_` cho Prefab.

### Bước 9 - (Tùy chọn) Thêm script điều khiển runtime

Dùng khi cần đổi tham số theo gameplay (charge level, crit, buff stack):

1. Xác minh API trước bằng `unity_reflect`.
2. Viết MonoBehaviour điều khiển MaterialPropertyBlock hoặc material instance.
3. Kiểm tra compile và play test:
   - console phải sạch lỗi.

## Logic quyết định nhanh

**Hiệu ứng cần rẻ (mobile)?**
- Ưu tiên Unlit + ít texture sample + hạn chế transparency chồng lớp.

**Hiệu ứng cần chiều sâu ánh sáng?**
- Dùng Lit Shader Graph, nhưng phải kiểm tra chi phí draw và ánh sáng scene.

**Hiệu ứng xuất hiện ngắn (impact)?**
- One-shot particle + shader dissolve/fade nhanh.

**Hiệu ứng luôn tồn tại (aura)?**
- Loop particle nhẹ + parameter animate ổn định, tránh spike GPU.

## Tiêu chí hoàn thành (Quality Gates)

- [ ] Render pipeline tương thích và shader compile thành công
- [ ] Material đã gán đúng shader graph và hiển thị đúng trong scene
- [ ] Exposed properties đủ để tune runtime (ít nhất: màu, intensity, speed)
- [ ] Không có lỗi trong Console
- [ ] VFX nhìn rõ ở camera gameplay thật, không quá chói hoặc quá mờ
- [ ] Prefab đã lưu và có thể instantiate lại không mất reference
- [ ] Tên asset theo chuẩn (`SG_`, `MAT_`, `PF_`)

## Lỗi thường gặp và cách xử lý

| Lỗi | Nguyên nhân | Cách xử lý |
|---|---|---|
| Shader không hiện đúng | Sai pipeline hoặc shader target không tương thích | Kiểm tra `pipeline_get_info`, dùng đúng loại Shader Graph theo pipeline |
| Material hồng (pink) | Shader compile fail hoặc missing shader | Mở console, sửa graph/property, reimport asset |
| Particle không dùng đúng material | Gán sai renderer module | Set lại material ở ParticleSystem Renderer |
| Hiệu ứng quá nặng | Overdraw cao, quá nhiều hạt/layer transparent | Giảm spawn rate, giảm layer chồng, tối ưu blend |
| Không đổi được tham số runtime | Property chưa Exposed hoặc tên property sai | Expose property trong graph, đồng bộ tên khi set từ code |

## Ví dụ prompt để gọi skill

- "Tạo VFX aura lửa cho nhân vật bằng Shader Graph, loop nhẹ, dùng được cho mobile."
- "Làm hiệu ứng hit điện one-shot bằng particle + shader dissolve, đóng gói prefab để gọi từ combat system."
- "Tạo material variant màu xanh/đỏ cho cùng một VFX prefab và expose intensity để điều khiển runtime."
