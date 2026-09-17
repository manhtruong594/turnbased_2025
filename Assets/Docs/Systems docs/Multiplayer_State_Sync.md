# Multiplayer — Phase 4: Đồng bộ state và vào trận

Cập nhật: `2026-09-15`.

## Phạm vi và trạng thái

Đã thêm implementation snapshot, replica applier, scene handshake, ordered replacement state, hash và resync.
Chưa nghiệm thu milestone: chưa chạy Unity EditMode/Play Mode và hai process gameplay.
Dùng lại collector/manager hiện có; không thêm lobby, Relay, sparse delta hoặc cơ chế rút bài mới.

- Host cố định Player1, client Player2; kết nối trực tiếp loopback/LAN chỉ dành cho development.
- Hai process dùng cùng build, scene/map và loadout đã serialize. Host dùng `SelectedSpells` của
  `PlayerController` cho cả hai phe; không nhận loadout tùy ý từ client. Chọn loadout/session là Phase 5.
- Không Authentication, Relay, giữ slot reconnect, host migration hoặc rematch. Khôi phục replica trong
  kết nối đang sống không đồng nghĩa reconnect lifecycle đã hoàn thành.
- Không có flag `-mp-gameplay-role`: luồng local vẫn được giữ; cần smoke test để xác nhận không regression.

## Luồng vào trận

`MatchGameplayBootstrap` được tạo trước scene khi có flag `-mp-gameplay-role host|client`.
Vai trò được giữ cả sau disconnect; client không tự chuyển thành authority.

1. Mở `HUDScene` (hoặc `-mp-gameplay-scene <scene-name>`), tạo NGO/UTP, tắt NGO scene management.
2. Chờ `GameMediator.IsInitialized` và các callback `Start` hoàn tất, gồm hand/UI setup.
3. Client gửi ready cùng compatibility và scene fingerprint. Fingerprint gồm scene path, grid configuration,
   tile data/vacancy và spawn/capture point. Không hash Unity instance ID.
4. Host bind peer Player2, gửi snapshot Initialization. Client validate/apply, trả sequence/hash.
5. Host bắt đầu lượt đầu qua system transaction đúng một lần (không chiếm player command ID/sequence),
   gửi snapshot mới. Client áp dụng và xác nhận; host gửi ready cuối cùng rồi mở input.

Loading/bootstrap/resync timeout `30s`. Peer dư hoặc compatibility/scene mismatch bị từ chối.
Không dùng `TurnTransitionDelay` để đoán client đã load xong.

## Contract state

`MatchSnapshotProtocol.Version = 1`; command protocol/rules vẫn `2`.
Snapshot/result-state có compatibility, MatchId, scene fingerprint, server sequence, `NextClientSequence`,
`NextCommandId`, authoritative deadline và SHA-256 của state theo quyền quan sát.
Giới hạn `2048` entry và `256 KiB` cho toàn envelope.

`StateChanges` là toàn bộ collection sau commit, không phải sparse delta. Dùng lại
`LocalMatchAuthority.CaptureState`; client thay collection, kể cả rỗng. Unit vắng mặt bị gỡ khỏi
runtime registry, roster và grid; không chạy death animation/damage lại.

- Unit: ID/content/owner/grid/HP, move/action flags, `Value4` = UndoMove khả dụng tại sequence đó.
- Cooldown, status (gồm initial/remaining duration), hazard, MP, spawn/capture và hand instance ID.
- Turn: current/count/state/winner; `ContentId` = deadline round-trip invariant, `Value4 = 1` khi kết thúc
  bởi capture victory (điều kiện kết thúc gameplay hiện có), `0` khi chưa kết thúc.
- Dice: turn và bitmask `1 << DiceIndex` (`2`, `4`, hoặc `6`). UI không mở lại die đã dùng khi resync.
- Chỉ gửi hand của viewer. RNG seed/state và hand đối phương không ra wire; RNG vẫn ở authority.
- Runtime chưa có deck/discard riêng: không tạo cơ chế mới để lấp trường tài liệu.

Applier validate cấu trúc, duplicate, IDs/content, ownership và collection bắt buộc trước mutation.
Các API replica không gọi executor, damage, tick status/capture hoặc start-turn gameplay event.
Hash được tính lại từ state runtime sau apply, theo thứ tự canonical; loại animation, UI và timer đếm lùi.
Deadline tuyệt đối được hash; timer client chỉ hiển thị `max(0, deadline - NGO ServerTime)`.

## Ordering và phục hồi

- `sequence <= applied`: không apply lần nữa; ACK đúng request vẫn có thể hoàn tất callback.
- `sequence == applied + 1`: apply, kiểm tra hash rồi mới advance applied cursor và hoàn tất callback.
- Gap/apply mismatch: khóa input và yêu cầu snapshot; không acknowledge state chưa apply.
- Snapshot khôi phục cả sequence và command ID counter. Pending request được gửi lại **đúng bytes cũ**
  sau ready để lấy kết quả từ replay cache; không đổi CommandId để thử lại action.
- Pending ACK quá `10s`: yêu cầu resync một lần; quá `30s`: đóng kết nối, báo lỗi và giữ input khóa.
- Snapshot apply thất bại: thử snapshot một lần nữa; lỗi lặp lại kết thúc kết nối, không tiếp tục state dở dang.
- `MatchGameplayBootstrap.Instance.RequestSnapshot()` là entry point yêu cầu resync client khi test.
- Unload gameplay scene dọn transport/callback. Rematch chưa hỗ trợ; bắt đầu hai process mới.

Replica resync không sửa được authority đã `ExecutionFault`; faulted gate của Phase 3 vẫn không nhận
command mới. Kết thúc phiên và điều tra lỗi, không tự mở lại gate.

## Build và test thủ công

Không dùng executable `MultiplayerPrototype`: nó chỉ test ping/ack, không có gameplay.

Có thể dùng `Tools > Multiplayer > Gameplay Test Runner` để build, chạy/dừng Host và Client,
đặt IP/port, trì hoãn Client và theo dõi các dòng log multiplayer. `Start Both` tạo log riêng cho
mỗi phiên tại `Logs/MultiplayerGameplay/<yyyyMMdd-HHmmss>`. Tool chỉ điều phối hai process; các ca
duplicate/gap/reorder/hash mismatch vẫn cần Unity Test hoặc runtime debug hook riêng.

1. Chờ Unity import/compile sạch. Kiểm tra catalog, scene `HUDScene`, prefab/reference và loadout.
2. Chạy EditMode: `MatchProtocolTests`, `MatchSnapshotTests`, `MatchRegistryTests`, `MatchGameplayAuthorityTests`.
3. Khi sẵn sàng build, tự chọn `Tools > Multiplayer > Build Windows Gameplay Test`.
   Menu build `HUDScene` trước, giữ các scene enabled khác; không ghi lại Build Settings.
4. Mở hai terminal, chạy executable từ cùng build:

```powershell
& '.\Builds\MultiplayerGameplay\MultiplayerGameplay.exe' -mp-gameplay-role host -mp-port 27982 -logFile host-gameplay.log
```

```powershell
& '.\Builds\MultiplayerGameplay\MultiplayerGameplay.exe' -mp-gameplay-role client -mp-address 127.0.0.1 -mp-port 27982 -logFile client-gameplay.log
```

LAN: client thay `127.0.0.1` bằng IP host; chỉ dùng mạng tin cậy và cấu hình firewall phù hợp.
Không thêm `-nographics` khi test UI. Chờ cả hai log có `[MP-GAMEPLAY] READY sequence=1`.

| Test | Kết quả cần xác nhận |
|---|---|
| Client load chậm | Host không bắt đầu lượt trước xác nhận snapshot |
| 9 command hợp lệ | HP/MP/grid/hand/status/cooldown/capture/turn khớp, không thực thi hai lần |
| Reject sai owner/turn/target/cost | Không mutation; UI có thể thao tác lại |
| Roll hai die, UndoMove | Die đã dùng không mở lại; undo chỉ command move gần nhất |
| Skill/spell/death | Không tick hoặc gây damage lại khi restore; unit chết không còn trên grid |
| Restore snapshot nhiều lần | Không thêm unit/card trùng, collection rỗng thật sự được xóa |
| Gap/duplicate/reorder ở lớp message | Duplicate không apply; gap khóa và resync; callback sau apply |
| State mismatch | Hash không khớp dẫn tới resync hoặc lỗi rõ ràng, không tiếp tục âm thầm |
| End turn/endgame | Hash runtime khớp, timer chỉ host quyết định, UI đúng phe thắng |
| Disconnect/unload | Input khóa, pending callback giải phóng, không fallback sang local authority |
| Không có flag multiplayer | Chơi local bình thường; AI và timer không double action |

## Bằng chứng hiện có

- `61/61` NUnit case protocol/snapshot/gate đạt qua runner Mono ngoài Editor, gồm `11` case snapshot mới.
- Runtime, Editor và protocol test compile sơ bộ bằng Roslyn/tham chiếu Unity; không dùng full player build.
- Thêm `3` test replica vào `MatchGameplayAuthorityTests`: apply lặp không phát start-turn event,
  hand rỗng thay hand cũ, state thiếu bị từ chối trước mutation MP. Chưa chạy chúng trong Unity.
- Test `TwentyTurnReplacementSimulationKeepsViewerHashAfterGapsAndDuplicates` là mô phỏng protocol,
  **không phải** 20 lượt gameplay Unity hay 20 trận hoàn chỉnh.
- Chưa có bằng chứng Play Mode, hai process, fault injection gameplay hoặc kiểm tra trực quan UI/VFX.
  Không đánh dấu Phase 4 hoàn thành trước các kiểm chứng đó.
