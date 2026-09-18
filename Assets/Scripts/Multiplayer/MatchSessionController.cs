using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TurnBasedGame.Multiplayer
{
    // One owner for service membership, peer identity and the lifetime of the Relay connection.
    public sealed class MatchSessionController : MonoBehaviour
    {
        [Serializable] private sealed class Identity { public string Player; public string Token; }
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
        public bool IsClosing => leaving;
        public bool InMatch => launch != null;
        public bool HasSession => session != null || pendingCleanup != null;
        public bool IsHost => session != null && session.IsHost;
        public string Code => session?.Code ?? string.Empty;
        public int PlayerCount => session?.PlayerCount ?? 0;
        public int Round => launch?.Round ?? 0;
        public bool LocalReady => session != null && ReadyRound(session.CurrentPlayer) == Round + 1;
        public bool AllReady
        {
            get
            {
                if (terminal || session == null || session.PlayerCount != 2) return false;
                foreach (var player in session.Players) if (!IsPlayerReady(player)) return false;
                return true;
            }
        }
        public bool CanRematch => !terminal && InMatch && TurnManager.Instance != null && TurnManager.Instance.Winner.HasValue;
        public ulong? GuestPeer { get; private set; }
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
            session = await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions
            {
                Name = "The Summoners 1v1", MaxPlayers = 2, IsPrivate = true,
                PlayerProperties = PlayerProperties(),
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    ["build"] = Property(compatibility), ["map"] = Property(mapName), ["scene"] = Property(sceneName)
                }
            }.WithRelayNetwork().WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.DTLS }));
            AttachSession();
        }, true);

        public Task Join(string code) => Run(async () =>
        {
            if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Nhập mã phòng do host cung cấp.");
            await InitializeServices();
            CreateNetwork();
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant(),
                new JoinSessionOptions { PlayerProperties = PlayerProperties() }
                    .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.DTLS }));
            if (GetProperty("build") != compatibility) throw new InvalidOperationException("Khác phiên bản/content. Cả hai cần dùng cùng build.");
            mapName = GetProperty("map");
            ResolveMap(mapName);
            sceneName = GetProperty("scene");
            AttachSession();
        }, true);

        private async Task InitializeServices()
        {
            if (!Application.CanStreamedLevelBeLoaded(menuScene) || !Application.CanStreamedLevelBeLoaded(sceneName))
                throw new InvalidOperationException("Menu và scene trận cần có trong build trước khi tạo/tham gia phòng.");
            if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
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
                await current.RefreshAsync();
                if (session != current || leaving || terminal || GuestPeer.HasValue || current.PlayerCount != 2 ||
                    identity == null || identity.Player == current.Host || !current.HasPlayer(identity.Player)) return;
                var player = current.GetPlayer(identity.Player);
                if (GetPlayerProperty(player, "proof") != identity.Token || string.IsNullOrEmpty(identity.Token) ||
                    GetPlayerProperty(player, "build") != compatibility) return;
                GuestPeer = request.ClientNetworkId;
                guestServiceId = identity.Player;
                response.Approved = true;
            }
            catch (Exception) { /* Never include identity/proof in logs or disconnect reasons. */ }
            finally
            {
                if (!response.Approved) response.Reason = "Phòng đầy, khác build hoặc danh tính không hợp lệ. Kiểm tra mã phòng và thử lại.";
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
            session.RemovedFromSession += SessionEnded;
            session.SessionHostChanged += HostChanged;
            Status = "Host: Player1 • Khách: Player2. Kiểm tra đội hình rồi chọn Sẵn sàng.";
            SessionChanged();
        }

        public Task SetReady() => Run(async () =>
        {
            if (session == null || terminal || (InMatch && !CanRematch)) return;
            RequireLoadout(selected);
            string previous = GetPlayerProperty(session.CurrentPlayer, "ready");
            session.CurrentPlayer.SetProperty("ready", PlayerProperty(LocalReady ? "0" : (Round + 1).ToString()));
            try { await session.SaveCurrentPlayerDataAsync(); }
            catch { session.CurrentPlayer.SetProperty("ready", PlayerProperty(previous)); throw; }
            Status = LocalReady ? "Đã sẵn sàng. Chờ host bắt đầu." : "Đã hủy sẵn sàng.";
        });

        public Task StartMatch() => Run(async () =>
        {
            if (!IsHost || terminal || (InMatch && !CanRematch)) return;
            await session.RefreshAsync();
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
            try { await owner.SavePropertiesAsync(); committedRound = next.Round; }
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
                    throw new InvalidOperationException("Đối thủ đã rời phòng. Rời trận để trở về menu.");
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
                var operation = SceneManager.LoadSceneAsync(next.Scene);
                while (!operation.isDone) await Task.Yield();
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
                Status = error is SessionException serviceError
                    ? $"Dịch vụ: {serviceError.Error}. Kiểm tra mã phòng, kết nối và cấu hình Unity Services; thử lại."
                    : error.Message;
                if (connecting) await Cleanup();
            }
            finally { Busy = false; SessionChanged(); }
        }

        private async void Update()
        {
            if (session == null || leaving || terminal || Busy || refreshing || Time.realtimeSinceStartupAsDouble < nextRefresh) return;
            nextRefresh = Time.realtimeSinceStartupAsDouble + 5;
            refreshing = true;
            try { await session.RefreshAsync(); SessionChanged(); }
            catch (Exception) { Status = "Không cập nhật được phòng. Kiểm tra mạng hoặc rời phòng và thử lại."; Changed?.Invoke(); }
            finally { refreshing = false; }
        }

        private void Disconnected(ulong id)
        {
            if (leaving || terminal || session == null) return;
            if (IsHost && GuestPeer != id) return;
            GuestPeer = null;
            guestServiceId = null;
            if (InMatch || !IsHost) Fail("Đối thủ/host đã ngắt kết nối. Rời phòng để trở về menu.");
            else { Status = "Khách đã ngắt kết nối. Chờ khách tham gia lại và sẵn sàng."; Changed?.Invoke(); }
        }
        private void SessionEnded() { if (!leaving) Fail("Phòng đã đóng. Rời phòng để trở về menu."); }
        private void HostChanged(string _) { if (!leaving) Fail("Host đã rời phòng. MVP không chuyển host; hãy tạo phòng mới."); }
        internal void Fail(string message)
        {
            if (leaving || terminal) return;
            terminal = true; Status = message;
            MatchGameplayBootstrap.Instance?.Abort(message);
            network?.Shutdown();
            Changed?.Invoke();
        }
        public async Task Leave()
        {
            if (leaving || Busy) return;
            leaving = true; Busy = true; Changed?.Invoke();
            while (launching) await Task.Yield();
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
                var operation = SceneManager.LoadSceneAsync(menuScene);
                while (!operation.isDone) await Task.Yield();
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
                old.RemovedFromSession -= SessionEnded; old.SessionHostChanged -= HostChanged;
                try { if (old.IsHost) await old.AsHost().DeleteAsync(); else await old.LeaveAsync(); }
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
                network.ConnectionApprovalCallback = null;
                network.Shutdown();
                while (network != null && network.ShutdownInProgress) await Task.Yield();
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
            if (Instance == this) Instance = null;
        }
    }
}
