# Thuật ngữ Multiplayer quan trọng trong Unity

> Danh sách rút gọn các thuật ngữ cốt lõi để thiết kế, triển khai và kiểm thử game multiplayer theo mô hình client–server, server-authoritative trong Unity.

## 1. Kiến trúc mạng

### Client

Máy hoặc tiến trình của người chơi; nhận input, hiển thị game và giao tiếp với server.

### Server

Tiến trình quản lý trạng thái chung, xử lý luật chơi và xác thực hành động.

### Host

Một tiến trình vừa là server vừa là client.

### Dedicated server

Server chạy độc lập, không có người chơi cục bộ và thường không cần giao diện đồ họa.

### Client–server architecture

Kiến trúc trong đó các client kết nối tới một server trung tâm.

### Server-authoritative

Server là nguồn sự thật cuối cùng và quyết định trạng thái hợp lệ của game.

### Relay

Dịch vụ trung chuyển dữ liệu giữa các máy, giúp kết nối mà không cần người chơi tự mở port.

### Transport

Lớp chịu trách nhiệm thiết lập kết nối và truyền dữ liệu giữa các endpoint. Unity Transport là transport thường dùng với Netcode for GameObjects.

## 2. Phiên chơi và kết nối

### Session

Phiên kết nối có một nhóm người chơi và trạng thái dùng chung.

### Lobby

Nơi người chơi tập hợp, xem danh sách thành viên và chuẩn bị trước trận.

### Matchmaking

Hệ thống tìm và ghép người chơi vào cùng một trận.

### Authentication

Xác minh danh tính người chơi.

### Connection approval

Bước server chấp nhận hoặc từ chối client trước khi client tham gia session.

### Disconnect

Client hoặc server bị ngắt khỏi session.

### Reconnect

Client kết nối lại và khôi phục đúng danh tính, quyền sở hữu cùng trạng thái cần thiết.

### Join-in-progress

Cho phép client tham gia sau khi trận đã bắt đầu; cần gửi đủ trạng thái hiện tại cho client mới.

## 3. Định danh, authority và ownership

### Client ID

Định danh của một client trong session.

### Network object

GameObject được hệ thống mạng định danh, spawn và đồng bộ giữa server với client.

### Authority

Quyền quyết định trạng thái hợp lệ của object hoặc hệ thống.

### Ownership

Quan hệ xác định client nào sở hữu một network object. Ownership không đồng nghĩa với authority.

### Server validation

Server kiểm tra quyền, dữ liệu và luật chơi trước khi chấp nhận yêu cầu từ client.

### Source of truth

Nguồn dữ liệu được xem là chính xác cuối cùng; trong mô hình server-authoritative, đó là server.

## 4. Đồng bộ trạng thái

### Replication

Sao chép trạng thái từ nguồn authoritative sang các client liên quan.

### Spawn / Despawn

Tạo hoặc loại bỏ một network object trong session và thông báo thay đổi cho client.

### Network prefab

Prefab đã được đăng ký để có thể spawn và nhận diện nhất quán qua mạng.

### Network variable

Biến giữ trạng thái được đồng bộ, có quy tắc đọc và ghi xác định.

### Snapshot

Bản ghi trạng thái game tại một tick hoặc thời điểm cụ thể.

### Serialization / Deserialization

Chuyển dữ liệu thành byte để truyền qua mạng và khôi phục byte thành dữ liệu sử dụng được.

### Scene synchronization

Đảm bảo các client tải đúng scene và nhận đúng network object thuộc scene.

### Interest management

Chỉ gửi object hoặc dữ liệu cần thiết đến từng client để giảm băng thông và chi phí xử lý.

## 5. RPC và thông điệp

### RPC — Remote Procedure Call

Thông điệp yêu cầu code chạy trên máy khác. RPC phù hợp với request hoặc event; network variable phù hợp với state.

### Server RPC

Thông điệp từ client gửi tới server, thường biểu diễn ý định của người chơi và phải được server xác thực.

### Client RPC

Thông điệp từ server gửi tới một hoặc nhiều client, thường dùng cho event hoặc presentation.

### Reliable delivery

Cơ chế bảo đảm dữ liệu được nhận theo yêu cầu; phù hợp với sự kiện không được phép mất.

### Unreliable delivery

Cơ chế chấp nhận mất gói để giảm độ trễ và overhead; phù hợp với cập nhật liên tục có thể được thay thế bởi dữ liệu mới hơn.

## 6. Thời gian và chất lượng mạng

### Tick

Một bước cập nhật simulation hoặc network.

### Tick rate

Số tick mỗi giây. Tick rate khác frame rate.

### Latency

Thời gian dữ liệu đi từ nguồn tới đích.

### Ping / RTT

Thời gian khứ hồi của dữ liệu từ client tới server rồi quay lại.

### Jitter

Mức dao động của latency theo thời gian.

### Packet loss

Tỷ lệ packet không tới được nơi nhận.

### Bandwidth

Dung lượng dữ liệu tối đa kết nối có thể truyền trong một đơn vị thời gian.

### Network time

Mốc thời gian chung dùng để sắp xếp event và đồng bộ simulation giữa các máy.

## 7. Bù trễ và simulation

### Interpolation

Nội suy giữa các trạng thái đã nhận để hiển thị chuyển động mượt hơn.

### Client-side prediction

Client mô phỏng trước input cục bộ để giảm cảm giác trễ.

### Server reconciliation

Client sửa trạng thái dự đoán theo kết quả authoritative từ server và phát lại input chưa được xác nhận.

### Lag compensation

Nhóm kỹ thuật giúp server xét hành động theo độ trễ của người chơi.

### Server rewind

Server xem lại trạng thái trong quá khứ để kiểm tra hành động tại thời điểm client thực hiện.

### Deterministic simulation

Cùng trạng thái ban đầu và input sẽ tạo cùng kết quả trên mọi máy.

### Lockstep

Các máy tiến simulation dựa trên cùng tập input theo từng bước; thường cần tính xác định cao.

### Rollback netcode

Quay về trạng thái trước, áp dụng input đúng rồi mô phỏng lại để sửa sai lệch.

## 8. Netcode for GameObjects

### Netcode for GameObjects — NGO

Framework multiplayer cấp cao của Unity dành cho workflow GameObject và MonoBehaviour.

### NetworkManager

Component trung tâm quản lý cấu hình, transport, kết nối, client và server của NGO.

### NetworkBehaviour

Base class cho MonoBehaviour cần lifecycle, ownership, RPC hoặc state đồng bộ của NGO.

### NetworkObject

Component cấp định danh mạng, ownership và lifecycle spawn cho GameObject.

### NetworkVariable<T>

Kiểu dữ liệu đồng bộ trạng thái của NGO với quyền đọc và ghi cấu hình được.

### NetworkTransform

Component đồng bộ position, rotation và scale theo authority đã cấu hình.

### OnNetworkSpawn / OnNetworkDespawn

Callback khi NetworkObject bắt đầu hoặc kết thúc lifecycle mạng. Dùng chúng cho khởi tạo và dọn dẹp phụ thuộc vào network state.

### IsServer / IsClient / IsHost / IsOwner

Các cờ runtime cho biết vai trò tiến trình và ownership của NetworkBehaviour hiện tại.

### OwnerClientId

Client ID của client đang sở hữu NetworkObject.

### Unity Transport — UTP

Transport chính thức của Unity, thường được dùng bên dưới NGO.

### Unity Lobby / Unity Relay

Các dịch vụ Unity Gaming Services hỗ trợ tập hợp người chơi và kết nối qua relay. Chúng không tự đồng bộ gameplay state.

## 9. Bảo mật, tối ưu và kiểm thử

### Anti-cheat boundary

Ranh giới xác định dữ liệu nào từ client không được tin và phải được server kiểm tra.

### Rate limiting

Giới hạn tần suất request hoặc message để giảm spam và lạm dụng.

### Network budget

Giới hạn băng thông, số message và thời gian xử lý mạng cho mỗi tick hoặc client.

### Network object pooling

Tái sử dụng object spawn thường xuyên để giảm Instantiate, Destroy và garbage collection.

### Network simulator

Công cụ mô phỏng latency, jitter và packet loss để kiểm thử ngoài điều kiện localhost lý tưởng.

### Desync

Trạng thái giữa server và client, hoặc giữa các máy trong simulation xác định, bị sai khác.

### Load test

Kiểm thử hệ thống với số client hoặc lưu lượng dự kiến để tìm giới hạn thực tế.

### Metrics / Telemetry

Dữ liệu đo lường như latency, packet loss, tick duration, băng thông, lỗi và số client.

## Nguyên tắc cần nhớ

1. Client gửi ý định; server xác thực và quyết định kết quả.
2. Ownership xác định chủ sở hữu, không tự cấp quyền quyết định gameplay state.
3. Dùng RPC cho request hoặc event; dùng NetworkVariable cho state cần tồn tại và đồng bộ.
4. Không phải dữ liệu nào cũng cần gửi cho mọi client hoặc gửi ở mọi tick.
5. FPS và tick rate là hai khái niệm khác nhau.
6. Kiểm thử với latency, jitter, packet loss, disconnect, reconnect và nhiều client.
7. Tách gameplay logic, network logic và presentation logic.
