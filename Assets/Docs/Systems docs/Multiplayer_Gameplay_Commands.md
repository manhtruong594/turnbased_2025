# Multiplayer gameplay command — Giai đoạn 3

Cập nhật: `2026-09-10`.

## Trạng thái và phạm vi

Đã triển khai command coverage, executor, kết quả có state, rollback state và adapter NGO. Compile runtime,
Editor và test không có lỗi; 50 test protocol/gate đạt trên Mono đi kèm Unity. Chưa nghiệm thu milestone:
5 test `MatchGameplayAuthorityTests` chưa chạy trong Unity; chưa kiểm chứng Play Mode và hai process gameplay.

`MatchGameplayTransport` cần session đang kết nối, `MatchId` của host và mapping peer/player do host xác nhận.
Update `2026-09-15`: Phase 4 đã thêm bootstrap trực tiếp, snapshot/replica và scene handshake khi có flag
`-mp-gameplay-role`; chưa nghiệm thu hai process. Update `2026-09-18`: session/authentication/Relay,
loadout và rematch đã nối qua [Session, phòng chờ và UX](Multiplayer_Session_Lobby.md), chờ nghiệm thu Unity/Internet.
Xem [Đồng bộ state và vào trận](Multiplayer_State_Sync.md) để build/chạy gameplay, không dùng prototype ping/ack.

## Command và validation

| Command | Payload riêng | Authority xử lý |
|---|---|---|
| `EndTurn` | Không có | Kiểm tra actor/turn/state, finish unit, capture/victory, tick đầu lượt kế tiếp |
| `SpawnUnit` | `UnitContentId`, `SpawnPointId` tùy chọn | Resolve catalog, MP, owner/availability của spawn point, tile |
| `MoveUnit` | `UnitRuntimeId`, `Destination` | Owner, alive/action/move/root, tile/path/range; commit grid trước animation |
| `NormalAttack` | `UnitRuntimeId`, `SkillContentId`, `Destination` | Chỉ skill Normal của chính source; target/range/line-of-sight/state |
| `UseSkill` | Như normal attack | Skill runtime thuộc source, không Passive, cooldown, MP, target và custom validation |
| `CastSpell` | `CardInstanceId`, `Destination` | Lá còn trong hand của actor, effect, target set do host chọn, MP, consume |
| `FinishUnit` | `UnitRuntimeId` | Owner, living/action state; đánh dấu hoàn tất action |
| `UndoMove` | `UnitRuntimeId` | Chỉ move vừa commit, chưa có command thành công khác, chưa action, ô cũ còn trống |
| `RollDice` | `DiceIndex` = 1 hoặc 2 | Mỗi die một lần/lượt; RNG và cộng MP trong cùng transaction |

Mọi command đi qua kiểm tra compatibility, match, authenticated actor, turn, sequence và replay của
`MatchCommandGate`. Không nhận cost, damage, cooldown hoặc danh sách target từ client. ID skill phải resolve
trong danh sách clone runtime của source, không lấy một skill tùy ý từ catalog.

Line-of-sight kiểm tra các tile dọc đoạn nối tâm source/target trên map; tile không tồn tại hoặc không
`Vacant` ở giữa chặn tầm nhìn. `canAttackThroughObstacles` bỏ qua phép kiểm tra này. Cần kiểm chứng riêng
các trường hợp sát góc/cạnh và loại grid sử dụng trong scene; đây không phải physics raycast qua mesh.

`UndoMove` khôi phục vị trí, owner của capture point đích, status của unit và RNG trước move. Không cho undo
sau command thành công khác vì có thể ghi đè hệ quả mới hơn. Button cũ gửi intent, không gọi undo state trực tiếp.

## Skill, spell và presentation

`SkillBase.Execute` gửi command. Executor gọi `ExecuteAuthorized`, áp effect đồng bộ một lần, đặt cooldown
và hoàn tất action. `ApplyEffect` vẫn tồn tại cho animation/VFX reference cũ nhưng không gây mutation.
MultiArrow giải toàn bộ số arrow theo data; FlameThrower giải số tick theo duration/tickInterval trong
transaction. Animation/projectile trình diễn sau khi đã tính kết quả; thời điểm HP thay đổi vì vậy sớm hơn
luồng animation-driven cũ. Cần kiểm chứng cảm giác animation/VFX trong Play Mode.

Hand giữ danh sách data phục vụ UI cũ và danh sách instance ID song song. Hai lá cùng data có ID khác nhau;
ID cấp tăng trong vòng đời manager, không tái sử dụng sau consume. Selection ghi nhớ ID đang xác nhận.
Cancel trước submit chỉ đóng lựa chọn vì chưa có mutation/reservation phía host. Sau submit, Confirm/Cancel
không phát thêm action cho đến khi có result hoặc disconnect.

`SingleAlly`, `SingleEnemy`, `Self`, `AnyUnit` chọn đúng một unit tại tile. `AllAllies`/`AllEnemies` chọn
toàn map khi `range = 0`, hoặc vùng quanh tile khi `range > 0`. Không có target hợp lệ thì reject trước
trừ MP/consume. Card là phép toàn bản đồ, không có unit caster làm gốc để tính cast distance.

`MatchCommandResult.Pending` phân biệt đang chờ mạng với reject. Spell/dice chờ callback; move không gọi
callback hoàn tất ngay khi gửi. Nhánh reject trả UI về khả năng chọn lại. Phần dựng presentation từ
Phase 4 nối apply/hash trước `ResultReceived` và callback hoàn tất; gap yêu cầu snapshot.

## Authority và vòng đời lượt

- `TurnManager` tick/reset unit trước event bắt đầu lượt. `PlayerController` và AI không tick unit lần nữa.
- `EndTurn` hoàn tất chuyển state đồng bộ; `TurnTransitionDelay` vẫn phục vụ khởi động lượt đầu.
- Capture và hazard nhận mediator event do mutation phía host tạo ra. Client không resolve capture/status/death/MP.
- Death gỡ unit khỏi map, runtime registry và roster ngay trong transaction; animation hủy object sau đó.
  Spawn point tại vị trí unit chết được giải phóng.
- Timer host dùng `ExecuteSystem`: server sequence vẫn tăng, nhưng không lấy `CommandId` hay client sequence
  của remote player. Result hệ thống dùng `CommandId = 0`; client không được gửi ID này.
- AI local được cấp MP qua `GrantLocalAIMana` một lần/lượt. AI không chạy khi có network session.
- Một match có transport client tiếp tục ở vai trò replica sau disconnect; không tự trở thành authority
  chỉ vì `NetworkManager.IsListening` đổi về false.

## Transaction và lỗi

Các reject thông thường xảy ra trước mutation. Gate cache cả accept và reject sau admission; gửi lại cùng
payload không thực thi hoặc gọi completion lần hai. Result được sao chép để caller không sửa cache.

Trước executor, authority chụp state phục hồi: MP, roster/runtime IDs, vị trí/HP/action flags của unit,
cooldown/status, hand/instance IDs, spawn/capture point, hazard, turn và RNG/dice. Nếu executor hoặc bước
thu result ném exception, rollback state rồi trả `ExecutionFault`; gate dừng nhận command mới. Unit tạo
dở được vô hiệu hóa/hủy; coroutine move/death mới được dừng. Không tự retry transaction lỗi.

Rollback phục hồi authoritative state, không hoàn tác animation, VFX, âm thanh hoặc UI event đã phát.
Sau `ExecutionFault` cần kết thúc hoặc phục hồi trận từ snapshot trước khi chơi tiếp; workflow phục hồi
snapshot/session chưa triển khai trong giai đoạn này. Cần test fault injection trong Unity trước nghiệm thu.

## Result và wire version

`ProtocolVersion = 2`, `GameplayRulesVersion = 2`; peer version 1 bị từ chối. Command vẫn tối đa 1024 byte.
Acknowledgement gồm ID, actor, client/server sequence, `NextClientSequence`, accepted/reason/detail,
`DiceValue` và `StateChanges`. Tối đa 2048 entry, decoder giới hạn 256 KiB. NGO dùng
`ReliableFragmentedSequenced` vì result lớn hơn một gói transport.

`StateChanges` hiện chứa **toàn bộ collection sau command**, cùng tombstone `RemovedUnit`, chưa phải ordered
delta tối ưu. Collector giai đoạn 4 phải thay collection status/cooldown/hand/hazard khi apply; không append
mọi entry vào state cũ. Trường rỗng/mất khỏi collection nghĩa là đã được gỡ. Không dùng result này thay initial
snapshot hoàn chỉnh: còn cần bootstrap, dữ liệu private khác, deadline và state hash.

| Kind | Cách đọc |
|---|---|
| `MP` | Player; Value = MP |
| `Unit` | Entity/runtime ID, ContentId, Player, Position; Value = HP, Value2 = move done, Value3 = action committed |
| `RemovedUnit` | Entity/runtime ID cần gỡ |
| `Cooldown` | Entity/unit, ContentId/skill, Value = cooldown |
| `Status` | Entity/unit, Player/source; Value = type, Value2 = value, Value3 = remaining turns, Value4 = initial duration |
| `HandCard` | Player/owner, Entity/instance ID, ContentId/spell |
| `Capture` | Position và Player; 0 nghĩa là neutral |
| `Turn` | Player/current; Value = count, Value2 = TurnState, Value3 = winner hoặc 0 |
| `Random` | Entity = RNG sequence; Value/Value2 giữ bit uint seed/state |
| `Hazard` | Position, Player; Value = type, Value2 = damage, Value3 = remaining, Value4 = status duration, Scalar = chance |
| `SpawnPoint` | ContentId = SpawnPointId, Player, Value = available |
| `Dice` | Value = turn đã roll, Value2 = bitmask die đã dùng |

## Nối session với NGO

Sau khi session xác nhận vai trò, tạo `MatchGameplayTransport(networkManager, localPlayer, hostMatchId)`.
Constructor gắn adapter vào `LocalMatchAuthority`; host gọi `BindAuthenticatedPeer(clientId, player)` từ
mapping kết nối được session xác thực, không lấy actor từ payload. Host phải khởi tạo match trước khi nhận
command; client phải có catalog và replica IDs/turn/hand từ bootstrap trước khi tương tác.

Host broadcast mỗi result đã commit; reply cho request/replay gồm cả reject. `ResultReceived` chỉ phát cho
accepted result có server sequence mới hơn. Hand của đối phương được lọc khỏi payload trước gửi. Client chỉ
nhận result từ NGO server; callback của request chỉ chạy khi match/actor/command ID khớp. Disconnect giải
phóng request đang chờ. Session gọi `Dispose`; adapter tự detach khỏi `LocalMatchAuthority` khi rời match.

Phase 4 đã thêm snapshot apply, gap/resync và timeout qua `MatchGameplayBootstrap`; result được bọc trong
snapshot envelope, lọc cả RNG và hand đối phương. `ResultReceived` chỉ phát sau khi apply/hash thành công.
`Unit.Value4` chứa quyền UndoMove; `Turn.ContentId` chứa deadline invariant và `Turn.Value4` chứa end reason.
Session dịch vụ đã có implementation Phase 5; reconnect lifecycle vẫn thuộc Phase 6. Xem tài liệu
Phase 4–5 để biết giới hạn và kiểm chứng còn thiếu. Trong session, spawn còn bị giới hạn bởi loadout đã chốt.

## Kiểm chứng

- 50/50 NUnit case `MatchProtocolTests` đạt qua runner Mono ngoài Editor: round-trip 9 command, payload lỗi,
  replay, rejection, state result, RNG, rollback callback và timeout không tiêu thụ sequence của player.
- Compile bằng Roslyn/response/reference của Unity `6000.3.9f1`: protocol, runtime, Editor và test đều 0 lỗi.
  Hai warning field chưa dùng có sẵn trong `ObjectPoolUsageExample` và `GameplayTestTool`.
- 5 test `MatchGameplayAuthorityTests` đã compile, **chưa chạy**: owner attack, stale turn, card của đối phương,
  spell target không tồn tại, dice/MP/replay. Fixture tách singleton và bỏ qua khi đang Play Mode/network session.
- Chưa xác minh Console sau import, missing reference, Play Mode, hai process và rollback visual.

Trước nghiệm thu: chạy EditMode tests; smoke spawn/move/undo/attack từng skill/spell; thử invalid target,
cost/cooldown/owner, replay, target chết trong multi-hit, capture/victory, tick status đúng một lần;
fault injection giữa effect; rồi nối session/bootstrap và kiểm tra hai process nhận cùng kết quả.
