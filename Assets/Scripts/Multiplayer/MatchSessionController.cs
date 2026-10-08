using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Maps;
using TurnBasedGame.Multiplayer.Protocol;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TurnBasedGame.Multiplayer
{
    // One owner for service membership, peer identity and the lifetime of the Relay connection.
    public sealed class MatchSessionController : MonoBehaviour
    {
        // Metadata schema is pinned to Multiplayer Services 2.3.1. Never log Relay credentials.
        [Serializable] private sealed class RelayMetadata { public string RelayJoinCode; public string HostId; }
        [Serializable] private sealed class Identity { public string Player; public string Token; }
        // Sessions owns Relay/membership; this handler owns NGO across temporary transport restarts.
        private sealed class SessionNetworkHandler : INetworkHandler
        {
            private readonly NetworkManager manager;
            private readonly CancellationToken cancellation;
            public SessionNetworkHandler(NetworkManager manager, CancellationToken cancellation)
            { this.manager = manager; this.cancellation = cancellation; }
            public async Task StartAsync(NetworkConfiguration configuration)
            {
                if (configuration.Type != NetworkType.Relay ||
                    (configuration.Role != NetworkRole.Host && configuration.Role != NetworkRole.Client))
                    throw new InvalidOperationException("Only Relay host/client sessions are supported.");
                cancellation.ThrowIfCancellationRequested();
                bool host = configuration.Role == NetworkRole.Host;
                var transport = (UnityTransport)manager.NetworkConfig.NetworkTransport;
                transport.SetRelayServerData(configuration.RelayServerData);
                if (!(host ? manager.StartHost() : manager.StartClient()))
                    throw new InvalidOperationException("Cannot start session network.");
                double deadline = Time.realtimeSinceStartupAsDouble + MatchReconnectWindow.Duration;
                while (manager != null && manager.IsListening && !manager.IsConnectedClient &&
                    !cancellation.IsCancellationRequested && Time.realtimeSinceStartupAsDouble < deadline) await Task.Yield();
                if (cancellation.IsCancellationRequested || manager == null || !manager.IsConnectedClient)
                {
                    await StopAsync();
                    throw new InvalidOperationException("Kết nối Relay quá hạn hoặc bị host từ chối.");
                }
            }
            public async Task StopAsync()
            {
                if (manager == null) return;
                manager.Shutdown();
                double deadline = Time.realtimeSinceStartupAsDouble + 5;
                while (manager != null && manager.ShutdownInProgress && Time.realtimeSinceStartupAsDouble < deadline) await Task.Yield();
            }
        }
        [Serializable] private sealed class LaunchData
        {
            public int Round;
            public string Host, Guest, Map, Scene;
            public MatchLoadout HostLoadout, GuestLoadout;
        }

        public static MatchSessionController Instance { get; private set; }
        public event Action Changed;
        public string Status { get; private set; } = "Tạo phòng hoặc nhập mã phòng để tham gia.";
        public bool Busy { get; private set; }
        public bool Failed => terminal;
        public MatchDisconnectReason DisconnectReason { get; private set; }
        public bool IsClosing => leaving;
        public bool InMatch => launch != null;
        public bool HasSession => session != null || pendingCleanup != null || pendingConnection;
        public bool IsHost => session != null && session.IsHost;
        public string Code => session?.Code ?? string.Empty;
        public int PlayerCount => session?.PlayerCount ?? 0;
        public int Round => launch?.Round ?? 0;
        public bool LocalReady => session != null && ReadyRound(session.CurrentPlayer) == Round + 1;
        public bool AllReady
        {
            get
            {
                if (terminal || reconnect.Active || session == null || session.PlayerCount != 2) return false;
                foreach (var player in session.Players) if (!IsPlayerReady(player)) return false;
                return true;
            }
        }
        public bool CanRematch => !terminal && InMatch && TurnManager.Instance != null && TurnManager.Instance.Winner.HasValue;
        public ulong? GuestPeer { get; private set; }
        private CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool pendingConnection;
        private AsyncOperation sceneLoad;
        private ISession session;
        private ISession pendingCleanup;
        private NetworkManager network;
        private MatchContentRegistry content;
        private MatchLoadout selected;
        private LaunchData launch;
        private string mapName, sceneName, menuScene, originalHost, token, compatibility;
        private bool leaving, launching, terminal;
        private double nextRefresh;
        private bool refreshing;
        private int committedRound;
        private string guestServiceId;
        private readonly MatchReconnectWindow reconnect = new MatchReconnectWindow();
        private bool reconnectAttempt;
        private double nextReconnect;
        private bool CanRecover => !leaving && !terminal && reconnect.Allows(Time.realtimeSinceStartupAsDouble);

        public static void Open(PlayerDataSO player, BattleMapDefinitionSO map, string scene, UIDocument sourceDocument)
        {
            if (Instance != null) return;
            var go = new GameObject(nameof(MatchSessionController));
            DontDestroyOnLoad(go);
            var controller = go.AddComponent<MatchSessionController>();
            Instance = controller;
            controller.menuScene = SceneManager.GetActiveScene().name;
            try
            {
                controller.content = new MatchContentRegistry(UnityEngine.Resources.Load<MatchContentCatalog>(MatchContentCatalog.ResourceName));
                controller.selected = new MatchLoadout { Units = new string[player.SelectedDeck.Count], Spells = new string[player.SelectedSpells.Count] };
                for (int i = 0; i < controller.selected.Units.Length; i++) controller.selected.Units[i] = controller.content.GetId(player.SelectedDeck[i]);
                for (int i = 0; i < controller.selected.Spells.Length; i++) controller.selected.Spells[i] = controller.content.GetId(player.SelectedSpells[i]);
                controller.RequireLoadout(controller.selected);
                controller.mapName = map != null ? map.name : string.Empty;
                controller.ResolveMap(controller.mapName);
                controller.sceneName = scene;
                controller.compatibility = $"{MatchProtocol.ProtocolVersion}:{MatchProtocol.GameplayRulesVersion}:{Application.version}:{controller.content.ContentCatalogHash}";
                go.AddComponent<MatchSessionView>().Initialize(controller, sourceDocument);
            }
            catch (Exception error)
            {
                TurnBasedGame.UI.PopupManager.Instance?.ShowNotification(error.Message);
                Destroy(go);
            }
        }

        public Task Create() => Run(async () =>
        {
            await InitializeServices();
            CreateNetwork();
            session = await WaitForSession(MultiplayerService.Instance.CreateSessionAsync(new SessionOptions
            {
                Name = "The Summoners 1v1", MaxPlayers = 2, IsPrivate = true,
                PlayerProperties = PlayerProperties(),
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    ["build"] = Property(compatibility), ["map"] = Property(mapName), ["scene"] = Property(sceneName)
                }
            }.WithRelayNetwork().WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.DTLS })
                .WithNetworkHandler(new SessionNetworkHandler(network, lifetime.Token))));
            AttachSession();
        }, true);

        public Task Join(string code) => Run(async () =>
        {
            if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Nhập mã phòng do host cung cấp.");
            await InitializeServices();
            CreateNetwork();
            session = await WaitForSession(MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant(),
                new JoinSessionOptions { PlayerProperties = PlayerProperties() }
                    .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.DTLS })
                    .WithNetworkHandler(new SessionNetworkHandler(network, lifetime.Token))));
            if (GetProperty("build") != compatibility) throw new InvalidOperationException("Khác phiên bản/content. Cả hai cần dùng cùng build.");
            mapName = GetProperty("map");
            ResolveMap(mapName);
            sceneName = GetProperty("scene");
            AttachSession();
        }, true);

        private Task Wait(Task task) => MatchServiceWait.Run(task, lifetime.Token);

        private async Task<T> WaitForSession<T>(Task<T> request) where T : ISession
        {
            try { return await MatchServiceWait.Run(request, lifetime.Token); }
            catch { pendingConnection = true; _ = CleanupLateSession(request); throw; }
        }

        private async Task CleanupLateSession<T>(Task<T> request) where T : ISession
        {
            ISession late = null;
            try
            {
                late = await request;
                await MatchServiceWait.Run(late.IsHost ? late.AsHost().DeleteAsync() : late.LeaveAsync(), CancellationToken.None);
            }
            catch
            {
                if (late != null) Debug.LogWarning("[MP-SESSION] Late session cleanup failed.");
                if (late != null && this != null)
                {
                    pendingCleanup = late;
                    terminal = true;
                    Status = "Phòng trả về muộn chưa dọn được. Bấm Rời phòng để thử lại.";
                }
            }
            finally { pendingConnection = false; if (this != null) Changed?.Invoke(); }
        }

        private static async Task WaitScene(AsyncOperation operation)
        {
            if (operation == null) return;
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            while (!operation.isDone)
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                    throw new TimeoutException("Tải scene quá hạn (30s). Chờ tải xong rồi bấm Rời phòng để thử lại.");
                await Task.Yield();
            }
        }

        private async Task InitializeServices()
        {
            if (!Application.CanStreamedLevelBeLoaded(menuScene) || !Application.CanStreamedLevelBeLoaded(sceneName))
                throw new InvalidOperationException("Menu và scene trận cần có trong build trước khi tạo/tham gia phòng.");
            if (UnityServices.State != ServicesInitializationState.Initialized) await Wait(UnityServices.InitializeAsync());
            if (!AuthenticationService.Instance.IsSignedIn) await Wait(AuthenticationService.Instance.SignInAnonymouslyAsync());
            token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        private Dictionary<string, PlayerProperty> PlayerProperties() => new Dictionary<string, PlayerProperty>
        {
            ["loadout"] = PlayerProperty(JsonUtility.ToJson(selected)),
            ["ready"] = PlayerProperty("0"), ["build"] = PlayerProperty(compatibility), ["proof"] = PlayerProperty(token)
        };
        private static PlayerProperty PlayerProperty(string value) => new PlayerProperty(value, VisibilityPropertyOptions.Member);
        private static SessionProperty Property(string value) => new SessionProperty(value, VisibilityPropertyOptions.Member);
        private string GetProperty(string key) => session.Properties.TryGetValue(key, out var value) ? value.Value : string.Empty;
        private static string GetPlayerProperty(IReadOnlyPlayer player, string key) => player.Properties.TryGetValue(key, out var value) ? value.Value : string.Empty;
        private static int ReadyRound(IReadOnlyPlayer player) => int.TryParse(GetPlayerProperty(player, "ready"), out int value) ? value : 0;

        private void CreateNetwork()
        {
            if (NetworkManager.Singleton != null) throw new InvalidOperationException("Một kết nối khác đang hoạt động. Rời kết nối đó trước.");
            var go = new GameObject("SessionNetwork");
            DontDestroyOnLoad(go);
            var utp = go.AddComponent<UnityTransport>();
            network = go.AddComponent<NetworkManager>();
            network.NetworkConfig = new NetworkConfig { NetworkTransport = utp, EnableSceneManagement = false,
                ConnectionApproval = true, TickRate = 30,
                ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Identity { Player = AuthenticationService.Instance.PlayerId, Token = token })) };
            network.ConnectionApprovalCallback = Approve;
            network.OnClientDisconnectCallback += Disconnected;
            network.OnTransportFailure += TransportFailed;
            network.OnServerStopped += ServerStopped;
        }

        private async void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            if (request.ClientNetworkId == NetworkManager.ServerClientId) { response.Approved = true; return; }
            response.Pending = true;
            try
            {
                var current = session;
                if (current == null || leaving || terminal || GuestPeer.HasValue || request.Payload.Length > 512) return;
                var identity = JsonUtility.FromJson<Identity>(Encoding.UTF8.GetString(request.Payload));
                await Wait(current.RefreshAsync());
                if (session != current || leaving || terminal || GuestPeer.HasValue || current.PlayerCount != 2 ||
                    identity == null || identity.Player == current.Host || !current.HasPlayer(identity.Player)) return;
                var player = current.GetPlayer(identity.Player);
                if (InMatch && (identity.Player != launch.Guest || !reconnect.Allows(identity.Player, Time.realtimeSinceStartupAsDouble))) return;
                if (!InMatch && reconnect.Active && !reconnect.Allows(identity.Player, Time.realtimeSinceStartupAsDouble)) return;
                if (GetPlayerProperty(player, "proof") != identity.Token || string.IsNullOrEmpty(identity.Token) ||
                    GetPlayerProperty(player, "build") != compatibility) return;
                GuestPeer = request.ClientNetworkId;
                guestServiceId = identity.Player;
                response.Approved = true;
                if (!InMatch) Reconnected();
            }
            catch (Exception) { /* Never include identity/proof in logs or disconnect reasons. */ }
            finally
            {
                if (!response.Approved) response.Reason = GuestPeer.HasValue ? "MP_SLOT_BUSY" :
                    "Phòng đầy, khác build hoặc danh tính không hợp lệ. Kiểm tra mã phòng và thử lại.";
                response.Pending = false;
            }
        }

        private void AttachSession()
        {
            if (network == null || !network.IsListening || (!network.IsServer && !network.IsConnectedClient))
                throw new InvalidOperationException("Kết nối Relay đã đóng. Kiểm tra mạng và tham gia lại.");
            originalHost = session.Host;
            session.Changed += SessionChanged;
            session.Deleted += SessionEnded;
            session.RemovedFromSession += RemovedFromSession;
            session.SessionHostChanged += HostChanged;
            Status = "Host: Player1 • Khách: Player2. Kiểm tra đội hình rồi chọn Sẵn sàng.";
            SessionChanged();
        }

        public Task SetReady() => Run(async () =>
        {
            if (session == null || terminal || reconnect.Active || (InMatch && !CanRematch)) return;
            RequireLoadout(selected);
            string previous = GetPlayerProperty(session.CurrentPlayer, "ready");
            session.CurrentPlayer.SetProperty("ready", PlayerProperty(LocalReady ? "0" : (Round + 1).ToString()));
            try { await Wait(session.SaveCurrentPlayerDataAsync()); }
            catch { session.CurrentPlayer.SetProperty("ready", PlayerProperty(previous)); throw; }
            Status = LocalReady ? "Đã sẵn sàng. Chờ host bắt đầu." : "Đã hủy sẵn sàng.";
        });

        public Task StartMatch() => Run(async () =>
        {
            if (!IsHost || terminal || reconnect.Active || (InMatch && !CanRematch)) return;
            await Wait(session.RefreshAsync());
            if (terminal || leaving || session == null) return;
            var host = session.GetPlayer(originalHost);
            IReadOnlyPlayer guest = null;
            foreach (var player in session.Players) if (player.Id != originalHost) guest = player;
            if (guest == null || !GuestPeer.HasValue || guest.Id != guestServiceId)
                throw new InvalidOperationException("Cần hai người đã kết nối trước khi bắt đầu.");
            var hostDeck = ReadLoadout(host); var guestDeck = ReadLoadout(guest);
            if (!MatchLobbyRules.CanStart(session.PlayerCount, Round + 1, ReadyRound(host), ReadyRound(guest),
                ValidLoadout(hostDeck), ValidLoadout(guestDeck), GetPlayerProperty(guest, "build") == compatibility))
                throw new InvalidOperationException("Cả hai cần sẵn sàng và có loadout/build hợp lệ.");
            if (!Application.CanStreamedLevelBeLoaded(sceneName)) throw new InvalidOperationException("Scene trận chưa có trong build: " + sceneName);
            var next = new LaunchData { Round = Round + 1, Host = originalHost, Guest = guest.Id,
                HostLoadout = hostDeck, GuestLoadout = guestDeck, Map = mapName, Scene = sceneName };
            var owner = session.AsHost();
            string previousLaunch = GetProperty("launch");
            bool previousLock = owner.IsLocked;
            owner.IsLocked = true;
            owner.SetProperty("launch", Property(JsonUtility.ToJson(next)));
            try { await Wait(owner.SavePropertiesAsync()); committedRound = next.Round; }
            catch
            {
                owner.IsLocked = previousLock;
                owner.SetProperty("launch", Property(previousLaunch));
                throw;
            }
            SessionChanged();
        });

        private MatchLoadout ReadLoadout(IReadOnlyPlayer player)
        {
            string json = GetPlayerProperty(player, "loadout");
            if (json.Length > 2048) throw new InvalidOperationException("Loadout vượt giới hạn.");
            return JsonUtility.FromJson<MatchLoadout>(json);
        }
        private bool ValidLoadout(MatchLoadout value) => MatchLobbyRules.ValidLoadout(value, PlayerDataSO.MaxDeckSize,
            PlayerDataSO.MaxSpellSlots, id => content.ResolveUnitPrefab(id) != null, id => content.Resolve<SpellCardData>(id) != null);
        private void RequireLoadout(MatchLoadout value)
        {
            if (!ValidLoadout(value)) throw new InvalidOperationException("Đội hình không hợp lệ. Chọn lại unit/phép có trong catalog, không trùng lặp.");
        }

        private bool IsPlayerReady(IReadOnlyPlayer player)
        {
            try { return ReadyRound(player) == Round + 1 && GetPlayerProperty(player, "build") == compatibility && ValidLoadout(ReadLoadout(player)); }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        private BattleMapDefinitionSO ResolveMap(string name)
        {
            if (string.IsNullOrEmpty(name)) return null; // Scene's authored default map.
            var catalog = BattleMapCatalogSO.LoadDefault();
            BattleMapDefinitionSO result = null;
            if (catalog != null) foreach (var map in catalog.Maps)
            {
                if (map == null || map.name != name) continue;
                if (result != null || !map.Enabled || !map.TryValidate(out _)) throw new InvalidOperationException("Map không hợp lệ hoặc trùng tên: " + name);
                result = map;
            }
            return result != null ? result : throw new InvalidOperationException("Không tìm thấy map của host: " + name);
        }

        private async void SessionChanged()
        {
            Changed?.Invoke();
            if (leaving || terminal || launching || Busy || session == null) return;
            try
            {
                if (InMatch && session.PlayerCount != 2)
                {
                    if (IsHost) ForfeitGuest("Đối thủ đã rời phòng hoặc bị loại khỏi session.", MatchDisconnectReason.ClientLeft);
                    else Fail("Host đã rời phòng. Trận kết thúc; không chuyển host.", MatchDisconnectReason.HostLeft);
                    return;
                }
                string json = GetProperty("launch");
                if (string.IsNullOrEmpty(json)) return;
                var next = JsonUtility.FromJson<LaunchData>(json);
                if (next == null || next.Round <= Round) return;
                if (IsHost && next.Round != committedRound) return;
                if (next.Round != Round + 1 || next.Host != originalHost || !session.HasPlayer(next.Guest) || next.Host == next.Guest ||
                    next.Scene != sceneName || next.Map != mapName || (InMatch && !CanRematch))
                    throw new InvalidOperationException("Trạng thái bắt đầu trận không hợp lệ. Rời phòng và tạo lại.");
                RequireLoadout(next.HostLoadout); RequireLoadout(next.GuestLoadout);
                if (!Application.CanStreamedLevelBeLoaded(next.Scene)) throw new InvalidOperationException("Scene trận chưa có trong build: " + next.Scene);
                launching = true;
                MatchGameplayBootstrap.Instance?.CloseSessionMatch();
                if (InMatch) TurnBasedGame.ObjectPool.ObjectPoolManager.Instance?.ClearAllPools();
                launch = next;
                BattleLaunchContext.SelectMap(ResolveMap(next.Map));
                MatchGameplayBootstrap.BeginSession(network, IsHost, next.Round);
                Status = "Đang tải trận và chờ đối thủ xác nhận snapshot…";
                Changed?.Invoke();
                sceneLoad = SceneManager.LoadSceneAsync(next.Scene);
                await WaitScene(sceneLoad);
            }
            catch (Exception error) { Fail(error.Message); }
            finally { launching = false; Changed?.Invoke(); }
        }

        public IReadOnlyList<UnitController> Units(PlayerID player)
        {
            var deck = player == PlayerID.Player1 ? launch.HostLoadout : launch.GuestLoadout;
            var units = new List<UnitController>(deck.Units.Length);
            foreach (string id in deck.Units) units.Add(content.ResolveUnitPrefab(id));
            return units;
        }
        public IReadOnlyList<SpellCardData> Spells(PlayerID player)
        {
            var deck = player == PlayerID.Player1 ? launch.HostLoadout : launch.GuestLoadout;
            var spells = new List<SpellCardData>(deck.Spells.Length);
            foreach (string id in deck.Spells) spells.Add(content.Resolve<SpellCardData>(id));
            return spells;
        }
        public bool AllowsUnit(PlayerID player, string id) => launch != null &&
            Array.IndexOf(player == PlayerID.Player1 ? launch.HostLoadout.Units : launch.GuestLoadout.Units, id) >= 0;

        public string PlayersStatus()
        {
            if (session == null) return string.Empty;
            var text = new StringBuilder();
            foreach (var player in session.Players)
            {
                text.Append(player.Id == originalHost ? "Player1 (host)" : "Player2");
                text.Append(player.Id == session.CurrentPlayer.Id ? " — bạn" : string.Empty);
                text.AppendLine(IsPlayerReady(player) ? " • Sẵn sàng, loadout hợp lệ" : " • Chưa sẵn sàng / loadout chưa hợp lệ");
                try
                {
                    var deck = ReadLoadout(player);
                    if (ValidLoadout(deck))
                    {
                        text.Append("  ");
                        foreach (string id in deck.Units) text.Append(content.ResolveUnitPrefab(id).UnitData.unitName).Append(" · ");
                        text.AppendLine($"{deck.Spells.Length} phép");
                    }
                }
                catch (ArgumentException) { }
                catch (InvalidOperationException) { }
            }
            return text.ToString();
        }

        private async Task Run(Func<Task> action, bool connecting = false)
        {
            if (Busy || leaving || terminal || (connecting && HasSession)) return;
            Busy = true; Status = "Đang xử lý…"; Changed?.Invoke();
            try { await action(); }
            catch (Exception error)
            {
                if (this == null) return;
                Status = error is SessionException serviceError
                    ? $"Dịch vụ: {serviceError.Error}. Kiểm tra mã phòng, kết nối và cấu hình Unity Services; thử lại."
                    : error.Message;
                if (connecting) { lifetime.Cancel(); await Cleanup(); lifetime.Dispose(); lifetime = new CancellationTokenSource(); }
                else if (error is TimeoutException) Fail(Status, MatchDisconnectReason.ServiceError);
            }
            finally { if (this != null) { Busy = false; SessionChanged(); } }
        }

        private async void Update()
        {
            if (session == null || leaving || terminal) return;
            if (reconnect.Expired(Time.realtimeSinceStartupAsDouble))
            {
                if (IsHost) ForfeitGuest("Khách không reconnect trong 30 giây; khách thua do mất kết nối.", MatchDisconnectReason.ReconnectExpired);
                else Fail("Không kết nối lại được host trong 30 giây. Trận kết thúc; không chuyển host.", MatchDisconnectReason.HostLost);
                return;
            }
            if (reconnect.Active && !IsHost && !reconnectAttempt && Time.realtimeSinceStartupAsDouble >= nextReconnect)
                _ = ReconnectClient();
            if (Busy || refreshing || Time.realtimeSinceStartupAsDouble < nextRefresh) return;
            nextRefresh = Time.realtimeSinceStartupAsDouble + 5;
            refreshing = true;
            try
            {
                await Wait(session.RefreshAsync());
                if (!leaving && !terminal && DisconnectReason == MatchDisconnectReason.ServiceError)
                    DisconnectReason = reconnect.Active ? MatchDisconnectReason.TemporaryNetwork : MatchDisconnectReason.None;
                SessionChanged();
            }
            catch (Exception)
            {
                if (!leaving && !terminal)
                { DisconnectReason = MatchDisconnectReason.ServiceError; Status = "Không cập nhật được phòng. Kiểm tra mạng hoặc rời phòng và thử lại."; Changed?.Invoke(); }
            }
            finally { refreshing = false; }
        }

        private void Disconnected(ulong id)
        {
            if (leaving || terminal || session == null) return;
            if (IsHost && GuestPeer != id) return;
            if (!IsHost && !string.IsNullOrEmpty(network.DisconnectReason) &&
                !(reconnect.Active && network.DisconnectReason == "MP_SLOT_BUSY"))
            { Fail("Host từ chối hoặc ngắt kết nối: " + network.DisconnectReason,
                network.DisconnectReason == "MP_HOST_LEFT" ? MatchDisconnectReason.HostLeft : MatchDisconnectReason.Rejected); return; }
            GuestPeer = null;
            DisconnectReason = MatchDisconnectReason.TemporaryNetwork;
            reconnect.Begin(Time.realtimeSinceStartupAsDouble, IsHost ? guestServiceId : originalHost);
            MatchGameplayBootstrap.Instance?.SuspendConnection();
            Status = IsHost ? "Khách mất kết nối. Giữ slot 30 giây; đồng hồ lượt vẫn chạy."
                : "Mất kết nối host. Đang thử kết nối lại trong 30 giây…";
            Changed?.Invoke();
        }

        private async Task ReconnectClient()
        {
            reconnectAttempt = true;
            var current = session;
            try
            {
                while (CanRecover && network.ShutdownInProgress) await Task.Yield();
                if (!CanRecover || current != session) return;
                await Wait(current.ReconnectAsync());
                await Wait(current.RefreshAsync());
                if (!CanRecover || current != session) return;
                if (current.Host != originalHost || !current.HasPlayer(originalHost))
                { Fail("Host đã rời phòng. Trận kết thúc; không chuyển host.", MatchDisconnectReason.HostLeft); return; }
                if (!AuthenticationService.Instance.IsSignedIn || current.CurrentPlayer.Id != AuthenticationService.Instance.PlayerId)
                { Fail("Danh tính đăng nhập đã thay đổi. Không thể khôi phục slot.", MatchDisconnectReason.Rejected); return; }
                // Rejoin Relay for fresh allocation data; an old client allocation may have expired.
                var metadata = JsonUtility.FromJson<RelayMetadata>(GetProperty("_session_network"));
                if (metadata == null || metadata.HostId != originalHost || string.IsNullOrEmpty(metadata.RelayJoinCode))
                    throw new InvalidOperationException("Relay metadata unavailable.");
                var allocation = await MatchServiceWait.Run(RelayService.Instance.JoinAllocationAsync(metadata.RelayJoinCode), lifetime.Token);
                if (!CanRecover || current != session) return;
                network.Shutdown();
                while (CanRecover && network.ShutdownInProgress) await Task.Yield();
                if (!CanRecover || current != session) return;
                ((UnityTransport)network.NetworkConfig.NetworkTransport).SetRelayServerData(allocation.ToRelayServerData("dtls"));
                if (!network.StartClient()) throw new InvalidOperationException("Cannot restart client.");
                while (CanRecover && network.IsListening && !network.IsConnectedClient) await Task.Yield();
                if (!CanRecover || current != session || !network.IsConnectedClient) return;
                if (InMatch) MatchGameplayBootstrap.Instance?.ResumeConnection();
                else Reconnected();
                // Keep the original reconnect deadline until snapshot ACK completes.
                while (CanRecover && network.IsConnectedClient) await Task.Yield();
            }
            catch (Exception)
            {
                if (CanRecover)
                { Status = "Dịch vụ/Relay chưa phục hồi. Đang thử lại trong cửa sổ 30 giây…"; Changed?.Invoke(); }
            }
            finally { reconnectAttempt = false; nextReconnect = Time.realtimeSinceStartupAsDouble + 2; }
        }

        internal bool Reconnected()
        {
            if (leaving || terminal || reconnect.Expired(Time.realtimeSinceStartupAsDouble)) return false;
            reconnect.Complete();
            DisconnectReason = MatchDisconnectReason.None;
            Status = "Đã kết nối lại và đồng bộ trạng thái trận.";
            Changed?.Invoke();
            return true;
        }

        private void ForfeitGuest(string reason, MatchDisconnectReason code)
        {
            try
            {
                if (InMatch && TurnManager.Instance != null && !TurnManager.Instance.Winner.HasValue)
                    LocalMatchAuthority.ForfeitDisconnectedGuest();
            }
            finally { Fail(reason, code); }
        }
        private void TransportFailed()
        {
            if (IsHost) Fail("Host mất kết nối Relay. Trận kết thúc; không chuyển host.", MatchDisconnectReason.HostLost);
        }
        private void ServerStopped(bool _) { if (IsHost) TransportFailed(); }
        private void SessionEnded() { if (!leaving) Fail("Host đã đóng phòng hoặc session bị xóa. Trận kết thúc.", MatchDisconnectReason.HostLeft); }
        private void RemovedFromSession() { if (!leaving) Fail("Bạn đã bị loại khỏi phòng (kick). Không thể reconnect slot này.", MatchDisconnectReason.Kicked); }
        private void HostChanged(string _) { if (!leaving) Fail("Host đã rời phòng. MVP không chuyển host; hãy tạo phòng mới.", MatchDisconnectReason.HostLeft); }
        internal void Fail(string message, MatchDisconnectReason reason = MatchDisconnectReason.None)
        {
            if (leaving || terminal) return;
            terminal = true; lifetime.Cancel(); Status = message; DisconnectReason = reason;
            MatchGameplayBootstrap.Instance?.Abort(message);
            network?.Shutdown();
            Changed?.Invoke();
        }
        public async Task Leave()
        {
            if (leaving || Busy) return;
            leaving = true; lifetime.Cancel(); reconnect.Complete(); Busy = true; Changed?.Invoke();
            if (IsHost && GuestPeer.HasValue && network != null && network.IsListening)
                network.DisconnectClient(GuestPeer.Value, "MP_HOST_LEFT");
            while (launching) await Task.Yield();
            // Unity cannot cancel an in-flight scene load. Never restore local authority before it finishes.
            try { await WaitScene(sceneLoad); }
            catch (TimeoutException error)
            {
                Status = error.Message; leaving = false; Busy = false; terminal = true;
                Changed?.Invoke(); return;
            }
            bool returnToMenu = InMatch;
            if (!await Cleanup())
            {
                leaving = false; Busy = false; terminal = true; Changed?.Invoke();
                return;
            }
            // Scene managers must be gone before restoring local authority.
            if (returnToMenu)
            {
                if (!Application.CanStreamedLevelBeLoaded(menuScene))
                { Status = "Menu chưa có trong build: " + menuScene; Busy = false; leaving = false; Changed?.Invoke(); return; }
                sceneLoad = SceneManager.LoadSceneAsync(menuScene);
                try { await WaitScene(sceneLoad); }
                catch (TimeoutException error)
                {
                    Status = error.Message; leaving = false; Busy = false; terminal = true;
                    Changed?.Invoke(); return;
                }
            }
            MatchContext.ConfigureLocalPvP(); BattleLaunchContext.Clear();
            LocalMatchAuthority.ClearSessionState();
            Destroy(gameObject);
        }
        private async Task<bool> Cleanup()
        {
            MatchGameplayBootstrap.Instance?.CloseSessionMatch();
            if (InMatch) TurnBasedGame.ObjectPool.ObjectPoolManager.Instance?.ClearAllPools();
            var old = session ?? pendingCleanup;
            session = null;
            pendingCleanup = null;
            if (old != null)
            {
                old.Changed -= SessionChanged; old.Deleted -= SessionEnded;
                old.RemovedFromSession -= RemovedFromSession; old.SessionHostChanged -= HostChanged;
                try { await MatchServiceWait.Run(old.IsHost ? old.AsHost().DeleteAsync() : old.LeaveAsync(), CancellationToken.None); }
                catch (SessionException error) when (error.Error == SessionError.SessionDeleted || error.Error == SessionError.SessionNotFound) { }
                catch (Exception)
                {
                    pendingCleanup = old;
                    terminal = true;
                    Status = "Chưa xác nhận được việc rời phòng. Kiểm tra mạng rồi bấm Rời phòng để thử lại.";
                }
            }
            if (network != null)
            {
                network.OnClientDisconnectCallback -= Disconnected;
                network.OnTransportFailure -= TransportFailed;
                network.OnServerStopped -= ServerStopped;
                network.ConnectionApprovalCallback = null;
                network.Shutdown();
                double deadline = Time.realtimeSinceStartupAsDouble + 5;
                while (network != null && network.ShutdownInProgress && Time.realtimeSinceStartupAsDouble < deadline) await Task.Yield();
                if (network != null) Destroy(network.gameObject);
                network = null;
                await Task.Yield();
            }
            GuestPeer = null;
            guestServiceId = null;
            return pendingCleanup == null;
        }
        private void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
