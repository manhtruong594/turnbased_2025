# Chuyển HUDScene sang UI Toolkit

## Phạm vi đã chuyển

`HUDScene` tạo HUD UI Toolkit khi scene được load. Cấu trúc nằm trong
`Assets/Resources/UI/BattleHUD.uxml`, style nằm trong `BattleHUD.uss`, và controller runtime là
`BattleHUDToolkit`.

HUD mới xử lý:

- tên player, trạng thái lượt và MP;
- tung hai xúc xắc, kết thúc lượt;
- danh sách unit để triệu hồi và trạng thái đủ MP/spawn point;
- spell hand, trạng thái có thể dùng và xác nhận spell;
- danh sách skill của unit được chọn, cooldown, hoàn tác di chuyển và kết thúc hành động;
- màn hình thắng/thua, chơi lại và về menu chính;
- chặn thao tác map khi con trỏ nằm trên UI Toolkit.

Canvas uGUI cũ vẫn giữ nguyên serialized reference để rollback. Khi HUD UI Toolkit khởi tạo thành
công, các `Canvas` screen-space và `GraphicRaycaster` cũ bị tắt; world-space UI không bị tác động.

## Rollback

Đặt PlayerPrefs `BattleHUD.UseUIToolkit` thành `0` trước khi load `HUDScene` để dùng lại toàn bộ HUD
uGUI cũ. Giá trị mặc định là `1`.

```csharp
PlayerPrefs.SetInt("BattleHUD.UseUIToolkit", 0);
```

Đặt lại thành `1` để bật UI Toolkit.

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
9. Đặt `BattleHUD.UseUIToolkit = 0`, load lại scene và xác nhận HUD uGUI cũ vẫn hoạt động.
