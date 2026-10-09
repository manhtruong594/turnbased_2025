# Workflow tạo map nhanh bằng UCP

Áp dụng cho The Summoners: Ordain and Abyss, Unity `6000.3.9f1`, URP `17.3.0`. Đúc kết từ lần tạo KenneyTownTest và gắn vào HUDScene. Đây là hướng dẫn thực hiện cho agent/người dùng; chưa phải công cụ sinh map một lệnh.

## 1. Đầu vào và kết quả

Chốt các thông tin sau từ yêu cầu; dùng mặc định nếu người dùng không chỉ định:

| Đầu vào | Ví dụ/mặc định |
|---|---|
| Tên map duy nhất | `TownTest02` |
| Bộ model | `Assets/MyGame/Models/kenney_fantasy-town-kit_2.0/` |
| Map tham chiếu | `Assets/MyGame/Maps/BattleMap_01/` |
| Scene gameplay | `Assets/Scenes/HUDScene.unity` |
| Grid | Square trên XZ, 21 × 17, `Edge = 2`, Y = 0 |
| Gameplay | 2 spawn mỗi phe, 3 capture, vùng đi được liên thông |
| Bố cục | Hai phe đối xứng, đường giữa và đường vòng, chướng ngại rõ ràng |
| Kiểm chứng | EditMode; chỉ vào Play Mode khi được yêu cầu |

Kết quả cần có:

```text
Assets/MyGame/Maps/<MapName>/
  <MapName>_Map.asset
  <MapName>_Definition.asset
  Prefabs/<MapName>_Environment.prefab
  Prefabs/...tile prefabs...
  Materials/...
Assets/Docs/<MapName>.md
Artifacts/<MapName>/<RunId>/
  HUDScene.before.unity
  preview.png
  ...script tạo map và báo cáo kiểm chứng...
```

Tạo data → dựng môi trường → gắn scene gameplay → kiểm chứng → lưu và bàn giao. Có thể bỏ scene preview trung gian để làm nhanh; vẫn phải tạo đủ MapSettings và BattleMapDefinitionSO.

## 2. Kiểm tra trước khi sửa

1. Đọc `AGENTS.md`, [phân tích Map Editor](../RedBjorn/ProtoTiles/Map%20Editor/MapEditor_Analysis.md), [map Kenney đã tạo](Kenney_Town_Test_Map.md), map mẫu và code hiện tại.
2. Chạy `git status --short`; giữ nguyên thay đổi có sẵn. Xác định đúng Unity instance/project, scene active, Edit Mode, trạng thái compile và Console trước khi sửa.
3. Xác nhận UCP CLI và bridge đang hoạt động. Lần trước dùng bridge `0.6.4`; kiểm tra `ucp --help` và help của subcommand nếu phiên bản khác. Luôn dùng `--bridge-update-policy off` để tránh tự cập nhật package.
4. Nếu scene đang dirty, lưu trạng thái hiện tại trước khi chuyển scene hoặc backup. Không bỏ thay đổi chưa lưu. Tạo backup riêng theo lần chạy, không ghi đè backup cũ.
5. Kiểm tra trước mọi model, shader, preset, reference và output path. Dừng nếu output đã tồn tại; chọn tên mới hoặc thực hiện chỉnh sửa có phạm vi rõ ràng theo yêu cầu.

```powershell
ucp --bridge-update-policy off scene active --json
# Sau khi đã xác định đúng scene và cần lưu thay đổi hiện tại:
ucp --bridge-update-policy off scene save --json
ucp --bridge-update-policy off scene load Assets/Scenes/HUDScene.unity --json
```

Không coi scene demo/package là scene gameplay. HUDScene có sẵn GameMediator, turn, AI, input, HUD, pool và các manager; tái sử dụng chúng.

## 3. Dùng một script Editor để dựng hàng loạt

Đặt script tạm ở `Assets/Scripts/Editor/`, triển khai `UCP.Bridge.IUCPScript` với `Name`, `Description`, `object Execute(string parameters)`. Gom thao tác tạo hàng trăm tile/props vào script, tránh một lệnh UCP cho mỗi GameObject.

Hai script tham khảo đã lưu ngoài Assets:

- [KenneyTownTestMapBuilder.cs](../../Artifacts/KenneyTownTest/KenneyTownTestMapBuilder.cs): tạo data, tile prefab, môi trường và preview trong SampleScene.
- [KenneyHudSetup.cs](../../Artifacts/KenneyTownTest/KenneyHudSetup.cs): gắn definition vào HUDScene, dựng preview, sửa reference và camera.

**Không copy rồi chạy nguyên bản để tạo map mới.** Chúng cố định tên, path, scene, tọa độ và có guard chống chạy lại. Khi chuyển thành script cho lần tạo mới:

- Đổi class, `Name`, output folder, asset name, root name, scene guard và backup path. Không bỏ guard để vượt lỗi trùng dữ liệu.
- Nếu dựng thẳng trong HUDScene, dùng phần tạo asset/môi trường của builder và phần gắn manager của setup; không tạo thêm MapManager/UnitSpawner/CapturePointManager.
- Giới hạn tìm kiếm và xóa object trong đúng scene, đúng nhánh map cần thay. Script setup cũ tìm SpawnPoint/CapturePoint trên các scene đã load; cần thu hẹp trước khi tái sử dụng với nhiều scene.
- Kiểm tra dependency trước mọi mutation. Dùng `SerializedObject`, `PrefabUtility`, `AssetDatabase`, Undo phù hợp; không sửa YAML hàng loạt hoặc tự tạo `.meta`.
- Guard Edit Mode, đúng scene, output chưa tồn tại và backup thành công. Nếu lỗi sau khi sửa một phần, giữ báo cáo và xử lý trạng thái dở dang trước khi chạy lại; exception không tự rollback asset.

### Tạo grid và môi trường

1. Clone MapSettings mẫu để giữ grid/rules tương thích. Xác định preset đi được/chặn bằng dữ liệu thực tế, không mặc định index `0`/`1` đúng với mọi map.
2. Tạo preset và tile prefab riêng trong folder map mới. Giữ mặt di chuyển tại Y = 0; chỉnh phần nền/mesh theo pivot model.
3. Định nghĩa layout bằng tọa độ grid: đường, khu nhà, props, footprint chặn, spawn và capture. Dùng cùng dữ liệu layout để sinh hình ảnh và vùng chặn.
4. Chuyển grid sang world bằng `MapSettings.ToWorld(position, Edge)` hoặc `MapEntity.WorldPosition(position)`. Không dùng trực tiếp tọa độ grid làm world khi `Edge != 1`.
5. Tạo `TileData` với preset Id/PrefabIndex hợp lệ, cập nhật vùng bằng `MapUtils.MarkAreas`. Collider của nhà không thay thế quy tắc ô chặn trong MapSettings.
6. Instantiate model bằng `PrefabUtility.InstantiatePrefab`. Kiểm tra pivot, bounds, scale và footprint thực tế; không để nhà/cây che lối đi mà grid vẫn cho đi xuyên.
7. Lưu môi trường vào EnvironmentPrefab, gồm nhà/props/nền/marker trang trí. Tile và component SpawnPoint/CapturePoint được dựng riêng; tránh nhét thêm các bản sao vào môi trường.
8. Tạo `BattleMapDefinitionSO`, gọi `ConfigureGenerated(...)`, gán `_environmentPrefab` qua `SerializedObject`, lưu asset.

Material phải dùng shader được hỗ trợ trong URP. Không sửa material/model nguồn của bên thứ ba; tạo bản riêng khi cần.

### Gắn vào HUDScene

1. Kiểm tra MapManager, MapView, UnitSpawner, CapturePointManager và GameMediator; tránh manager trùng. Kiểm tra reference `turnManager`, `mpManager`, `mapManager`, `unitSpawner`, `areaPathManager`, `capturePointManager`, `spellCardManager` của GameMediator.
2. Gán `MapManager.Map = definition.MapSettings` và `_editorTestMap = definition` qua `SerializedObject`.
3. Thay preview map cũ trong đúng nhánh MapView. Đặt tile, environment và gameplay point preview dưới MapView; giữ transform phù hợp với tọa độ world của runtime.
4. Tạo SpawnPoint bằng `Initialize(owner, gridPosition)` và CapturePoint bằng `Initialize(gridPosition)`. Cập nhật list `spawnPoints` của UnitSpawner và `_capturePoints` của CapturePointManager để Inspector/validator không giữ reference đã xóa.
5. Chỉ tắt/xóa trang trí map cũ đã xác định rõ. Giữ HUD, player/AI, input, pool và hệ thống gameplay hiện có.
6. Frame camera theo bounds map. Với Kenney 21 × 17: position `(33, 42, -43)`, nhìn về gốc, orthographic size `27`, zoom `8..40` là mốc tham khảo. Kiểm tra controller thực tế và tránh hai controller cùng điều khiển camera.

`MapManager.Awake` ưu tiên `BattleLaunchContext.SelectedMap`, sau đó mới dùng `_editorTestMap` trong Editor. Khi có definition, nó dọn các con của MapView; RuntimeMapBuilder dựng lại tile, môi trường, spawn và capture. Vì vậy chỉnh preview đơn lẻ sẽ không cập nhật map runtime: phải sửa asset/definition nguồn tương ứng.

`_editorTestMap` không có trong build. Thêm map vào luồng chọn map/catalog/multiplayer là công việc riêng, chỉ thực hiện khi yêu cầu bao gồm phần đó.

## 4. Compile → chạy → kiểm chứng

Ví dụ dưới dùng tên script mới `town-test-02-build`; đây là tên `Name` cần tự khai báo trong script, chưa phải command đã có sẵn trong project:

```powershell
ucp --bridge-update-policy off compile --json
# Chờ compile/domain reload hoàn tất, kiểm tra Console trước khi chạy.
ucp --bridge-update-policy off exec run town-test-02-build --json
```

Chỉ thực hiện bước tiếp khi bước trước thành công. Nếu mất kết nối do domain reload, đọc lại trạng thái trước khi retry; không chạy lại mutation khi chưa biết lần trước đã tạo gì. Lấy lại object ID sau reload/thay scene.

Script nên trả kết quả có cấu trúc: scene path, output paths, tổng tile, walkable/blocked, số spawn/capture, kết quả liên thông, missing script/reference/material và trạng thái save.

| Kiểm tra | Điều kiện đạt |
|---|---|
| Definition | `TryValidate(out error)` thành công |
| Tile | Không trùng tọa độ; preset Id tồn tại; prefab/index hợp lệ |
| Gameplay points | Có cả hai phe; mọi điểm tồn tại, đi được, không trùng nhau |
| Đường đi | Spawn đến được capture theo quy tắc di chuyển thực tế; vùng đi được liên thông nếu thiết kế yêu cầu |
| Scene | Không thiếu script/reference cần thiết, không trùng manager/map preview |
| Hiển thị | Không thiếu material/shader; nhà, đường, marker và camera đúng bố cục |
| Console | Không có lỗi mới; ghi riêng warning có sẵn và warning mới |
| Lưu | Asset đã save, scene đúng đường dẫn và không dirty |

`TryValidate` chỉ kiểm tra cấu trúc tối thiểu; không chứng minh đường đi hợp lệ. Tái sử dụng validator trong `Assets/Scripts/Editor/BattleMapCreatorWindow.cs`. `ValidateActiveScene(bool)` hiện là private static; nếu gọi bằng reflection trong script tạm phải kiểm tra method còn tồn tại. Với rules phức tạp, kiểm tra path/neighbor movement của MapEntity; BFS chỉ dựa vào ô vacant kề nhau chưa đủ.

```powershell
ucp --bridge-update-policy off scene save --json
# Tạo folder artifact của lần chạy trước khi chụp; thay RunId bằng giá trị thật.
ucp --bridge-update-policy off screenshot --view game --width 1440 --height 1000 --output Artifacts/TownTest02/RunId/preview.png --json
ucp --bridge-update-policy off scene active --json
```

Mở ảnh để kiểm tra thực tế. Screenshot EditMode không chứng minh HUD và gameplay runtime hoạt động.

Nếu được yêu cầu test Play Mode: xác nhận map được chọn đúng, spawn hai phe, di chuyển hợp lệ/bị chặn, attack/skill hoặc spell, MP, capture, end turn và AI; kiểm tra Console và tránh double spawn/double action. Thoát Play Mode trước khi sửa/lưu cấu hình. Nếu chưa chạy, ghi rõ giới hạn này.

## 5. Dọn script, bàn giao và khôi phục

1. Lưu bản script đã chạy và kết quả kiểm chứng vào `Artifacts/<MapName>/<RunId>/`.
2. Gỡ đúng script Editor tạm qua `AssetDatabase.DeleteAsset` để Unity xử lý `.meta`; giữ tool lâu dài chỉ khi có yêu cầu. Chờ compile lại và kiểm tra Console.
3. Xác nhận scene đã lưu, đúng scene active; kiểm tra diff chỉ gồm thay đổi thuộc map. Viết tài liệu map trong `Assets/Docs`, ghi grid, điểm gameplay, đường dẫn asset, backup, ảnh và phần chưa kiểm chứng.
4. Bàn giao ngắn: map/scene đã tạo, kiểm chứng đạt, cách test và giới hạn. Không gọi map “đã test gameplay” nếu mới kiểm tra EditMode.

Khôi phục: về Edit Mode, giữ bản sao trạng thái hiện tại, thay nội dung scene bằng backup của đúng lần chạy, giữ nguyên `.unity.meta`, rồi reload scene. Việc này loại bỏ thay đổi scene sau thời điểm backup. Asset mới không tự biến mất khi restore scene: chỉ xóa bằng AssetDatabase sau khi xác nhận chúng thuộc lần chạy đó và không còn được tham chiếu. Không xóa cả folder có asset của người dùng.

## 6. Prompt dùng lại

```text
Đọc và thực hiện Assets/Docs/UCP_Map_Creation_Workflow.md.
Sử dụng UCP để tạo map TownTest02 từ
Assets/MyGame/Models/kenney_fantasy-town-kit_2.0/,
tham chiếu Assets/MyGame/Maps/BattleMap_01/.
Grid Square 21 × 17, Edge = 2; hai phe đối xứng,
mỗi phe 2 spawn, 3 capture, đường giữa và đường vòng liên thông.
Tạo MapSettings, BattleMapDefinitionSO, EnvironmentPrefab,
gắn vào Assets/Scenes/HUDScene.unity và frame camera.
Backup trước khi sửa, giữ hệ thống gameplay/HUD hiện có.
Kiểm tra data, đường đi, reference, Console và ảnh preview; lưu scene.
Chưa vào Play Mode. Báo rõ phần runtime chưa kiểm chứng.
```

Muốn kiểm chứng runtime, thay câu cuối về Play Mode bằng phạm vi test cụ thể. Workflow không yêu cầu tạo lại SampleScene khi mục tiêu là test gameplay trực tiếp trong HUDScene.
