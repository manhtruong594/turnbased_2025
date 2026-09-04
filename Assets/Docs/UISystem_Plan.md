# Kế hoạch UI System

## 1. Kiến trúc hiện có

Project dùng hai lớp UI:

- UI Toolkit cho Main Menu, Prepare Battle, Inventory và screen flow.
- uGUI cho battle HUD, spawn, skill, spell card, timer và endgame.

Không chuyển đổi giữa hai công nghệ nếu không có mục tiêu migration riêng.

| Thành phần | Trạng thái | File chính |
|---|---|---|
| Screen stack/navigation | Hiện có | `UIManager`, `BaseScreen` |
| Transition | Hiện có | `IScreenTransition`, `ScreenTransitions` |
| Popup service | Hiện có | `PopupManager` |
| Scene loading | Hiện có | `SceneLoader` |
| Main Menu | Một phần | `MainMenuScreen`; Shop/Settings chưa nối |
| Prepare Battle | Hiện có, cần kiểm chứng | `PrepareBattleScreen`, `DeckSlot`, `DeckValidator` |
| Inventory | Hiện có, cần kiểm chứng | `InventoryScreen`, detail panels |
| Shop | Chưa có | Có `ShopItemSO`, `ShopCatalogSO`; thiếu screen/service/item UI |
| Settings | Chưa có | Chưa có popup và persistence |
| Battle HUD | Hiện có, cần kiểm chứng | `Assets/Scripts/UI/Battle UI` |
| Persistence | Chưa có | `PlayerDataSO` chỉ giữ runtime/asset data |

## 2. Luồng màn hình mục tiêu

```text
Main Menu
├── Prepare Battle ──> Battle ──> Result ──> Main Menu
├── Inventory ───────> Detail
├── Shop ────────────> Purchase result
└── Settings
```

`UIManager` quản lý stack screen và popup. `SceneLoader` chịu trách nhiệm chuyển scene. Battle HUD
không được giữ state profile; dữ liệu ngoài trận lấy từ `PlayerDataSO` và lớp persistence tương lai.

## 3. Công việc theo ưu tiên

### P0 — Ổn định flow đang có

1. Kiểm tra prefab registry, duplicate screen và back navigation.
2. Bảo đảm callback được unsubscribe khi screen/popup bị hủy.
3. Kiểm tra deck tối đa 6 unit, tối đa 4 spell và validation trước battle.
4. Kiểm tra Main Menu → Prepare Battle → Battle → Result → Main Menu.
5. Loại debug shortcut hoặc giới hạn bằng development build nếu không dùng trong release.

### P1 — Battle feedback

1. Disable action khi sai turn, thiếu MP, cooldown hoặc target invalid.
2. Hiển thị lý do failure gần vị trí thao tác.
3. Đồng bộ buff/debuff icon, duration, tooltip và fallback.
4. Bảo đảm confirm/cancel spell trả UI về idle.
5. Kiểm tra HUD ở aspect ratio và resolution mục tiêu.

### P1 — Settings và persistence

1. Tạo model setting versioned cho master/music/SFX, quality, resolution/fullscreen.
2. Tạo Settings popup dùng chung từ Main Menu và pause flow.
3. Tạo save/load cho profile, Gold, XP, collection và selected deck/spells.
4. Có default, validation và migration khi save thiếu field hoặc version cũ.
5. Không serialize trực tiếp reference runtime không ổn định; dùng ID bền vững cho content.

### P2 — Shop

1. Chốt Shop có thuộc release đầu hay không.
2. Nếu có, tạo `ShopScreen`, `ShopItemElement` và `ShopService`.
3. Validate ownership, price, Gold và transaction một lần.
4. Cập nhật collection/Gold bằng event sau transaction thành công.
5. Persistence phải hoàn thành trước khi Shop được coi là usable.

## 4. Data contract

`PlayerDataSO` hiện có profile, level, XP, Gold, owned units/spells và selected deck/spells. Đây là
nguồn dữ liệu UI trong phiên chạy, chưa phải save format. Save model không nên phụ thuộc trực tiếp vào
Unity object reference; lưu content ID rồi resolve qua catalog.

Giới hạn hiện tại:

- `MaxDeckSize = 6`.
- `MaxSpellSlots = 4`.
- Không cho item trùng trong selected deck/spells.
- Chỉ chọn content đã sở hữu.

## 5. Tiêu chí kiểm chứng

- Không screen/popup trùng sau khi điều hướng qua lại.
- Escape/back ưu tiên đóng popup trước, sau đó mới pop screen.
- Transition không nhận input gây double navigation.
- Danh sách rỗng, asset null và content thiếu icon có fallback rõ.
- Deck invalid không thể bắt đầu trận và có thông báo lý do.
- Gold/XP/collection/deck khôi phục đúng sau khi restart ứng dụng.
- Battle result chỉ cấp reward một lần.

## 6. File dự kiến khi triển khai phần thiếu

```text
Assets/Scripts/UI/Screens/ShopScreen.cs
Assets/Scripts/UI/Components/ShopItemElement.cs
Assets/Scripts/UI/Services/ShopService.cs
Assets/Scripts/UI/Popups/SettingsPopup.cs
Assets/Scripts/Data/Persistence/PlayerSaveData.cs
Assets/Scripts/Data/Persistence/SaveService.cs
```

Tên/path cuối cùng có thể đổi để khớp convention tại thời điểm triển khai; không tạo file rỗng trước.
