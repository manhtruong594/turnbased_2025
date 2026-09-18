using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TurnBasedGame.Multiplayer
{
    // Binds the authored Prepare Battle lobby, then creates a persistent copy in gameplay.
    public sealed class MatchSessionView : MonoBehaviour
    {
        private MatchSessionController controller;
        private PanelSettings panelSettings;
        private UIDocument runtimeDocument;
        private VisualElement panel, lobby, root;
        private Label status, room, players;
        private TextField code;
        private Button create, join, ready, start, leave, copy;
        private bool wasInputReady, wasRematch;
        private string lastStatus, lastPlayers;

        public void Initialize(MatchSessionController owner, UIDocument sourceDocument)
        {
            if (sourceDocument == null) throw new ArgumentNullException(nameof(sourceDocument));
            controller = owner;
            panelSettings = sourceDocument.panelSettings;
            Bind(sourceDocument.rootVisualElement);
            SceneManager.sceneLoaded += OnSceneLoaded;
            controller.Changed += Refresh;
            Refresh();
        }

        private void Bind(VisualElement documentRoot)
        {
            UnbindButtons();
            root = documentRoot;
            panel = root.Q("multiplayer-panel");
            if (panel == null) throw new InvalidOperationException("PrepareBattle.uxml thiếu multiplayer-panel.");
            lobby = panel.Q("multiplayer-lobby");
            status = panel.Q<Label>("multiplayer-status");
            room = panel.Q<Label>("multiplayer-room");
            players = panel.Q<Label>("multiplayer-players");
            code = panel.Q<TextField>("multiplayer-code");
            create = panel.Q<Button>("multiplayer-create");
            join = panel.Q<Button>("multiplayer-join");
            copy = panel.Q<Button>("multiplayer-copy");
            ready = panel.Q<Button>("multiplayer-ready");
            start = panel.Q<Button>("multiplayer-start");
            leave = panel.Q<Button>("multiplayer-leave");
            if (lobby == null || status == null || room == null || players == null || code == null ||
                create == null || join == null || copy == null || ready == null || start == null || leave == null)
                throw new InvalidOperationException("PrepareBattle.uxml thiếu control multiplayer Phase 5.");
            code.maxLength = 16;
            create.clicked += Create;
            join.clicked += Join;
            copy.clicked += Copy;
            ready.clicked += Ready;
            start.clicked += StartMatch;
            leave.clicked += Leave;
            panel.style.display = DisplayStyle.Flex;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!controller.InMatch || runtimeDocument != null) return;
            BuildRuntimeDocument();
            Refresh();
        }

        private void BuildRuntimeDocument()
        {
            UnbindButtons();
            runtimeDocument = gameObject.AddComponent<UIDocument>();
            runtimeDocument.panelSettings = panelSettings;
            runtimeDocument.sortingOrder = 200;
            root = runtimeDocument.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.right = 16;
            panel.style.top = 16;
            panel.style.width = 410;
            panel.style.paddingTop = panel.style.paddingBottom = 16;
            panel.style.paddingLeft = panel.style.paddingRight = 16;
            panel.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.98f);
            panel.style.color = Color.white;
            root.Add(panel);
            panel.Add(new Label("MULTIPLAYER • 1v1"));
            status = new Label();
            status.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(status);
            lobby = new VisualElement();
            panel.Add(lobby);
            room = new Label();
            lobby.Add(room);
            players = new Label();
            players.style.whiteSpace = WhiteSpace.Normal;
            lobby.Add(players);
            code = new TextField("Mã phòng") { maxLength = 16 };
            lobby.Add(code);
            create = Add(lobby, "Tạo phòng", Create);
            join = Add(lobby, "Tham gia", Join);
            copy = Add(lobby, "Sao chép mã phòng", Copy);
            ready = Add(lobby, "Sẵn sàng", Ready);
            start = Add(lobby, "Bắt đầu trận", StartMatch);
            leave = Add(panel, "Rời phòng", Leave);
        }

        private static Button Add(VisualElement parent, string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.style.height = 32;
            button.style.marginTop = 6;
            parent.Add(button);
            return button;
        }

        private async void Create() => await controller.Create();
        private async void Join() => await controller.Join(code.value);
        private void Copy() => GUIUtility.systemCopyBuffer = controller.Code;
        private async void Ready() => await controller.SetReady();
        private async void StartMatch() => await controller.StartMatch();
        private async void Leave() => await controller.Leave();

        private void Update()
        {
            bool inputReady = MatchGameplayBootstrap.InputReady;
            bool rematch = controller != null && controller.CanRematch;
            if (wasInputReady != inputReady || wasRematch != rematch) Refresh();
        }

        private void Refresh()
        {
            if (controller == null || panel == null) return;
            wasInputReady = MatchGameplayBootstrap.InputReady;
            wasRematch = controller.CanRematch;
            bool showLobby = !controller.InMatch || controller.CanRematch;
            if (runtimeDocument != null)
            {
                root.pickingMode = showLobby ? PickingMode.Position : PickingMode.Ignore;
                root.style.backgroundColor = showLobby ? new Color(0, 0, 0, 0.65f) : Color.clear;
            }
            panel.style.display = showLobby || controller.Failed ? DisplayStyle.Flex : DisplayStyle.None;
            lobby.style.display = showLobby ? DisplayStyle.Flex : DisplayStyle.None;
            string text = controller.Status;
            if (!controller.Failed && controller.InMatch && MatchGameplayBootstrap.InputReady)
                text = controller.CanRematch
                    ? "Trận đã kết thúc. Cả hai chọn Tái đấu, sau đó host bắt đầu."
                    : "Đã kết nối • Trận " + controller.Round;
            if (lastStatus != text) { lastStatus = text; status.text = text; }
            if (showLobby)
            {
                room.text = controller.HasSession ? "Mã phòng: " + controller.Code : "Dùng đội hình đã chọn ở màn hình chuẩn bị.";
                string roster = controller.PlayersStatus();
                if (lastPlayers != roster) { lastPlayers = roster; players.text = roster; }
            }
            bool idle = !controller.Busy;
            code.style.display = create.style.display = join.style.display = controller.HasSession ? DisplayStyle.None : DisplayStyle.Flex;
            create.SetEnabled(idle);
            join.SetEnabled(idle);
            copy.style.display = ready.style.display = controller.HasSession ? DisplayStyle.Flex : DisplayStyle.None;
            ready.text = controller.LocalReady ? "Hủy sẵn sàng" : controller.InMatch ? "Tái đấu" : "Sẵn sàng";
            ready.SetEnabled(idle && !controller.Failed);
            start.style.display = controller.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
            start.SetEnabled(idle && controller.AllReady);
            leave.SetEnabled(idle);
        }

        private void UnbindButtons()
        {
            if (create != null) create.clicked -= Create;
            if (join != null) join.clicked -= Join;
            if (copy != null) copy.clicked -= Copy;
            if (ready != null) ready.clicked -= Ready;
            if (start != null) start.clicked -= StartMatch;
            if (leave != null) leave.clicked -= Leave;
        }

        private void OnDestroy()
        {
            UnbindButtons();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (controller != null) controller.Changed -= Refresh;
            if (runtimeDocument == null && panel != null) panel.style.display = DisplayStyle.None;
        }
    }
}
