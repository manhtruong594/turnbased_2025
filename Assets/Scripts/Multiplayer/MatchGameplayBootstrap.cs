using System;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Multiplayer.Protocol;
using TurnBasedGame.Unit;
using TurnBasedGame.Capture;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TurnBasedGame.Multiplayer
{
    // Development-only direct connection entry point. Sessions/Relay and retained slots belong to phases 5–6.
    [DefaultExecutionOrder(1000)]
    public sealed class MatchGameplayBootstrap : MonoBehaviour
    {
        private const string ControlMessage = "match-bootstrap-v1", SnapshotMessage = "match-snapshot-v1";
        public static MatchGameplayBootstrap Instance { get; private set; }
        public static bool Active => Instance != null;
        public static bool InputReady => Instance == null || Instance.ready;
        public bool IsHost { get; private set; }
        private NetworkManager network;
        private MatchGameplayTransport transport;
        private readonly MatchReplicaApplier applier = new MatchReplicaApplier();
        private string sceneHash;
        private string matchId;
        private ulong? peer;
        private MatchSnapshot issued;
        private bool ready, started, failed, managersReady, awaitingSnapshot;
        private double expires, retryAt;
        private int readyFrames;
        private int snapshotFailures;
        private string gameplayScene;
        private string role;

        private void Start()
        {
            if (failed) return;
            // NGO registers built-in message handlers in its AfterSceneLoad callback.
            // Starting earlier leaves ILPPMessageProvider empty in a player build.
            try { Connect(); }
            catch (Exception error) { Fail(error.Message); return; }
            string scene = Argument("-mp-gameplay-scene", "HUDScene");
            if (SceneManager.GetActiveScene().name != scene)
            {
                if (!Application.CanStreamedLevelBeLoaded(scene)) { Fail("Gameplay scene is not included in build: " + scene); return; }
                SceneManager.LoadScene(scene);
            }
        }

        public static bool CanControl(PlayerID player)
        {
            return InputReady && MatchContext.CanHumanControl(player) &&
                TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer == player &&
                TurnManager.Instance.CurrentState != TurnState.Initialization && TurnManager.Instance.CurrentState != TurnState.GameEnd;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Launch()
        {
            string role = Argument("-mp-gameplay-role", null);
            if (role == null) return;
            var go = new GameObject(nameof(MatchGameplayBootstrap));
            DontDestroyOnLoad(go);
            var bootstrap = go.AddComponent<MatchGameplayBootstrap>();
            Instance = bootstrap;
            try { bootstrap.PrepareRole(role); }
            catch (Exception error) { bootstrap.Fail(error.Message); }
        }

        private static string Argument(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            return fallback;
        }

        private void PrepareRole(string requestedRole)
        {
            if (requestedRole != "host" && requestedRole != "client") throw new ArgumentException("Expected -mp-gameplay-role host|client.");
            role = requestedRole;
            IsHost = role == "host";
            MatchContext.ConfigureNetworkPvP(IsHost ? PlayerID.Player1 : PlayerID.Player2, IsHost);
            if (!IsHost) LocalMatchAuthority.PrepareReplica();
        }

        private void Connect()
        {
            if (NetworkManager.Singleton != null) throw new InvalidOperationException("Another NetworkManager is active.");
            var utp = gameObject.AddComponent<UnityTransport>();
            network = gameObject.AddComponent<NetworkManager>();
            network.NetworkConfig = new NetworkConfig { NetworkTransport = utp, EnableSceneManagement = false };
            utp.SetConnectionData(Argument("-mp-address", "127.0.0.1"), ushort.Parse(Argument("-mp-port", "27982")), "0.0.0.0");
            if (!(role == "host" ? network.StartHost() : network.StartClient())) throw new InvalidOperationException("Cannot start gameplay transport.");
            network.CustomMessagingManager.RegisterNamedMessageHandler(ControlMessage, OnControl);
            if (!network.IsServer) network.CustomMessagingManager.RegisterNamedMessageHandler(SnapshotMessage, OnSnapshot);
            network.OnClientDisconnectCallback += OnDisconnect;
            expires = Time.realtimeSinceStartupAsDouble + 30;
        }

        private void Update()
        {
            if (failed || network == null) return;
            if (!ready && Time.realtimeSinceStartupAsDouble >= expires) { Fail("Gameplay bootstrap/resync timeout (30s)."); return; }
            if (!managersReady)
            {
                if (GameMediator.Instance == null || !GameMediator.Instance.IsInitialized) return;
                // All scene Start callbacks (including hand/loadout setup) must have completed.
                if (++readyFrames < 2) return;
                sceneHash = ComputeSceneHash();
                managersReady = true;
                gameplayScene = GameMediator.Instance.gameObject.scene.path;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                if (network.IsServer)
                {
                    matchId = LocalMatchAuthority.MatchId;
                    transport = new MatchGameplayTransport(network, PlayerId.Player1, matchId);
                }
            }
            transport?.Tick();
            if (!network.IsServer && network.IsConnectedClient && !ready && !awaitingSnapshot && Time.realtimeSinceStartupAsDouble >= retryAt)
            {
                SendControl(0, NetworkManager.ServerClientId);
                retryAt = Time.realtimeSinceStartupAsDouble + 1;
            }
        }

        internal MatchSnapshot Wrap(CommandAcknowledgement state, PlayerId viewer)
        {
            double deadline = 0;
            foreach (var entry in state.StateChanges)
                if (entry.Kind == StateChangeKind.Turn)
                    deadline = double.Parse(entry.ContentId, CultureInfo.InvariantCulture);
            return new MatchSnapshot { Compatibility = LocalMatchAuthority.Compatibility, SceneHash = sceneHash,
                NextCommandId = LocalMatchAuthority.NextCommandId(viewer), Deadline = deadline, State = state };
        }

        private void SendSnapshot()
        {
            var state = new CommandAcknowledgement { MatchId = matchId, Actor = PlayerId.Player2, Accepted = true,
                ServerSequence = LocalMatchAuthority.ServerSequence, NextClientSequence = LocalMatchAuthority.NextClientSequence(PlayerId.Player2),
                StateChanges = MatchSnapshotProtocol.Visible(LocalMatchAuthority.CaptureState().ToArray(), PlayerId.Player2) };
            issued = Wrap(state, PlayerId.Player2);
            issued.Hash = MatchSnapshotProtocol.Hash(state.StateChanges, issued.Deadline);
            Send(SnapshotMessage, peer.Value, MatchSnapshotProtocol.Serialize(issued));
        }

        private void OnControl(ulong sender, FastBufferReader buffer)
        {
            if (failed || !managersReady) return;
            try
            {
                var bytes = MatchGameplayTransport.ReadBytes(buffer, 1024);
                if (bytes == null) return;
                using var reader = new BinaryReader(new MemoryStream(bytes));
                byte kind = reader.ReadByte();
                var compatibility = MatchProtocol.DeserializeCompatibility(reader.ReadBytes(70));
                string scene = reader.ReadString();
                ulong sequence = reader.ReadUInt64(); string hash = reader.ReadString();
                if (reader.BaseStream.Position != reader.BaseStream.Length) return;
                if (!network.IsServer && sender != NetworkManager.ServerClientId) return;
                if (LocalMatchAuthority.Compatibility.Compare(compatibility) != CommandReason.None || scene != sceneHash)
                {
                    if (network.IsServer) network.DisconnectClient(sender);
                    else Fail("Gameplay compatibility/scene mismatch.");
                    return;
                }
                if (network.IsServer)
                {
                    if (sender == NetworkManager.ServerClientId || (peer.HasValue && peer.Value != sender))
                    { if (sender != NetworkManager.ServerClientId) network.DisconnectClient(sender); return; }
                    if (kind == 0)
                    {
                        if (!peer.HasValue) { peer = sender; transport.BindAuthenticatedPeer(sender, PlayerId.Player2); }
                        ready = false; expires = Time.realtimeSinceStartupAsDouble + 30;
                        SendSnapshot();
                    }
                    else if (kind == 1 && peer == sender && issued != null && sequence == issued.State.ServerSequence && hash == issued.Hash)
                    {
                        if (!started)
                        {
                            LocalMatchAuthority.StartNetworkMatch(); started = true; SendSnapshot();
                        }
                        else if (sequence != LocalMatchAuthority.ServerSequence) SendSnapshot();
                        else
                        {
                            ready = true; SendControl(2, sender, sequence, hash);
                            GameMediator.Instance.NotifyReplicaApplied();
                            Debug.Log("[MP-GAMEPLAY] READY sequence=" + sequence);
                        }
                    }
                }
                else if (kind == 2 && issued != null && sequence == issued.State.ServerSequence && hash == issued.Hash)
                {
                    ready = true; awaitingSnapshot = false;
                    RefreshPresentation(); transport.RetryPending();
                    Debug.Log("[MP-GAMEPLAY] READY sequence=" + sequence);
                }
            }
            catch (Exception error) when (error is IOException || error is ArgumentException || error is InvalidOperationException)
            { Fail(error.Message); }
        }

        private void OnSnapshot(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId || failed || !managersReady) return;
            try
            {
                var snapshot = MatchSnapshotProtocol.Deserialize(MatchGameplayTransport.ReadBytes(reader, MatchSnapshotProtocol.MaxBytes));
                if (snapshot.State.Actor != PlayerId.Player2 || !ValidateEnvelope(snapshot)) throw new ArgumentException("Snapshot identity mismatch.");
                if (transport != null && snapshot.State.ServerSequence < transport.AppliedSequence) return;
                ready = false;
                if (transport == null)
                {
                    matchId = snapshot.State.MatchId;
                    transport = new MatchGameplayTransport(network, PlayerId.Player2, matchId);
                }
                applier.Apply(snapshot, PlayerId.Player2);
                transport.RestoreSnapshot(snapshot); issued = snapshot;
                awaitingSnapshot = true;
                SendControl(1, NetworkManager.ServerClientId, snapshot.State.ServerSequence, snapshot.Hash);
            }
            catch (Exception error)
            {
                if (++snapshotFailures > 1) { Fail("Snapshot apply failed: " + error.Message); return; }
                Debug.LogWarning("[MP-GAMEPLAY] Snapshot retry: " + error.Message);
                awaitingSnapshot = false; RequestSnapshot();
            }
        }

        private bool ValidateEnvelope(MatchSnapshot snapshot) => snapshot.SceneHash == sceneHash &&
            LocalMatchAuthority.Compatibility.Compare(snapshot.Compatibility) == CommandReason.None &&
            (matchId == null || snapshot.State.MatchId == matchId);

        internal bool ApplyCommit(MatchSnapshot snapshot)
        {
            if (!ready || !ValidateEnvelope(snapshot)) return false;
            try { applier.Apply(snapshot, PlayerId.Player2); RefreshPresentation(); return true; }
            catch (Exception error) { Debug.LogWarning("[MP-GAMEPLAY] Resync: " + error.Message); return false; }
        }

        public void RequestSnapshot()
        {
            if (failed || awaitingSnapshot || network == null || network.IsServer || !managersReady) return;
            ready = false; awaitingSnapshot = true; expires = Time.realtimeSinceStartupAsDouble + 30;
            GameMediator.Instance?.NotifyReplicaApplied();
            SendControl(0, NetworkManager.ServerClientId);
        }

        private static void RefreshPresentation()
        {
            var mediator = GameMediator.Instance;
            for (int p = 1; p <= 2; p++) mediator.NotifyMPChanged((PlayerID)p,
                TurnBasedGame.Resources.MPManager.Instance.GetCurrentMP((PlayerID)p), TurnBasedGame.Resources.MPManager.Instance.MaxMP);
            mediator.NotifyHandChanged(PlayerID.Player2);
            mediator.NotifyReplicaApplied();
            for (int p = 1; p <= 2; p++)
                foreach (var unit in UnitSpawner.Instance.GetPlayerUnits((PlayerID)p)) unit.RefreshReplicaPresentation();
            if (TurnManager.Instance.Winner.HasValue) mediator.NotifyReplicaGameEnd(TurnManager.Instance.Winner.Value);
        }

        private void SendControl(byte kind, ulong recipient, ulong sequence = 0, string hash = "")
        {
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
            writer.Write(kind); writer.Write(MatchProtocol.SerializeCompatibility(LocalMatchAuthority.Compatibility));
            writer.Write(sceneHash); writer.Write(sequence); writer.Write(hash);
            Send(ControlMessage, recipient, stream.ToArray());
        }

        private void Send(string message, ulong recipient, byte[] bytes)
        {
            using var writer = new FastBufferWriter(bytes.Length + 4, Allocator.Temp);
            writer.WriteValueSafe(bytes.Length); writer.WriteBytesSafe(bytes);
            network.CustomMessagingManager.SendNamedMessage(message, recipient, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private static string ComputeSceneHash()
        {
            var parts = new System.Collections.Generic.List<string>();
            parts.Add(GameMediator.Instance.gameObject.scene.path);
            var map = MapManager.Instance.Map;
            parts.Add($"grid:{map.Type}:{map.Axis}:{map.RotationType}:" + map.Edge.ToString("R", CultureInfo.InvariantCulture));
            foreach (var tile in map.Tiles)
            {
                var entity = MapManager.Instance.MapEntity.Tile(tile.TilePos);
                parts.Add(JsonUtility.ToJson(tile) + ":" + (entity != null && entity.Vacant));
            }
            foreach (var point in UnitSpawner.Instance.SpawnPoints) parts.Add(LocalMatchAuthority.Runtime.GetSpawnPointId(point));
            if (CapturePointManager.Instance != null)
                foreach (var point in CapturePointManager.Instance.CapturePoints) parts.Add("capture:" + point.GridPosition);
            parts.Sort(StringComparer.Ordinal);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", parts)))).Replace("-", "").ToLowerInvariant();
        }

        private void OnDisconnect(ulong id)
        {
            if (IsHost && (!peer.HasValue || peer.Value != id)) return;
            Fail("Gameplay peer disconnected; reconnect lifecycle belongs to phase 6.");
        }
        internal void Abort(string reason) => Fail(reason);
        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.path == gameplayScene) { Fail("Gameplay scene unloaded."); Destroy(gameObject); }
        }
        private void Fail(string reason)
        {
            if (failed) return;
            failed = true; ready = false;
            GameMediator.Instance?.NotifyReplicaApplied();
            Debug.LogError("[MP-GAMEPLAY] " + reason);
            // Keep the replica role/closed gate after disconnect; never fall back to local authority.
            if (network != null) network.Shutdown();
        }

        private void OnDestroy()
        {
            transport?.Dispose();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (network != null)
            {
                network.OnClientDisconnectCallback -= OnDisconnect;
                network.CustomMessagingManager?.UnregisterNamedMessageHandler(ControlMessage);
                network.CustomMessagingManager?.UnregisterNamedMessageHandler(SnapshotMessage);
                network.Shutdown();
            }
            if (Instance == this) Instance = null;
        }
    }
}
