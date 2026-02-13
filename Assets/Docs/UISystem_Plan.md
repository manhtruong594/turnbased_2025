# Plan: Xây dựng UI System — Main Menu đến Battle Preparation

**TL;DR:** Xây dựng hệ thống UI theo kiến trúc **Prefab-based Screen Management** với **UI Toolkit cho menu/panels** (Main Menu, Shop, Inventory, Prepare Battle) và **uGUI cho in-game HUD** (giữ nguyên code hiện tại). Hệ thống gồm 3 layer: **UIManager** (điều phối screen flow + transitions), **Screen Controllers** (logic từng màn hình), và **Data Layer** (ScriptableObject mở rộng từ `PlayerDataSO` hiện có). Tất cả giao tiếp qua `GameMediator` pattern đã có, tuân thủ `BaseManager` inheritance và namespace conventions (`TurnBasedGame.UI`).

---

## Phase A: UI Infrastructure (Core Framework)

### A1. Tạo `UIManager` — Screen Flow Controller
- Tạo `Assets/Scripts/UI/Core/UIManager.cs` kế thừa `BaseManager`, singleton pattern giống `ObjectPoolManager`.
- Chức năng: quản lý stack các screen (`Stack<BaseScreen>`), push/pop screen, transition animation.
- API chính: `ShowScreen<T>()`, `HideScreen()`, `GoBack()`, `ShowPopup<T>()`.
- Registry: `Dictionary<Type, BaseScreen>` mapping screen type → prefab instance.
- Screen prefabs được khai báo qua `[SerializeField] List<BaseScreen> screenPrefabs` — Inspector-assigned.
- `DontDestroyOnLoad` để persist qua scene transitions.

### A2. Tạo `BaseScreen` abstract class
- Tạo `Assets/Scripts/UI/Core/BaseScreen.cs`.
- Abstract MonoBehaviour đặt trên root GameObject của mỗi screen prefab.
- Template Method: `OnScreenShow()`, `OnScreenHide()`, `OnScreenDestroy()`, `OnBackPressed()`.
- Giữ ref đến `UIManager` và `UIDocument` (UI Toolkit).
- Animation hooks: `PlayShowAnimation()`, `PlayHideAnimation()` (virtual, default fade in/out).

### A3. Tạo `ScreenTransition` system
- Tạo `Assets/Scripts/UI/Core/ScreenTransition.cs`.
- Strategy pattern: `IScreenTransition` interface với `TransitionIn(VisualElement)`, `TransitionOut(VisualElement)`.
- Implementations: `FadeTransition`, `SlideTransition`, `ScrollUnfurlTransition` (phù hợp theme Đại Nam).
- Dùng USS transitions/animations cho UI Toolkit elements.

### A4. Tạo `PopupManager`
- Tạo `Assets/Scripts/UI/Core/PopupManager.cs`.
- Quản lý modal dialogs, confirm dialogs, notifications riêng biệt khỏi screen stack.
- `ShowConfirmDialog(title, message, onConfirm, onCancel)`, `ShowNotification(message, duration)`.
- Overlay layer luôn trên top của screen stack.

---

## Phase B: Data Layer (ScriptableObject mở rộng)

### B1. Mở rộng `PlayerDataSO`
- Mở rộng `Assets/Scripts/Data/PlayerData/PlayerDataSO.cs`:
  - Thêm: `string PlayerName`, `int Gold`, `int PlayerLevel`, `int XP`.
  - Thêm: `List<UnitData> OwnedUnits` (tất cả unit đã mở khóa).
  - Thêm: `List<SkillBase> OwnedSpells` (tất cả spell đã mở khóa).
  - Thêm: `List<UnitData> SelectedDeck` (unit được chọn cho trận đấu, max ~5-6).
  - Thêm: `List<SkillBase> SelectedSpells` (spell được chọn, max ~3-4).
  - Giữ nguyên `AvailableUnits` hiện tại cho backward compatibility.

### B2. Tạo `ShopItemSO`
- Tạo `Assets/Scripts/Data/Shop/ShopItemSO.cs`.
- Fields: `string ItemName`, `string Description`, `Sprite Icon`, `int Price`, `ShopItemType Type` (Unit/Spell/Cosmetic), `ScriptableObject ItemRef` (ref đến `UnitData` hoặc `SkillBase`).

### B3. Tạo `ShopCatalogSO`
- Tạo `Assets/Scripts/Data/Shop/ShopCatalogSO.cs`.
- `List<ShopItemSO> AvailableItems` — danh sách hàng hóa.
- Có thể filter theo `ShopItemType`.

### B4. Tạo Reactive SO bindings mới
- Mở rộng pattern `IntReference` hiện có tại `Assets/Scripts/ReferencesSO/IntReference.cs`.
- Tạo `StringReference`, `BoolReference` nếu cần cho UI binding.
- Hoặc tạo generic `ReactiveValue<T>` SO base class.

---

## Phase C: Main Menu Screen

### C1. Tạo Main Menu Scene
- Tạo `Assets/Scenes/MainMenu.unity` — scene nhẹ, chỉ chứa Camera + UIManager + MainMenuScreen.
- Set làm scene đầu tiên trong Build Settings.

### C2. Tạo `MainMenuScreen`
- Tạo `Assets/Scripts/UI/Screens/MainMenuScreen.cs` kế thừa `BaseScreen`.
- UXML layout: `Assets/MyGame/UI/UXML/MainMenu.uxml`.
- USS stylesheet: `Assets/MyGame/UI/USS/MainMenu.uss`.
- Buttons: **Chiến Đấu** (→ Prepare Battle), **Cửa Hàng** (→ Shop), **Bộ Sưu Tập** (→ Inventory), **Cài Đặt** (→ Settings popup).
- Style: Theme Đại Nam — bàn trà cổ, con dấu cho nút Play, hộp sơn mài cho Shop (theo GDD section 4.2).

### C3. Tạo `SceneLoader` utility
- Tạo `Assets/Scripts/UI/Core/SceneLoader.cs`.
- Async scene loading: `LoadSceneAsync(sceneName, onProgress, onComplete)`.
- Loading screen overlay (UI Toolkit) với progress bar.
- Pattern: Load scene additive → unload previous → callback.

---

## Phase D: Prepare Battle Screen (Deck Builder)

### D1. Tạo `PrepareBattleScreen`
- Tạo `Assets/Scripts/UI/Screens/PrepareBattleScreen.cs` kế thừa `BaseScreen`.
- UXML: `Assets/MyGame/UI/UXML/PrepareBattle.uxml`.
- Layout 2 vùng: **Collection** (bên trái, scroll list tất cả `OwnedUnits` + `OwnedSpells`) và **Selected Deck** (bên phải, slots cho unit/spell đã chọn).

### D2. Tạo `DeckSlot` UI component
- Component cho mỗi slot trong deck: hiển thị icon, tên, stats; click để remove.
- Max slots configurable (từ `PlayerDataSO` hoặc constant).

### D3. Tạo `UnitCardElement` / `SpellCardElement`
- Custom VisualElement (UI Toolkit) hiển thị unit/spell card.
- Data binding từ `UnitData` / `SkillBase` SO.
- Drag-and-drop: Kéo từ Collection → Deck slot.

### D4. Logic validation
- Check: deck có đủ unit tối thiểu? Spell không trùng? Tổng cost hợp lệ?
- Button **Xác Nhận** → lưu vào `PlayerDataSO.SelectedDeck/SelectedSpells` → load battle scene.

---

## Phase E: Shop Screen

### E1. Tạo `ShopScreen`
- Tạo `Assets/Scripts/UI/Screens/ShopScreen.cs` kế thừa `BaseScreen`.
- UXML: `Assets/MyGame/UI/UXML/Shop.uxml`.
- Tabs: **Units**, **Spells**, **Cosmetics** (dùng UI Toolkit `TabView` hoặc custom tab system).
- Header: hiển thị Gold hiện tại từ `PlayerDataSO.Gold`.

### E2. Tạo `ShopItemElement`
- Custom VisualElement cho mỗi item: Icon, Name, Price, Buy button.
- State: Affordable (đủ gold), Owned (đã mua), Locked (chưa đủ level).
- Buy flow: Click Buy → `PopupManager.ShowConfirmDialog()` → trừ Gold → thêm vào `OwnedUnits/OwnedSpells`.

### E3. Tạo `ShopService`
- Tạo `Assets/Scripts/UI/Services/ShopService.cs`.
- Logic mua bán tách khỏi UI: `TryPurchase(ShopItemSO, PlayerDataSO) → bool`.
- Validate: đủ gold? Chưa sở hữu? Đủ level?
- Fire events qua Mediator: `OnItemPurchased(ShopItemSO)`.

---

## Phase F: Inventory / Collection Screen

### F1. Tạo `InventoryScreen`
- Tạo `Assets/Scripts/UI/Screens/InventoryScreen.cs` kế thừa `BaseScreen`.
- UXML: `Assets/MyGame/UI/UXML/Inventory.uxml`.
- Grid/List view hiển thị tất cả `OwnedUnits` và `OwnedSpells` từ `PlayerDataSO`.
- Filter/Sort: theo type, rarity, cost, name.

### F2. Tạo `UnitDetailPanel`
- Panel chi tiết khi click vào unit: full art, stats (HP, Cost, MoveRange), skill list, description.
- Có thể xem 3D model preview (stretch goal).

### F3. Tạo `SpellDetailPanel`
- Panel chi tiết spell: icon, description, cooldown, range, damage/effect, type.

---

## Phase G: Kết nối & Integration

### G1. Mở rộng `GameMediator` cho UI events
- Thêm events vào `Assets/Scripts/Manager/GameMediator.cs`:
  - `OnGoldChanged(int currentGold)`
  - `OnItemPurchased(ShopItemSO)`
  - `OnDeckChanged(PlayerDataSO)`
  - `OnScreenChanged(BaseScreen from, BaseScreen to)`

### G2. Kết nối battle flow
- Prepare Battle → Confirm → `SceneLoader.LoadSceneAsync("HUDScene")` → `GameMediator` nhận `PlayerDataSO.SelectedDeck` → `UnitSpawner` dùng deck data thay vì hardcoded units.
- Battle End → `OnGameEnd` → hiển thị Result popup (Win/Lose) → reward Gold/XP → return to Main Menu.

### G3. Settings Popup
- Tạo `Assets/Scripts/UI/Popups/SettingsPopup.cs`.
- Audio volume (Master/SFX/Music), Graphics quality, Language.
- Lưu vào `PlayerPrefs` (settings không cần SO).

---

## File Structure tổng quan

```
Assets/Scripts/UI/
├── Core/
│   ├── UIManager.cs          (Screen flow controller)
│   ├── BaseScreen.cs         (Abstract screen base)
│   ├── PopupManager.cs       (Modal/notification system)
│   ├── SceneLoader.cs        (Async scene loading)
│   └── ScreenTransition.cs   (IScreenTransition + impls)
├── Screens/
│   ├── MainMenuScreen.cs
│   ├── PrepareBattleScreen.cs
│   ├── ShopScreen.cs
│   └── InventoryScreen.cs
├── Popups/
│   ├── SettingsPopup.cs
│   ├── ConfirmDialog.cs
│   └── ResultPopup.cs
├── Components/
│   ├── UnitCardElement.cs
│   ├── SpellCardElement.cs
│   ├── DeckSlot.cs
│   └── ShopItemElement.cs
├── Services/
│   └── ShopService.cs
└── (existing files: PlayerUI.cs, DiceUI.cs, etc.)

Assets/Scripts/Data/
├── PlayerData/
│   └── PlayerDataSO.cs      (extended)
└── Shop/
    ├── ShopItemSO.cs
    └── ShopCatalogSO.cs

Assets/MyGame/UI/
├── UXML/
│   ├── MainMenu.uxml
│   ├── PrepareBattle.uxml
│   ├── Shop.uxml
│   └── Inventory.uxml
├── USS/
│   ├── Common.uss           (shared theme)
│   ├── MainMenu.uss
│   ├── PrepareBattle.uss
│   ├── Shop.uss
│   └── Inventory.uss
└── Prefabs/
    ├── MainMenuScreen.prefab
    ├── PrepareBattleScreen.prefab
    ├── ShopScreen.prefab
    └── InventoryScreen.prefab

Assets/Scenes/
├── MainMenu.unity           (new — entry point)
├── HUDScene.unity           (existing — battle)
└── LoadingScene.unity       (new — transition)
```

---

## Verification

1. **Unit test**: `ShopService.TryPurchase()` — test đủ/thiếu gold, đã sở hữu, chưa đủ level.
2. **Integration test**: Main Menu → Prepare Battle → Confirm → Load HUDScene → verify `UnitSpawner` nhận đúng deck data.
3. **Manual test**:
   - Flow: Main Menu → Shop → mua unit → Back → Inventory → thấy unit vừa mua → Prepare Battle → kéo unit vào deck → Confirm → vào trận.
   - Back button hoạt động đúng (screen stack pop).
   - Transition animations mượt.
   - Gold hiển thị real-time khi mua.
4. **Performance**: Profiler check UI rebuild cost, đặc biệt ListView virtualization cho Inventory/Shop (UI Toolkit `ListView` tự virtualize).

---

## Decisions

- **UI Toolkit cho menu, uGUI cho HUD**: Menu screens (Main Menu, Shop, Inventory, Prepare Battle) dùng UI Toolkit (USS styling, UXML layout, ListView virtualization). In-game HUD giữ nguyên uGUI hiện tại (PlayerUI, DiceUI, MPDisplayUI, SpawnPanel) — không refactor code đang hoạt động.
- **Prefab-based screens**: Mỗi screen là 1 prefab chứa `UIDocument` + Screen Controller script. `UIManager` instantiate khi cần, destroy khi pop khỏi stack. Tiết kiệm memory hơn single-canvas.
- **SO cho data, không persistence giai đoạn này**: `PlayerDataSO` mở rộng là nguồn dữ liệu chính. Data reset mỗi session (chấp nhận ở vertical slice). Save/Load system bổ sung sau.
- **Không reuse Simple Inventory System plugin**: Code quality thấp (static mutable state), pattern không phù hợp với kiến trúc Mediator hiện tại. Chỉ tham khảo UX drag-drop.
- **Thứ tự triển khai**: A (Infrastructure) → B (Data) → C (Main Menu) → D (Prepare Battle) → E (Shop) → F (Inventory) → G (Integration). Mỗi phase có thể demo độc lập.
