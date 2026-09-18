# HUD UI Toolkit

## Phạm vi

`HUDScene` chỉ khởi tạo HUD bằng UI Toolkit. Cấu trúc nằm trong
`Assets/Resources/UI/BattleHUD.uxml`, style nằm trong `BattleHUD.uss`, controller runtime là
`BattleHUDToolkit`.

Hai vị trí `local-frame-slot` và `opponent-frame-slot` được dựng tại runtime theo `MatchContext`.
Local player luôn nằm ở slot local, kể cả khi client mạng là `Player2`. Trong `VersusAI`, slot đối
thủ dùng biến thể AI; trong `LocalPvP` và `NetworkPvP`, slot đối thủ dùng biến thể player và bind với
`MatchContext.OpponentOf(MatchContext.LocalPlayer)`. Ở `LocalPvP`, action bar đổi binding theo player
đang tới lượt. Frame chỉ hiển thị state, không tạo thêm `PlayerController` hoặc `AIController`.

HUD xử lý:

- tên player, trạng thái lượt, thời gian lượt và MP;
- tung hai xúc xắc, kết thúc lượt;
- danh sách unit để triệu hồi và trạng thái đủ MP/spawn point;
- spell hand, trạng thái có thể dùng và xác nhận spell;
- danh sách skill của unit được chọn, cooldown, hoàn tác di chuyển và kết thúc hành động;
- màn hình thắng/thua, chơi lại và về menu chính;
- chặn thao tác map khi con trỏ nằm trên UI Toolkit.

Gameplay không giữ reference đến `Button`, `CanvasGroup`, `TextMeshProUGUI` hoặc `EventSystem` của
uGUI. `TurnManager`, `PlayerController`, `UnitController`, `UnitAttack` và `SpellCardManager` chỉ giữ
state/gameplay command; dữ liệu hiển thị được chuyển cho `BattleHUDToolkit`.

Không còn PlayerPrefs `BattleHUD.UseUIToolkit` hoặc đường rollback sang HUD uGUI. Các Canvas
screen-space cũ còn được `BattleHUDToolkit` tắt khi load `HUDScene` để tránh prefab/scene cũ render
chồng trong giai đoạn dọn asset. World-space UI như health bar và floating text không bị tác động.

## Kiểm chứng runtime

Trong Play Mode cần kiểm tra lần lượt:

1. Player 1 và Player 2 đổi lượt đúng; chỉ action bar của người đang điều khiển hoạt động.
2. Mỗi nút xúc xắc chỉ dùng một lần trong lượt và số action còn lại đúng.
3. MP cập nhật sau khi tung xúc xắc, triệu hồi unit và dùng spell.
4. Unit không đủ MP hoặc không còn spawn point bị vô hiệu hóa.
5. Spell không đủ điều kiện bị vô hiệu hóa; Confirm/Cancel trả state về đúng luồng.
6. Chọn unit hiển thị đúng skill và cooldown; nút hoàn tác chỉ bật sau khi di chuyển.
7. Click lên HUD không chọn tile hoặc kích hoạt attack/spell target phía sau.
8. Endgame hiển thị đúng winner; restart và main menu tải đúng scene.
9. HUD cũ không render và không nhận input.
