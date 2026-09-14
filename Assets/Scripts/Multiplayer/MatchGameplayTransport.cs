using System;
using System.Collections.Generic;
using System.IO;
using TurnBasedGame.Command;
using TurnBasedGame.Multiplayer.Protocol;
using Unity.Collections;
using Unity.Netcode;

namespace TurnBasedGame.Multiplayer
{
    // Session supplies the host MatchId and authenticates peer bindings before admitting commands.
    public sealed class MatchGameplayTransport : IDisposable
    {
        private const string CommandMessage = "match-command-v2", ResultMessage = "match-result-v2";
        private readonly NetworkManager network;
        private readonly Dictionary<ulong, PlayerId> players = new();
        private Action<MatchCommandResult> pending;
        private long pendingId, nextId;
        private ulong nextSequence = 1, serverSequence;
        private readonly string matchId;
        public PlayerId LocalPlayer { get; }
        public bool IsServer { get; }
        public event Action<CommandAcknowledgement> ResultReceived;

        public MatchGameplayTransport(NetworkManager network, PlayerId localPlayer, string matchId)
        {
            if (network == null || !network.IsListening || !MatchProtocol.IsPlayer(localPlayer) || !MatchProtocol.IsHex(matchId, 32))
                throw new ArgumentException("Active session, authenticated player and host MatchId required.");
            this.network = network;
            this.matchId = matchId;
            LocalPlayer = localPlayer;
            IsServer = network.IsServer;
            if (network.IsServer) network.CustomMessagingManager.RegisterNamedMessageHandler(CommandMessage, OnCommand);
            else network.CustomMessagingManager.RegisterNamedMessageHandler(ResultMessage, OnResult);
            network.OnClientDisconnectCallback += OnDisconnected;
            if (IsServer) LocalMatchAuthority.CommandCommitted += OnCommitted;
            LocalMatchAuthority.AttachTransport(this);
        }

        private void OnCommitted(CommandAcknowledgement result)
        {
            foreach (var peer in players) SendResult(peer.Key, peer.Value, result);
        }

        private void SendResult(ulong peer, PlayerId player, CommandAcknowledgement result)
        {
            // Copy before filtering so other recipients and replay cache retain their own visibility.
            var copy = MatchProtocol.DeserializeAcknowledgement(MatchProtocol.SerializeAcknowledgement(result));
            var visible = new List<MatchStateChange>();
            foreach (var change in copy.StateChanges)
                if (change.Kind != StateChangeKind.HandCard || change.Player == player) visible.Add(change);
            copy.StateChanges = visible.ToArray();
            SendBytes(ResultMessage, peer, MatchProtocol.SerializeAcknowledgement(copy));
        }

        public void BindAuthenticatedPeer(ulong clientId, PlayerId player)
        {
            if (!network.IsServer || !MatchProtocol.IsPlayer(player) || player == LocalPlayer)
                throw new InvalidOperationException("Only host can bind an authenticated remote player.");
            foreach (var pair in players)
                if (pair.Key != clientId && pair.Value == player) throw new InvalidOperationException("Player already bound.");
            players[clientId] = player;
        }

        internal MatchCommandDto Create(MatchCommandKind kind, PlayerId actor, int turn)
        {
            if (actor != LocalPlayer || pendingId != 0) return null;
            return new MatchCommandDto { Compatibility = LocalMatchAuthority.Compatibility, MatchId = matchId,
                Actor = actor, Kind = kind, ExpectedTurn = turn, CommandId = ++nextId,
                ClientSequence = nextSequence, AcknowledgedServerSequence = serverSequence };
        }

        internal MatchCommandResult Send(MatchCommandDto command, Action<MatchCommandResult> complete)
        {
            if (command == null || command.Actor != LocalPlayer || pendingId != 0)
                return MatchCommandResult.Failure("Command đang chờ hoặc actor không hợp lệ.");
            var bytes = MatchProtocol.Serialize(command);
            pendingId = command.CommandId;
            pending = complete;
            try { SendBytes(CommandMessage, NetworkManager.ServerClientId, bytes); }
            catch { pendingId = 0; pending = null; throw; }
            return MatchCommandResult.Waiting();
        }

        private void OnCommand(ulong sender, FastBufferReader reader)
        {
            if (!network.IsServer || !players.TryGetValue(sender, out var actor)) return;
            var bytes = ReadBytes(reader, MatchProtocol.MaxCommandBytes);
            if (bytes == null || !MatchProtocol.TryDeserialize(bytes, out _, out _)) return;
            var result = LocalMatchAuthority.SubmitBytes(bytes, actor).Acknowledgement;
            SendResult(sender, actor, result);
        }

        private void OnResult(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId) return;
            var bytes = ReadBytes(reader, 256 * 1024);
            if (bytes == null) return;
            CommandAcknowledgement result;
            try { result = MatchProtocol.DeserializeAcknowledgement(bytes); }
            catch (Exception e) when (e is IOException || e is ArgumentException) { return; }
            if (result.MatchId != matchId) return;
            if (result.Accepted && result.ServerSequence > serverSequence)
            {
                serverSequence = result.ServerSequence;
                ResultReceived?.Invoke(result);
            }
            if (result.Actor != LocalPlayer || result.CommandId != pendingId) return;
            var callback = pending;
            pending = null; pendingId = 0;
            nextSequence = result.NextClientSequence;
            serverSequence = Math.Max(serverSequence, result.ServerSequence);
            callback?.Invoke(new MatchCommandResult(result));
        }

        private void SendBytes(string message, ulong recipient, byte[] bytes)
        {
            using var writer = new FastBufferWriter(bytes.Length + sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(bytes.Length);
            writer.WriteBytesSafe(bytes);
            network.CustomMessagingManager.SendNamedMessage(message, recipient, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private static byte[] ReadBytes(FastBufferReader reader, int limit)
        {
            if (!reader.TryBeginRead(sizeof(int))) return null;
            reader.ReadValueSafe(out int length);
            if (length <= 0 || length > limit || reader.Length - reader.Position != length) return null;
            var bytes = new byte[length];
            reader.ReadBytesSafe(ref bytes, length);
            return bytes;
        }

        private void OnDisconnected(ulong peer)
        {
            players.Remove(peer);
            if (!network.IsServer) FailPending();
        }

        private void FailPending()
        {
            var callback = pending;
            pending = null; pendingId = 0;
            callback?.Invoke(MatchCommandResult.Failure("Kết nối trận đấu đã đóng."));
        }

        public void Dispose()
        {
            network.OnClientDisconnectCallback -= OnDisconnected;
            LocalMatchAuthority.CommandCommitted -= OnCommitted;
            network.CustomMessagingManager?.UnregisterNamedMessageHandler(IsServer ? CommandMessage : ResultMessage);
            players.Clear();
            FailPending();
        }
    }
}
