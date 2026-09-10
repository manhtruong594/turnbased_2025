using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using UnityEngine;
using TurnBasedGame.Multiplayer;
using TurnBasedGame.Multiplayer.Protocol;

namespace TurnBasedGame.Command
{
    public class CommandInvoker
    {
        private readonly Stack<ICommand> _commandHistory = new Stack<ICommand>();

        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _commandHistory.Push(command);
        }

        public void UndoLastCommand()
        {
            if (_commandHistory.Count > 0)
            {
                var command = _commandHistory.Pop();
                command.Undo();
            }
        }
    }
    
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    public readonly struct MatchCommandResult
    {
        public bool Succeeded { get; }
        public string FailureReason { get; }
        public CommandReason Reason { get; }
        public ulong ServerSequence { get; }
        public CommandAcknowledgement Acknowledgement { get; }

        internal MatchCommandResult(CommandAcknowledgement acknowledgement)
        {
            Acknowledgement = acknowledgement;
            Succeeded = acknowledgement.Accepted;
            FailureReason = acknowledgement.Detail;
            Reason = acknowledgement.Reason;
            ServerSequence = acknowledgement.ServerSequence;
        }
        public static MatchCommandResult Failure(string reason) => new MatchCommandResult(
            new CommandAcknowledgement { Reason = CommandReason.ExecutionRejected, Detail = reason });
    }

    /// <summary>Local adapter and authority executor. Only MatchCommandDto crosses the wire.</summary>
    public static class LocalMatchAuthority
    {
        private static MatchCommandGate gate;
        private static long nextCommandId;
        public static string MatchId { get; private set; }
        public static MatchContentRegistry Content { get; private set; }
        public static MatchRuntimeRegistry Runtime { get; private set; } = new MatchRuntimeRegistry();
        private static MatchRandom random;
        public static MatchRandom Random => random ?? throw new System.InvalidOperationException("Match RNG is not initialized.");
        public static MatchCompatibility Compatibility => new MatchCompatibility { ContentCatalogHash = Content?.ContentCatalogHash };

        public static void Reset()
        {
            var catalog = UnityEngine.Resources.Load<MatchContentCatalog>(MatchContentCatalog.ResourceName);
            Content = new MatchContentRegistry(catalog);
            MatchId = System.Guid.NewGuid().ToString("N");
            uint seed = System.BitConverter.ToUInt32(System.Guid.NewGuid().ToByteArray(), 0);
            random = new MatchRandom(seed == 0 ? 1u : seed);
            Runtime = new MatchRuntimeRegistry();
            gate = new MatchCommandGate(MatchId, Compatibility);
            nextCommandId = 0;
        }

        public static MatchCommandResult Submit(MatchCommandDto command, PlayerId authenticatedActor)
        {
            return SubmitLocal(command, authenticatedActor, null);
        }

        public static bool TryRollDice(PlayerID actor, int expectedTurn, out int value)
        {
            value = 0;
            var turn = TurnManager.Instance;
            if (gate == null || MPManager.Instance == null || turn == null || turn.IsTurnTransitionPending ||
                turn.CurrentState == TurnState.Initialization || turn.CurrentState == TurnState.GameEnd ||
                turn.CurrentPlayer != actor || turn.TurnCount != expectedTurn) return false;
            value = Random.Range(1, 7);
            return true;
        }

        public static MatchCommandResult SubmitBytes(byte[] payload, PlayerId authenticatedActor)
        {
            if (!MatchProtocol.TryDeserialize(payload, out var command, out var reason))
                return MatchCommandResult.Failure(reason);
            return Submit(command, authenticatedActor);
        }

        private static MatchCommandResult SubmitLocal(MatchCommandDto command, PlayerId actor, System.Action onComplete)
        {
            if (gate == null) return MatchCommandResult.Failure("Match authority chưa sẵn sàng.");
            return new MatchCommandResult(gate.Submit(command, actor, dto => Execute(dto, onComplete)));
        }

        private static (CommandReason, string) Execute(MatchCommandDto command, System.Action onComplete)
        {
            var turn = TurnManager.Instance;
            if (turn == null || turn.CurrentState == TurnState.Initialization ||
                turn.CurrentState == TurnState.GameEnd || turn.IsTurnTransitionPending)
                return (CommandReason.InvalidState, "Trận đấu không nhận command ở state hiện tại.");
            var actor = (PlayerID)command.Actor;
            if (turn.CurrentPlayer != actor || turn.TurnCount != command.ExpectedTurn)
                return (CommandReason.WrongTurn, "Command không khớp người chơi/lượt hiện tại.");
            string reason;
            bool success;
            switch (command.Kind)
            {
                case MatchCommandKind.EndTurn:
                    success = turn.TryEndCurrentTurn(actor, command.ExpectedTurn, out reason);
                    break;
                case MatchCommandKind.SpawnUnit:
                    var prefab = Content.ResolveUnitPrefab(command.UnitContentId);
                    if (prefab == null || prefab.UnitData == null || UnitSpawner.Instance == null)
                        return (CommandReason.ExecutionRejected, "UnitContentId không tồn tại hoặc spawner chưa sẵn sàng.");
                    SpawnPoint point = null;
                    if (!string.IsNullOrEmpty(command.SpawnPointId))
                    {
                        point = Runtime.ResolveSpawnPoint(command.SpawnPointId);
                        if (point == null) return (CommandReason.ExecutionRejected, "SpawnPointId không tồn tại.");
                    }
                    if (MPManager.Instance == null || !MPManager.Instance.HasEnoughMP(actor, prefab.UnitData.spawnCost))
                        return (CommandReason.ExecutionRejected, "Không đủ MP để spawn unit.");
                    success = UnitSpawner.Instance.TrySpawnUnitAuthorized(prefab, actor, point, out reason);
                    break;
                case MatchCommandKind.MoveUnit:
                    var unit = Runtime.ResolveUnit(command.UnitRuntimeId);
                    if (unit == null || unit.IsDead() || unit.GetOwner() != actor || !unit.CanMove())
                        return (CommandReason.ExecutionRejected, "Unit không tồn tại, sai owner hoặc không thể di chuyển.");
                    var destination = new Vector3Int(command.Destination.X, command.Destination.Y, command.Destination.Z);
                    var manager = MapManager.Instance;
                    var map = manager != null ? manager.MapEntity : null;
                    if (map == null || !manager.IsTileAvailable(destination))
                        return (CommandReason.ExecutionRejected, "Tile đích không hợp lệ hoặc đã bị chiếm.");
                    var path = map.PathTiles(unit.transform.position, map.WorldPosition(destination), unit.GetMoveRange());
                    if (path == null || path.Count == 0 || path[path.Count - 1].Position != destination)
                        return (CommandReason.ExecutionRejected, "Không có đường đi hợp lệ tới tile đích.");
                    success = unit.TryMoveAuthorized(path, onComplete, out reason);
                    break;
                default: return (CommandReason.InvalidPayload, "Command không được hỗ trợ.");
            }
            return (success ? CommandReason.None : CommandReason.ExecutionRejected, reason);
        }

        public static MatchCommandResult SubmitEndTurn(PlayerID actor)
        {
            return SubmitLocal(Create(MatchCommandKind.EndTurn, actor), (PlayerId)actor, null);
        }

        public static MatchCommandResult SubmitSpawn(PlayerID actor, UnitController unitPrefab, SpawnPoint spawnPoint = null)
        {
            string contentId = Content?.GetId(unitPrefab);
            if (contentId == null) return MatchCommandResult.Failure("Unit prefab chưa đăng ký trong content catalog.");
            string pointId = Runtime.GetSpawnPointId(spawnPoint);
            if (spawnPoint != null && pointId == null) return MatchCommandResult.Failure("Spawn point chưa đăng ký.");
            var command = Create(MatchCommandKind.SpawnUnit, actor);
            if (command == null) return MatchCommandResult.Failure("Actor hoặc authority không hợp lệ.");
            command.UnitContentId = contentId;
            command.SpawnPointId = pointId;
            return SubmitLocal(command, (PlayerId)actor, null);
        }

        public static MatchCommandResult SubmitMove(PlayerID actor, UnitController unit, Vector3Int destination,
            System.Action onComplete = null)
        {
            ulong runtimeId = Runtime.GetUnitId(unit);
            if (runtimeId == 0) return MatchCommandResult.Failure("Unit chưa có UnitRuntimeId.");
            var command = Create(MatchCommandKind.MoveUnit, actor);
            if (command == null) return MatchCommandResult.Failure("Actor hoặc authority không hợp lệ.");
            command.UnitRuntimeId = runtimeId;
            command.Destination = new GridCoordinate(destination.x, destination.y, destination.z);
            return SubmitLocal(command, (PlayerId)actor, onComplete);
        }

        private static MatchCommandDto Create(MatchCommandKind kind, PlayerID actor)
        {
            if (gate == null || (actor != PlayerID.Player1 && actor != PlayerID.Player2)) return null;
            return new MatchCommandDto
            {
                Compatibility = Compatibility, MatchId = MatchId, CommandId = ++nextCommandId,
                Actor = (PlayerId)actor, ExpectedTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnCount : 0,
                ClientSequence = gate.NextClientSequence((PlayerId)actor), AcknowledgedServerSequence = gate.ServerSequence, Kind = kind
            };
        }
    }
}
