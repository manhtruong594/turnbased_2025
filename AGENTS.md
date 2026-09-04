# AGENTS.md - The Summoners: Ordain and Abyss

Hướng dẫn này áp dụng cho toàn bộ project Unity tại
`E:\UnityProject\My Turnbased\turnbased_2025`. Chỉ dẫn cụ thể hơn của người dùng hoặc
`AGENTS.md` nằm gần file mục tiêu hơn được ưu tiên.

## 1. Ngữ cảnh dự án

- Engine: Unity `6000.3.9f1`.
- Render pipeline: Universal Render Pipeline `17.3.0`.
- Input: Input System `1.18.0`.
- UI dùng cả UI Toolkit và uGUI; giữ đúng công nghệ của màn hình đang sửa, không tự chuyển đổi.
- Gameplay là turn-based trên grid, gồm spawn unit, di chuyển, tấn công/skill, spell card,
  capture point, MP, AI và điều kiện kết thúc trận.
- `ScriptableObject` giữ dữ liệu unit, skill, spell, player và shop. Không đưa dữ liệu cân bằng
  có thể chỉnh bởi designer vào code nếu đã có data asset phù hợp.

### Khu vực code chính

- `Assets/Scripts/Manager/TurnSystem`: vòng đời lượt, player turn và AI turn.
- `Assets/Scripts/Manager`: `GameMediator`, MP, map, path và object-pool orchestration.
- `Assets/Scripts/Unit`: unit runtime, spawn, movement, attack, animation và health bar.
- `Assets/Scripts/Skills`: skill pipeline, validation, cooldown, event và hiệu ứng kỹ năng.
- `Assets/Scripts/SpellCard`: spell card, targeting, effect, buff/debuff và UI xác nhận.
- `Assets/Scripts/CapturePoint`: chiếm điểm và điều kiện liên quan.
- `Assets/Scripts/UI`: UI battle, screen flow, component và floating text.
- `Assets/Scripts/Data`: dữ liệu player, shop, unit/skill/spell.
- `Assets/Scripts/ObjectPool` và `Assets/Scripts/VFX`: vòng đời object tái sử dụng và hiệu ứng.
- `Assets/Docs`: thiết kế, roadmap và tài liệu hệ thống; dùng để hiểu ý định nhưng phải đối chiếu
  với code/scene hiện tại vì checklist có thể chưa được cập nhật.

Scene dự án nằm trong `Assets/Scenes`. Không coi scene demo trong package/asset bên thứ ba là
scene gameplay của dự án.

## 2. Trước khi thay đổi

Xác định trước:

- Mục tiêu, phạm vi file và tiêu chí hoàn thành có thể kiểm chứng.
- Luồng liên quan từ input/UI đến manager, model/runtime state, event và presentation.
- Asset Unity liên quan trực tiếp: scene, prefab, `ScriptableObject`, material, animator hoặc UXML/USS.
- Giả định kỹ thuật ảnh hưởng đến cách làm. Nếu có nhiều cách hiểu làm thay đổi đáng kể kết quả,
  nêu giả định hoặc hỏi người dùng.

Với bug, tìm đường tái hiện và nguyên nhân trước khi sửa. Với công việc nhiều bước, dùng kế hoạch
ngắn theo dạng “thay đổi → kiểm chứng”. Không cần kế hoạch dài cho chỉnh sửa nhỏ, rõ ràng.

### Đọc ngữ cảnh

1. Đọc `AGENTS.md` gần file mục tiêu nhất và các file người dùng nêu.
2. Kiểm tra `git status` trước khi sửa; các thay đổi có sẵn thuộc về người dùng.
3. Đọc caller, interface/base class, event, data asset và scene/prefab liên quan trực tiếp.
4. Đối chiếu tài liệu trong `Assets/Docs` với implementation trước khi dựa vào trạng thái checklist.
5. Chỉ mở rộng tìm kiếm khi bằng chứng hiện có chưa đủ để sửa an toàn.

## 3. Nguyên tắc triển khai

- Viết lượng code tối thiểu giải quyết đúng yêu cầu. Không thêm feature, option, abstraction hoặc
  fallback ngoài phạm vi.
- Giữ naming, namespace, formatting và cấu trúc của vùng code đang sửa. Code hiện tại có cả class
  trong global namespace và namespace `TurnBasedGame.*`; không di chuyển namespace ngoài yêu cầu.
- Ưu tiên sửa tại lớp chịu trách nhiệm trực tiếp. Không đưa gameplay rule vào UI hoặc VFX.
- Dùng `GameMediator`/event hiện có khi luồng đã dựa trên event; không tạo event bus song song.
- Skill mới phải đi theo contract `ISkill`/`SkillBase`, validation và cooldown hiện có. Spell mới
  phải đi theo `SpellCardData`, targeting và effect pipeline hiện có.
- Giữ state turn nhất quán với `TurnManager`/`TurnState`; không kết thúc lượt, trừ MP hoặc đổi quyền
  điều khiển từ nhiều nơi cho cùng một action.
- Tái sử dụng `ObjectPoolManager` cho projectile/VFX lặp lại. Tránh `Instantiate`/`Destroy`, LINQ,
  closure và cấp phát managed trong `Update`, vòng lặp pathfinding hoặc hot path gameplay.
- Cache component/reference dùng lặp lại. Không dùng `Find*`, `Camera.main` hoặc `GetComponent`
  mỗi frame.
- Dùng coroutine/animation callback có điểm kết thúc rõ ràng; xử lý unit/target bị hủy hoặc chết
  giữa chuỗi effect nếu luồng đó cho phép.
- Chỉ chia `#region` khi class đủ lớn và vùng trách nhiệm rõ ràng; không thêm region hình thức.

### Serialization và asset Unity

- Không đổi tên/xóa serialized field, component class hoặc enum value đang được asset tham chiếu
  nếu chưa kiểm tra migration. Khi cần đổi tên field, ưu tiên `FormerlySerializedAs`.
- Không đổi GUID trong `.meta`. Khi di chuyển/đổi tên asset, giữ asset và `.meta` đi cùng nhau.
- Không tự tạo `.meta` giả. Để Unity tạo `.meta`, trừ khi workflow đang dùng Unity MCP và có thể
  xác minh asset trong Editor.
- Không sửa YAML scene/prefab hàng loạt. Với thay đổi cần Unity serialization, ưu tiên Unity Editor;
  chỉ dùng Unity MCP khi người dùng yêu cầu làm việc qua MCP.
- Sau khi sửa scene/prefab, kiểm tra missing script/reference và bảo đảm scene cần thiết đã được save.

## 4. Thay đổi có kiểm soát

- Chỉ sửa dòng và asset cần thiết cho yêu cầu. Không refactor, format lại, đổi tên, xóa dead code
  hoặc sửa comment lân cận ngoài phạm vi.
- Dọn import, biến, hàm hoặc asset chỉ khi chính thay đổi hiện tại làm chúng không còn được dùng.
- Không ghi đè, revert, stash hoặc xóa thay đổi có sẵn của người dùng.
- Nếu phát hiện vấn đề ngoài phạm vi, báo riêng; không tự mở rộng task.

### Ranh giới thư mục

- Không sửa `Library/`, `Temp/`, `Logs/`, `Obj/`, `Build/`, `Builds/`, `UserSettings/`, cache,
  `.csproj`, `.sln` hoặc `.slnx` do Unity/IDE sinh ra.
- Xem `Assets/RedBjorn`, `Assets/Mesh Optimizer`, `Assets/EffectsPack`, `Assets/Synty`,
  `Assets/Blink` và `Assets/TextMesh Pro` là nội dung bên thứ ba. Không sửa nếu yêu cầu không trực
  tiếp nhắm tới package/asset đó.
- Không sửa `Packages/manifest.json`, `Packages/packages-lock.json` hoặc `ProjectSettings` nếu thay
  đổi không trực tiếp cần package/cấu hình tương ứng.
- Với scene, prefab, material, animator và import settings, chỉ chỉnh asset được nêu hoặc dependency
  trực tiếp cần thiết để tính năng hoạt động.

## 5. Kiểm chứng

Chọn mức kiểm chứng tương ứng với rủi ro:

- Thay đổi C# thuần: kiểm tra compile errors. `dotnet build` chỉ là kiểm tra cú pháp/tham chiếu sơ bộ;
  Unity Console mới là nguồn xác nhận chính cho Unity serialization và Editor API.
- Gameplay/scene/prefab: kiểm tra Unity Console, reference, scene save và Play Mode theo đúng luồng
  bị ảnh hưởng.
- Skill/spell: kiểm tra valid target, invalid target, MP cost, cooldown, event, target chết giữa
  effect và trạng thái UI sau khi action kết thúc.
- Turn/AI: kiểm tra không double action/double end-turn, không kẹt state và không giữ input sai bên.
- UI: kiểm tra navigation, callback subscribe/unsubscribe, dữ liệu rỗng và scene transition.
- Object pool/VFX: kiểm tra object được reset và trả pool trong cả success, cancel và target mất.

Không tự chạy full build, thay đổi Build Settings hoặc mở Play Mode nếu người dùng không yêu cầu
và thao tác có thể ảnh hưởng phiên Unity đang mở. Nếu không thể kiểm chứng runtime, báo rõ phần chưa
được xác minh. Không coi compile thành công là bằng chứng gameplay hoạt động đúng.

Khi thêm test, ưu tiên Unity Test Framework:

- EditMode cho logic/data không phụ thuộc scene.
- PlayMode cho MonoBehaviour, coroutine, turn flow, scene và integration.

## 6. Tài liệu và báo cáo

- Viết tài liệu dự án bằng tiếng Việt có dấu, định dạng Markdown trong `Assets/Docs`; giữ nguyên
  class name, API, path, identifier, version và error string.
- Khi hành vi hoặc cấu hình hệ thống thay đổi, cập nhật đúng tài liệu liên quan; không đánh dấu hoàn
  thành checklist nếu chưa có bằng chứng kiểm chứng tương ứng.
- Báo cáo cuối ngắn gọn: kết quả, file đã đổi, kiểm chứng đã chạy và giới hạn chưa kiểm chứng.
