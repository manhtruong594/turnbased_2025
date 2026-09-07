using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using UnityEngine;

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

        private MatchCommandResult(bool succeeded, string failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static MatchCommandResult Success() => new MatchCommandResult(true, null);
        public static MatchCommandResult Failure(string reason) => new MatchCommandResult(false, reason);
    }

    public interface IMatchCommand
    {
        long CommandId { get; }
        PlayerID Actor { get; }
        int ExpectedTurn { get; }
        bool TryExecute(out string failureReason);
    }

    /// <summary>
    /// Authority local duy nhất cho gameplay command trước khi manager thay đổi state.
    /// </summary>
    public static class LocalMatchAuthority
    {
        private static readonly HashSet<long> ProcessedCommandIds = new HashSet<long>();
        private static long _nextCommandId;

        public static void Reset()
        {
            ProcessedCommandIds.Clear();
            _nextCommandId = 0;
        }

        public static MatchCommandResult Submit(IMatchCommand command)
        {
            if (command == null)
                return MatchCommandResult.Failure("Command không tồn tại.");
            if (command.CommandId <= 0)
                return MatchCommandResult.Failure("CommandId không hợp lệ.");
            if (!ProcessedCommandIds.Add(command.CommandId))
                return MatchCommandResult.Failure($"Command {command.CommandId} đã được xử lý.");

            var turnManager = TurnManager.Instance;
            if (turnManager == null)
                return MatchCommandResult.Failure("TurnManager chưa sẵn sàng.");
            if (turnManager.CurrentState == TurnState.Initialization ||
                turnManager.CurrentState == TurnState.GameEnd)
            {
                return MatchCommandResult.Failure("Trận đấu không nhận gameplay command ở state hiện tại.");
            }
            if (turnManager.CurrentPlayer != command.Actor)
                return MatchCommandResult.Failure("Command không thuộc người chơi đang có lượt.");
            if (turnManager.TurnCount != command.ExpectedTurn)
                return MatchCommandResult.Failure("Command thuộc lượt cũ hoặc lượt chưa bắt đầu.");

            return command.TryExecute(out var failureReason)
                ? MatchCommandResult.Success()
                : MatchCommandResult.Failure(failureReason);
        }

        public static MatchCommandResult SubmitEndTurn(PlayerID actor)
        {
            return Submit(new EndTurnMatchCommand(NextCommandId(), actor, CurrentTurn));
        }

        public static MatchCommandResult SubmitSpawn(
            PlayerID actor,
            UnitController unitPrefab,
            SpawnPoint spawnPoint = null)
        {
            return Submit(new SpawnUnitMatchCommand(
                NextCommandId(), actor, CurrentTurn, unitPrefab, spawnPoint));
        }

        public static MatchCommandResult SubmitMove(
            PlayerID actor,
            UnitController unit,
            Vector3Int destination,
            System.Action onComplete = null)
        {
            return Submit(new MoveUnitMatchCommand(
                NextCommandId(), actor, CurrentTurn, unit, destination, onComplete));
        }

        private static int CurrentTurn => TurnManager.Instance != null
            ? TurnManager.Instance.TurnCount
            : -1;

        private static long NextCommandId()
        {
            _nextCommandId++;
            return _nextCommandId;
        }
    }

    public sealed class EndTurnMatchCommand : IMatchCommand
    {
        public long CommandId { get; }
        public PlayerID Actor { get; }
        public int ExpectedTurn { get; }

        public EndTurnMatchCommand(long commandId, PlayerID actor, int expectedTurn)
        {
            CommandId = commandId;
            Actor = actor;
            ExpectedTurn = expectedTurn;
        }

        public bool TryExecute(out string failureReason)
        {
            return TurnManager.Instance.TryEndCurrentTurn(Actor, ExpectedTurn, out failureReason);
        }
    }

    public sealed class SpawnUnitMatchCommand : IMatchCommand
    {
        private readonly UnitController _unitPrefab;
        private readonly SpawnPoint _spawnPoint;

        public long CommandId { get; }
        public PlayerID Actor { get; }
        public int ExpectedTurn { get; }

        public SpawnUnitMatchCommand(
            long commandId,
            PlayerID actor,
            int expectedTurn,
            UnitController unitPrefab,
            SpawnPoint spawnPoint = null)
        {
            CommandId = commandId;
            Actor = actor;
            ExpectedTurn = expectedTurn;
            _unitPrefab = unitPrefab;
            _spawnPoint = spawnPoint;
        }

        public bool TryExecute(out string failureReason)
        {
            if (UnitSpawner.Instance == null)
            {
                failureReason = "UnitSpawner chưa sẵn sàng.";
                return false;
            }
            if (_unitPrefab == null || _unitPrefab.UnitData == null)
            {
                failureReason = "Unit prefab hoặc UnitData không hợp lệ.";
                return false;
            }
            if (MPManager.Instance == null ||
                !MPManager.Instance.HasEnoughMP(Actor, _unitPrefab.UnitData.spawnCost))
            {
                failureReason = "Không đủ MP để spawn unit.";
                return false;
            }

            return UnitSpawner.Instance.TrySpawnUnitAuthorized(
                _unitPrefab, Actor, _spawnPoint, out failureReason);
        }
    }

    public sealed class MoveUnitMatchCommand : IMatchCommand
    {
        private readonly UnitController _unit;
        private readonly Vector3Int _destination;
        private readonly System.Action _onComplete;

        public long CommandId { get; }
        public PlayerID Actor { get; }
        public int ExpectedTurn { get; }

        public MoveUnitMatchCommand(
            long commandId,
            PlayerID actor,
            int expectedTurn,
            UnitController unit,
            Vector3Int destination,
            System.Action onComplete = null)
        {
            CommandId = commandId;
            Actor = actor;
            ExpectedTurn = expectedTurn;
            _unit = unit;
            _destination = destination;
            _onComplete = onComplete;
        }

        public bool TryExecute(out string failureReason)
        {
            if (_unit == null || _unit.IsDead())
            {
                failureReason = "Unit không tồn tại hoặc đã chết.";
                return false;
            }
            if (_unit.GetOwner() != Actor)
            {
                failureReason = "Người chơi không sở hữu unit này.";
                return false;
            }
            if (!_unit.CanMove())
            {
                failureReason = "Unit không thể di chuyển ở trạng thái hiện tại.";
                return false;
            }

            var mapManager = MapManager.Instance;
            var map = mapManager != null ? mapManager.MapEntity : null;
            if (map == null || !mapManager.IsTileAvailable(_destination))
            {
                failureReason = "Tile đích không hợp lệ hoặc đã bị chiếm.";
                return false;
            }

            var path = map.PathTiles(
                _unit.transform.position,
                map.WorldPosition(_destination),
                _unit.GetMoveRange());
            if (path == null || path.Count == 0 || path[path.Count - 1].Position != _destination)
            {
                failureReason = "Không có đường đi hợp lệ tới tile đích.";
                return false;
            }

            return _unit.TryMoveAuthorized(path, _onComplete, out failureReason);
        }
    }
}
