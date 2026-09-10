# Multiplayer protocol — Giai đoạn 2

Cập nhật: `2026-09-10`.

## Phạm vi

Đã triển khai protocol cho `EndTurn`, `SpawnUnit`, `MoveUnit`, registry content/runtime, kiểm tra tương
thích và RNG riêng của authority. Luồng production hiện vẫn chạy local. Transport gameplay, command
attack/skill/spell, transaction và state delta thuộc giai đoạn 3; snapshot/bootstrap client thuộc giai đoạn 4.

## ID

| Identifier | Biểu diễn và nguồn |
|---|---|
| `MatchId` | Chuỗi GUID lowercase 32 ký tự; authority tạo khi `LocalMatchAuthority.Reset` |
| `PlayerId` | Wire enum byte: `Player1 = 1`, `Player2 = 2`; adapter ánh xạ sang `PlayerID` hiện có |
| `UnitRuntimeId` | `ulong`, bắt đầu từ 1; authority cấp tăng dần, không tái sử dụng sau removal |
| `SpawnPointId` | Chuỗi `owner:x:y:z`, invariant culture; tính sau khi xác định tile của spawn point |
| `UnitContentId` | GUID `.meta` của `UnitData`; catalog liên kết tới data và đúng một unit prefab |
| `SkillContentId` | GUID `.meta` của skill asset; clone runtime giữ ID nguồn |
| `SpellContentId` | GUID `.meta` của `SpellCardData`, tra qua `MatchContentRegistry.GetId` |
| Grid | `GridCoordinate`: ba số `int` X/Y/Z; adapter chuyển thành `Vector3Int` |

Không dùng `GetInstanceID`, tên GameObject, tên hiển thị hoặc thứ tự `FindObjectsByType` làm identity.
Spawn point cùng owner và tọa độ bị từ chối. Danh sách spawn point được sắp theo ID để chọn điểm mặc
định ổn định. Hai prefab dùng chung một `UnitData` bị coi là content mapping mơ hồ và dừng bake.

Client phải nhận `MatchId`, `UnitRuntimeId` và RNG state từ host; không tự tạo chúng để đoán state host.
`MatchRuntimeRegistry.BindUnit` hỗ trợ ánh xạ ID host sang object replica. Việc vận chuyển/bind từ
spawn result hoặc snapshot sẽ nối ở các giai đoạn sau. Registry mới được tạo mỗi trận; unit bị hủy được
gỡ khỏi registry, ID đã cấp không quay lại bộ đếm.

## Content catalog

`MatchContentCatalogBuilder` chạy sau Editor domain reload, trước Play Mode và trước build; cũng có menu:

```text
Tools > Multiplayer > Rebuild Content Catalog
```

Unity Editor tạo `Assets/Resources/Multiplayer/MatchContentCatalog.asset` cùng `.meta`. Không sửa GUID
hoặc ghi thêm ID vào prefab/data asset gốc. Cần để Editor import script mới trước khi chạy trận.

Nguồn catalog:

- Prefab có root `UnitController` trong `Assets/MyGame` và `UnitData` của chúng.
- Skill/spell asset trong `Assets/Scripts/Data`.
- Dependency hash của các nguồn trên và scene trong `Assets/Scenes` được đưa vào compatibility hash.

`MatchContentRegistry` kiểm tra ID/reference thiếu, trùng, sai kiểu, prefab/data không khớp và starting
skill chưa đăng ký trước khi trận bắt đầu. Bake kiểm tra candidate trước khi thay catalog đang có.
Nếu chuyển content ra ngoài các thư mục nguồn trên, phải cập nhật builder; không có fallback dùng tên.

`ContentCatalogHash` là SHA-256 của danh sách GUID + `AssetDatabase.GetAssetDependencyHash`, sắp theo
GUID. Hash bao gồm dependency presentation/import; thay visual hoặc build target cũng có thể gây reject.
Đây là kiểm tra bảo thủ cho MVP cùng nền tảng, không phải hash chỉ riêng dữ liệu cân bằng. Catalog được
bake lại trước build để tránh dùng hash cũ. Thay luật C# không thể hiện trong content phải tăng
`GameplayRulesVersion`.

## Wire contract và executor

Assembly `TurnBasedGame.Protocol` đặt `noEngineReferences = true`. `MatchCommandDto` không chứa Unity
object, delegate hoặc `TryExecute`. `ICommand` cũ chỉ còn phục vụ move/undo nội bộ.

Header command gồm `ProtocolVersion`, `GameplayRulesVersion`, `ContentCatalogHash`, `MatchId`,
`CommandId`, `Actor`, `ExpectedTurn`, `ClientSequence`, `AcknowledgedServerSequence`, `Kind`.

| Kind | Payload |
|---|---|
| `EndTurn` | Không có |
| `SpawnUnit` | `UnitContentId`, `SpawnPointId`; chuỗi rỗng nghĩa là authority chọn điểm khả dụng |
| `MoveUnit` | `UnitRuntimeId`, `Destination` |

`MatchProtocol` dùng binary little-endian, header cố định và string length có giới hạn. Command tối đa
`1024` byte; payload thiếu byte, thừa byte, enum/ID không hợp lệ và field không thuộc kind đều bị từ chối.
`ProtocolVersion = 1`, `GameplayRulesVersion = 1`. Thay wire shape không tương thích phải tăng version.

`LocalMatchAuthority.SubmitBytes(payload, authenticatedActor)` deserialize rồi qua `MatchCommandGate`.
Transport/session phải lấy `authenticatedActor` từ kết nối đã xác thực, không sao chép từ payload.
Gate kiểm tra compatibility và identity trước khi gọi executor; executor giải ID bằng registry rồi
kiểm tra turn/owner/map/path/MP và gọi manager đang sở hữu mutation.

Các entry point local `SubmitSpawn`, `SubmitMove`, `SubmitEndTurn` vẫn dùng được. Adapter tạo DTO,
chuyển reference thành ID; callback di chuyển chỉ truyền riêng trong lời gọi executor local. Callback
không được serialize và không được gọi lại khi replay command đã xử lý.

## Sequence, acknowledgement và replay

- `CommandId` duy nhất theo player trong một match, số dương. Hai player có thể dùng cùng số.
- `ClientSequence` bắt đầu từ 1, tăng liên tiếp riêng từng player. Envelope bị từ chối trước admission
  không tiêu thụ sequence. Command đã admission nhưng gameplay reject vẫn tiêu thụ sequence.
- `ServerSequence` bắt đầu từ 0, tăng chỉ khi executor chấp nhận command.
- `AcknowledgedServerSequence` là mốc kết quả host mà client đã nhận; không được vượt mốc host hiện tại.
- Retry phải gửi lại đúng payload, gồm cả acknowledgement cũ. Cùng ID và payload trả kết quả đã cache;
  cùng ID khác payload trả `ReplayConflict`. Không thực thi lại command bị reject sau admission.
- Gate chặn command gọi lồng từ mediator trong lúc executor đang chạy.

`CommandAcknowledgement` chứa match/player/command ID, client/server sequence, `Accepted`, `Reason`,
`Detail`; có codec riêng. Reason phân biệt protocol/rules/content mismatch, sai match/actor/sequence,
replay conflict, state/turn và gameplay reject.

Accepted hiện nghĩa là executor đã nhận action; với move, coroutine có thể còn đang chạy. Đây chưa
phải kết quả transaction chứa state changes sau toàn bộ animation. Exception của executor không được
coi là reject an toàn hay rollback; command ID đã được giữ để không retry mutation. Atomicity và
completion/result sau action phải được hoàn thiện ở giai đoạn 3.

## RNG authority

`LocalMatchAuthority.Random` sở hữu `MatchRandom` theo trận, thuật toán xorshift32 version 1. Seed khác
0 được tạo độc lập với `UnityEngine.Random`. `Capture()` trả `Seed`, `State`, `Sequence`; `Restore()`
khôi phục đúng phần tiếp theo của chuỗi. Int range dùng rejection sampling để tránh modulo bias.

Các nguồn gameplay đã chuyển: damage/effect của `MultiArrowSkill`, burn của `MagicianFireSealSkill`,
hazard của `TileHazardManager`, kết quả cuối xúc xắc. `TryRollDice` kiểm tra actor/turn trước khi lấy
RNG; lượt đổi trong lúc animation thì không roll hoặc cộng MP cho phe kế tiếp. RNG của số nhấp nháy
trên xúc xắc, floating text và loading hint vẫn là presentation RNG.

Skill/hazard hiện vẫn được gọi từ pipeline local. Tách host/client execution, dice MP/action transaction
và chuyển effect khỏi animation callback là phần giai đoạn 3; chỉ có RNG riêng chưa tạo ranh giới bảo mật.

## Compatibility prototype

Prototype NGO trao `MatchCompatibility` trong ping/ack trước done handshake, kiểm tra cả hai phía.
Peer không khớp bị dừng với log `REJECT: ProtocolMismatch`, `RulesMismatch`, `ContentMismatch` hoặc
`InvalidPayload`. Gate gameplay cũng kiểm tra compatibility trong từng envelope.

Không thay gameplay scene hoặc thêm NGO component vào scene local. Prototype chưa chuyển gameplay command.

## Kiểm chứng ngày 2026-09-10

- 15 test NUnit của `MatchProtocolTests` đạt trên .NET `9.0`: round-trip ba command, auto spawn point,
  payload lỗi, compatibility, replay, sequence, actor/match, reentrancy, acknowledgement Unicode và RNG restore.
- Compile runtime với source/reference của project: 0 lỗi; harness ngoài Unity có warning về field
  serialized/chưa dùng và reference framework `MSB3277` từ dependency hiện có.
- Compile `MatchContentCatalogBuilder` và `MatchRegistryTests`: 0 lỗi, 0 warning.
- 4 test `MatchRegistryTests` đã thêm cho Unity Editor: runtime binding/removal, spawn ID và duplicate,
  catalog mapping, content thiếu/trùng. Chưa chạy trong Unity.
- Chưa xác minh Unity Console sau import, asset catalog được bake, Play Mode local và handshake hai process.
  Không đánh dấu toàn bộ milestone đã hoàn thành từ kết quả compile.

Kiểm tra tiếp trong Editor: đợi import → rebuild catalog → chạy `MatchProtocolTests` và
`MatchRegistryTests` trong Test Runner → smoke `SpawnUnit`, `MoveUnit`, `EndTurn` → chạy lại prototype
hai process với cùng catalog và lần lượt thay version/hash để kiểm tra reject.
