# Multiplayer — Giai đoạn 5: Session, phòng chờ và UX

Cập nhật: `2026-09-18`.

## Trạng thái

Đã triển khai luồng session vào gameplay. Chưa nghiệm thu giai đoạn 5: chưa chạy Unity Play Mode,
hai process session hoặc hai thiết bị qua Internet. Compile ngoài Editor không chứng minh Relay,
scene lifecycle hoặc giao diện hoạt động đúng.

Giữ NGO `2.13.2`, UTP `2.7.4`, Multiplayer Services `2.3.1`. Không thay package, ProjectSettings,
scene hoặc prefab. Unity cần import các script mới và tự tạo `.meta`.

## Sử dụng

1. Mở menu, vào Prepare Battle, chọn unit/phép và map như luồng local.
2. Chọn **Multiplayer • Phòng riêng**. Nút được thêm bằng UI Toolkit tại `PrepareBattleScreen`;
   phòng chờ dùng `PanelSettings` của màn hình, tồn tại qua scene transition.
3. Host chọn **Tạo phòng**, sao chép mã. Khách nhập mã rồi chọn **Tham gia**.
4. Phòng hiển thị unit, số phép, phe và ready của hai người. Chọn **Sẵn sàng**;
   host chỉ có thể bắt đầu khi cả hai ready và loadout hợp lệ.
5. Host chọn **Bắt đầu trận**. Cả hai tải scene/map của host; input chỉ mở sau handshake snapshot.
6. Endgame: cả hai chọn **Tái đấu** (hoặc nút restart trong HUD), rồi host bắt đầu trận tiếp.
   Ready của trận cũ không được dùng lại. Player1 đi trước trận lẻ, Player2 đi trước trận chẵn.
7. **Rời phòng** có trong overlay khi loading/gameplay và trong endgame. Trở về scene menu nơi mở phòng.

Loadout được sao chép lúc mở phòng; muốn đổi deck, đóng/rời phòng rồi sửa tại Prepare Battle.
Map dùng tên duy nhất trong `Resources/BattleMapCatalog`; host chọn map, client phải có cùng map.
Nếu không chọn map, dùng map mặc định đã serialize trong scene. Fingerprint Phase 4 kiểm tra dữ liệu map
thực tế trước khi mở lượt. Không dùng index trong catalog để nhận diện map.

## Điều kiện dịch vụ và build

- Unity project đã có Cloud Project ID; vẫn cần xác nhận Authentication, Lobby/Sessions và Relay
  được bật, cấu hình đúng và truy cập được từ hai thiết bị. Code dùng environment mặc định của Unity Services.
- Menu và scene trận phải nằm trong build. Không sửa Build Settings tự động.
- Hai máy dùng cùng `Application.version`, protocol/rules version và content catalog hash.
- Không dùng `-mp-gameplay-role` khi thử session: flag đó vẫn dành cho LAN development Phase 4.
- Khi thử hai process cùng máy, cần hai danh tính Authentication riêng, chẳng hạn hai tài khoản OS
  có vùng lưu Authentication riêng. Không dùng chung anonymous player ID cho cả hai slot.
- Authentication dùng `InitializeAsync` và anonymous sign-in khi chưa đăng nhập; giữ danh tính đã đăng nhập.
  Rời phòng không sign-out/xóa tài khoản người chơi.

API được đối chiếu trực tiếp với source SDK đã cài. Luồng Sessions phối hợp Relay/NGO theo
[Session networking](https://docs.unity.com/en-us/mps-sdk/session-networking) và
[Create a session](https://docs.unity.com/en-us/mps-sdk/create-session).

## Quyền quyết định và dữ liệu

`MatchSessionController` sở hữu session, NGO manager, callback dịch vụ và vòng đời kết nối.
Session private có đúng hai slot; tạo bằng `WithRelayNetwork()` và `RelayProtocol.DTLS`.

- Host service ID → `Player1`; thành viên còn lại → `Player2`.
- Mỗi người publish loadout content IDs, compatibility và ý định ready theo round.
- NGO approval kiểm tra service membership bằng refresh, compatibility và proof ngẫu nhiên gắn với
  player property chỉ thành viên đọc được. Không dùng actor do command gửi lên để bind phe.
- Host chỉ bind đúng NGO peer đã qua approval; peer phải tương ứng service guest của lần launch.
- Proof chỉ dành cho slot của session, không phải access token UGS; không ghi proof vào log/error.
  Đây là mô hình hai người tin host, không phải backend xác minh quyền sở hữu vật phẩm hay anti-cheat competitive.
- Loadout yêu cầu 1–6 unit, tối đa 4 phép, ID đúng kiểu, có trong content registry và không trùng.
  Quy tắc dùng giới hạn của `PlayerDataSO`; host kiểm tra lại trước khi ghi launch.
- Host khóa phòng, chốt bản sao hai loadout/map/scene/round trong property `launch`.
  Client không tự chọn phe hoặc tự bắt đầu lượt. Host chỉ tải trận sau khi lưu launch thành công.
- Authority từ chối spawn unit ngoài loadout đã chốt. `PlayerController` lấy deck và phép đúng phe;
  không dùng spell của host để khởi tạo hand khách nữa.
- Loadout được công khai giữa hai thành viên trong lobby; hand runtime và RNG vẫn theo quyền quan sát Phase 4.

## Scene, tái đấu và cleanup

`MatchGameplayBootstrap.BeginSession` nhận NGO đang kết nối; không tạo kết nối LAN thứ hai.
Bootstrap đợi scene mới load và manager `Start` hoàn tất, rồi dùng handshake/snapshot Phase 4.
Tên control/snapshot message có hậu tố round để không nhận handshake của trận trước.

Tái đấu giữ session/Relay, thay bootstrap và scene gameplay. Dispose transport gỡ named handler,
disconnect callback và `CommandCommitted`, giải phóng pending request. Scene reload tạo manager mới;
host reset MatchId/RNG/gate/sequence, client tạo registry/replica mới. Pool gameplay được clear khi
chuyển khỏi trận để không giữ object đã thuộc scene cũ. Ready phải khớp round kế tiếp.

Rời trong loading đợi scene operation đang chạy kết thúc rồi dọn kết nối và về menu, tránh hai lần
load scene chồng nhau. Input đóng ngay khi bắt đầu rời. Cleanup gỡ callback session/bootstrap/transport,
host delete session hoặc khách leave, shutdown/destroy NGO, unload gameplay rồi mới trả `MatchContext`
về local và xóa runtime state của authority.

Nếu dịch vụ không xác nhận delete/leave, giữ handle cleanup và hiện hướng dẫn **Rời phòng** để thử lại;
không cho tạo thêm session trong lúc cleanup còn thất bại. Host rời hoặc mất kết nối trong trận sẽ khóa
gameplay và báo lý do. Không host migration, không giữ slot/reconnect 30 giây ở giai đoạn này.
Timeout handshake/snapshot vẫn là 30 giây; timeout tác vụ dịch vụ hiện theo SDK.

## Kiểm chứng

Đã chạy:

- `77/77` NUnit case protocol/gate/snapshot/lobby qua runner Mono ngoài Unity.
- `16` case `MatchLobbyRulesTests`: loadout rỗng/null/sai loại/trùng/quá giới hạn, thiếu người,
  thiếu ready, mismatch và ready cũ không mở rematch.
- Roslyn compile protocol, runtime, Editor và protocol test với reference Unity: không có lỗi.
- `git diff --check` cho phạm vi sửa: đạt.

Chưa chạy Unity Console/EditMode/Play Mode, kiểm tra trực quan UI hoặc Relay thật. Ma trận nghiệm thu còn lại:

| Ca kiểm tra | Kết quả cần đạt |
|---|---|
| Hai máy, hai mạng khác nhau | Create/join bằng mã, handshake và chơi trận thành công |
| Sai mã/phòng đầy/khác build hoặc content | Báo lý do, không vào gameplay, không còn NGO dư sau cleanup |
| Thiếu người/thiếu ready/loadout sai | Nút start khóa hoặc host từ chối, không đổi scene |
| Hai loadout khác nhau | Spawn list/hand đúng phe; gửi unit ngoài deck bị authority từ chối |
| Guest giả service ID/proof hoặc peer thứ ba | Không được bind vào Player2 |
| Map khác/chưa có scene trong build | Báo lỗi; không bắt đầu turn hoặc âm thầm dùng map khác |
| Load client chậm | Không mở input/lượt trước snapshot ACK |
| Endgame → ready một người | Không khởi tạo trận mới |
| Hai người ready → rematch, lặp ít nhất ba lần | MatchId/MP/hand/unit/turn reset; starting player đổi; không callback trùng |
| Rời ở lobby/loading/gameplay/endgame | Session rời/xóa; menu hiện; không NGO/handler/input gameplay còn hoạt động |
| Delete/leave lỗi mạng rồi phục hồi | Có thể retry cleanup; không tạo thêm session trước cleanup thành công |
| Chơi local sau khi rời session | Authority/AI/local PvP hoạt động theo lựa chọn mới |

Chỉ đánh dấu milestone hoàn thành sau khi có kết quả các ca Unity/hai thiết bị bắt buộc.
