# Kế hoạch hoàn thiện multiplayer

Cập nhật: `2026-09-07`

## 1. Mục tiêu và phạm vi MVP

Mục tiêu đầu tiên là trận đấu `1v1` đồng bộ qua mạng, dùng mô hình host-authoritative:

- Một người tạo phòng và làm host; người còn lại tham gia bằng mã phòng hoặc lời mời.
- Client chỉ gửi ý định hành động. Host xác thực, thay đổi gameplay state và phát kết quả.
- Hai máy chơi trọn một trận: vào phòng → sẵn sàng → tải trận → spawn → move → attack/skill/spell
  → capture/endgame → rematch hoặc rời phòng.
- Có timeout lượt, xử lý mất kết nối và reconnect trong một khoảng thời gian đã chốt.

Ngoài phạm vi MVP: dedicated server, public matchmaking, ranked/MMR, spectator, hơn hai người chơi,
host migration và anti-cheat cấp competitive. Các mục này chỉ bắt đầu sau khi MVP ổn định.

## 2. Trạng thái nền tảng hiện tại

| Hạng mục | Trạng thái | Bằng chứng/giới hạn |
|---|---|---|
| Command authority local | Một phần | `LocalMatchAuthority` nhận `EndTurn`, `SpawnUnit`, `MoveUnit` |
| Validation lượt | Hiện có, cần Play Mode test | Kiểm tra `CommandId`, `Actor`, `ExpectedTurn` và command trùng |
| Network-safe command DTO | Chưa có | Command còn giữ `UnityEngine.Object` và callback local |
| Runtime ID ổn định | Chưa có | Chưa có ID dùng chung để tham chiếu unit/spawn point/content qua mạng |
| State snapshot/delta | Chưa có | Chưa thể dựng hoặc phục hồi toàn bộ trận từ dữ liệu mạng |
| Transport/session/relay | Đã chốt, prototype đạt | NGO `2.13.2`, UTP `2.7.4`, Multiplayer Services `2.3.1`; chưa kiểm chứng Relay Internet |
| Lobby/reconnect | Chưa có | Chưa có lifecycle phiên mạng |
| Multiplayer test | Prototype transport đạt | Hai process loopback trao đổi message và disconnect sạch; chưa đồng bộ gameplay |

`LocalMatchAuthority` là seam tạm thời để gom mutation. Nó chưa phải server và không được xem là ranh
giới bảo mật cho tới khi authority chạy trên host/server, nhận DTO không chứa reference local.

## 3. Kiến trúc mục tiêu

```text
UI/Input/AI client
        │ intent
        ▼
Network-safe MatchCommand DTO
        │
        ▼
Host MatchAuthority ── validate ──> Gameplay managers
        │                              │
        └──── result/state delta <─────┘
                       │
                       ▼
                 Client presentation
```

Nguyên tắc:

1. Host là nguồn sự thật cho turn, timer, MP, RNG, unit, card, capture point và winner.
2. Client không tự trừ MP, gây damage, spawn, đổi lượt hoặc quyết định kết quả RNG.
3. Command chỉ chứa dữ liệu có thể serialize: ID, enum, số, grid coordinate và payload versioned.
4. Gameplay state không phụ thuộc `NetworkObject` để vẫn chạy local và test EditMode được.
5. Mọi kết quả phải có thể dựng lại từ snapshot; animation/VFX chỉ phản chiếu kết quả đã xác nhận.

## 4. Các giai đoạn triển khai

### Giai đoạn 0 — Chứng minh nền tảng local

Thực hiện:

1. Chạy Play Mode smoke test cho `SpawnUnit`, `MoveUnit`, `EndTurn` qua `LocalMatchAuthority`.
2. Kiểm tra command sai phe, sai turn, trùng `CommandId`, tile bị chiếm và không đủ MP đều bị từ chối
   mà không đổi state.
3. Bổ sung EditMode test cho validation thuần và PlayMode test cho manager/coroutine.
4. Xác nhận timer, player và AI không gây double end-turn.

Tiêu chí hoàn thành:

- Luồng local chơi được như trước, không exception từ code dự án.
- Command bị từ chối không làm đổi MP, position, roster hoặc turn.
- Một lượt chỉ schedule chuyển lượt một lần.

### Giai đoạn 1 — Chốt quyết định mạng — Hoàn thành `2026-09-07`

ADR: [ADR-001: Network stack và mô hình phiên](ADR-001_NETWORK_STACK_AND_SESSION.md).

Đã thực hiện:

1. Chốt NGO `2.13.2` + UTP `2.7.4`; pin Multiplayer Services `2.3.1` cho Sessions/Relay.
2. Chốt listen server host-authoritative, Windows x86-64, đúng hai người chơi, Relay `dtls` cho Internet.
3. Chốt gameplay event-driven, NGO tick `30 Hz`, payload/version/build compatibility trong ADR.
4. Chốt reconnect client `30 giây`; host mất kết nối thì kết thúc trận; không host migration trong MVP.
5. Tạo `MultiplayerPrototype.unity` độc lập, không thay đổi gameplay scene.
6. Build Windows Development và chạy host/client loopback trên port `27982`: message round-trip đạt, client
   disconnect sạch, hai process tự thoát, không exception trong lần chạy đạt.

Giới hạn kiểm chứng: chưa chạy Relay qua hai mạng khác nhau vì cần cấu hình Unity project ID,
environment và Authentication; hạng mục này thuộc giai đoạn 5.

### Giai đoạn 2 — Tạo protocol và ID ổn định

Thực hiện:

1. Tạo các ID runtime/content tối thiểu: `MatchId`, `PlayerId`, `UnitRuntimeId`, `SpawnPointId`,
   `UnitContentId`, `SkillContentId`, `SpellContentId` và grid coordinate.
2. Tách `IMatchCommand` hiện tại thành DTO serialize được và executor phía authority. Loại
   `UnitController`, `SpawnPoint`, `GameObject`, `Transform` và callback khỏi payload mạng.
3. Thêm `ProtocolVersion`, `CommandId`, `Actor`, `ExpectedTurn` và sequence/acknowledgement.
4. Tạo registry ánh xạ content ID tới `ScriptableObject`; phát hiện ID thiếu hoặc trùng trước trận.
5. Đưa RNG ảnh hưởng gameplay về authority và lưu seed/state cần thiết.

Tiêu chí hoàn thành:

- Serialize → deserialize mọi command mà không mất dữ liệu.
- Cùng một ID ánh xạ đúng content/runtime entity trên host và client.
- Payload không chứa Unity object reference hoặc logic presentation.
- Build có content/protocol không tương thích bị từ chối với lý do rõ ràng.

### Giai đoạn 3 — Bao phủ toàn bộ gameplay command

Thực hiện theo thứ tự:

1. Hoàn thiện `EndTurn`, `SpawnUnit`, `MoveUnit` trên authority mạng.
2. Thêm normal attack và active skill: source, target/tile, MP cost, cooldown, range và line-of-sight.
3. Thêm spell card: card instance trong hand, target, confirm/cancel, consume và MP transaction.
4. Đưa capture resolution, death/removal, status tick và victory check về authority.
5. Chuẩn hóa `CommandResult`: accepted/rejected, reason code, server sequence và state changes.
6. Bảo đảm mỗi command là atomic: thất bại không trừ MP, consume card hoặc thay đổi một phần state.

Tiêu chí hoàn thành:

- Không còn entry point gameplay production thay đổi authoritative state ngoài authority.
- Host từ chối owner/turn/target/range/cost/cooldown/state không hợp lệ.
- Gửi lặp cùng `CommandId` không thực thi action lần hai.
- UI chỉ phát action hoàn tất sau kết quả host; reject khôi phục trạng thái tương tác đúng.

### Giai đoạn 4 — Đồng bộ state và vào trận

Snapshot tối thiểu phải chứa:

- Match phase, turn state/count, current player và authoritative deadline.
- MP của hai phe.
- Unit: runtime/content ID, owner, grid position, HP, action flags, cooldown và status effect/duration.
- Spawn point/capture point state.
- Deck/hand/discard theo quyền quan sát; dữ liệu bài bí mật chỉ gửi cho chủ sở hữu.
- RNG state/sequence cần thiết và winner/end reason.

Thực hiện:

1. Tạo initial snapshot sau khi hai client load scene và báo ready.
2. Phát ordered state delta sau command; dùng snapshot để bootstrap và phục hồi lệch state.
3. Thêm state hash/checksum ở mốc cuối command hoặc cuối lượt để phát hiện desync.
4. Dùng server sequence để bỏ delta cũ/trùng và yêu cầu snapshot khi thiếu sequence.
5. Đồng bộ scene load bằng handshake; không bắt đầu turn trước khi cả hai client ready.

Tiêu chí hoàn thành:

- Client vào trận từ initial snapshot có state giống host.
- Client reconnect dựng lại trận từ snapshot mà không cần chạy lại animation cũ.
- Mô phỏng mất/trùng/đảo thứ tự message không gây thực thi action hai lần.
- State hash khớp sau mỗi lượt trong test tự động.

### Giai đoạn 5 — Session, phòng chờ và UX

Thực hiện:

1. Khởi tạo/đăng nhập dịch vụ người chơi và ánh xạ service player ID sang `PlayerId` trong trận.
2. Tạo/join/leave session bằng mã; hiển thị trạng thái kết nối và lỗi có thể hành động.
3. Đồng bộ lựa chọn deck/loadout; host kiểm tra content hợp lệ trước ready.
4. Ready check, chọn phe/starting player theo authority và chuyển scene đồng bộ.
5. Kết thúc trận, rematch, rời phòng và dọn session/network object/callback khi về menu.

Tiêu chí hoàn thành:

- Hai máy ở hai mạng khác nhau vào cùng trận bằng mã phòng.
- Không thể bắt đầu nếu thiếu người, chưa ready hoặc loadout không hợp lệ.
- Rời trận và rematch không để lại callback trùng, session rác hoặc state trận trước.

### Giai đoạn 6 — Timer, disconnect và reconnect

Thực hiện:

1. Timer dựa trên clock/deadline của host; client chỉ hiển thị giá trị ước lượng.
2. Chốt timeout khi người chơi không gửi action và quy tắc auto end-turn/forfeit.
3. Phân biệt mất mạng tạm thời, chủ động rời, kick, host mất kết nối và service lỗi.
4. Giữ slot reconnect trong khoảng thời gian đã chốt; xác thực lại player trước khi gửi snapshot.
5. Với MVP, host rời trận thì kết thúc trận có lý do rõ ràng; host migration để milestone sau.

Tiêu chí hoàn thành:

- Đổi clock máy client không làm thay đổi thời hạn lượt thực tế.
- Client mất mạng rồi reconnect nhận đúng turn/state và không lặp reward/action.
- Host/client disconnect ở lobby, loading, trong lượt và endgame đều thoát flow sạch.

### Giai đoạn 7 — Độ bền và an toàn

Thực hiện:

1. Giới hạn kích thước/tần suất command; từ chối enum, ID, coordinate và payload bất hợp lệ.
2. Không tin owner, cost, damage, RNG hoặc target list do client gửi.
3. Thêm structured log theo `MatchId`, player, command ID, turn và reject reason; không log token.
4. Có timeout/cancellation cho connect, scene load, command acknowledgement và reconnect.
5. Version save/reward result; chỉ authority ghi nhận kết quả trận một lần.

Tiêu chí hoàn thành:

- Fuzz payload cơ bản không gây exception, treo lượt hoặc state mutation trái phép.
- Spam/replay command không tạo thêm unit, damage, MP, card hoặc reward.
- Log đủ để truy vết một trận desync/reject mà không chứa credential.

### Giai đoạn 8 — Kiểm thử và phát hành MVP

Ma trận kiểm thử tối thiểu:

| Nhóm | Trường hợp bắt buộc |
|---|---|
| Luồng trận | Create/join, ready, đủ loại action, endgame, rematch, leave |
| Validation | Sai lượt/owner/target/cost/range/cooldown, command trùng/cũ |
| Mạng | Latency, jitter, packet loss, message trùng/đảo thứ tự, reconnect |
| Lifecycle | Client/host thoát ở lobby, loading, player turn, AI turn và result |
| Dữ liệu | Content mismatch, snapshot đầy đủ, private card visibility, state hash |
| Thiết bị | Hai process local và hai thiết bị thật trên nền tảng phát hành |

Tiêu chí phát hành MVP:

- Ít nhất 20 trận tự động và 10 trận hai thiết bị hoàn tất không soft-lock/desync.
- Không double action, double end-turn, double reward hoặc lộ bài riêng của đối thủ.
- Reconnect thành công trong cửa sổ hỗ trợ; ngoài cửa sổ trả kết quả nhất quán.
- Unity Console không có exception từ code dự án trong luồng chuẩn.
- Có version compatibility, telemetry tối thiểu và hướng dẫn xử lý lỗi kết nối cho người chơi.

## 5. Thứ tự triển khai khuyến nghị

| Ưu tiên | Deliverable | Phụ thuộc |
|---|---|---|
| P0 | Play Mode/test cho authority local | Nền gameplay local ổn định |
| P0 | ADR + prototype hai process | Quyết định host/transport/service |
| P0 | Runtime/content ID + command DTO | Prototype transport |
| P0 | Network `SpawnUnit` → `MoveUnit` → `EndTurn` | DTO và authority |
| P1 | Attack/skill/spell/capture/endgame | Command nền ổn định |
| P1 | Snapshot/delta/checksum | ID và command coverage |
| P1 | Session/ready/scene load | Kết nối và snapshot |
| P1 | Disconnect/reconnect/timer | Session lifecycle |
| P2 | Hardening, test matrix, telemetry | End-to-end flow |
| Sau MVP | Public matchmaking, ranked, dedicated server, host migration | Dữ liệu vận hành MVP |

## 6. Điểm dừng của từng pull request

Mỗi pull request chỉ nên tạo một lát cắt chạy được, ví dụ:

1. Stable unit/content ID và test uniqueness.
2. DTO cho một command và round-trip serialization test.
3. Hai process đồng bộ duy nhất `EndTurn`.
4. Hai process đồng bộ `SpawnUnit` với reject không đủ MP.
5. Initial snapshot dựng unit và MP.

Không gộp lobby, toàn bộ gameplay command, reconnect và matchmaking vào cùng một thay đổi. Một mục
chỉ chuyển sang hoàn thành khi có implementation, test tương ứng và kết quả Play Mode/hai process.
