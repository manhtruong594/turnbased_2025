using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using UnityEngine;
using TurnBasedGame.Multiplayer;
using TurnBasedGame.Multiplayer.Protocol;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;
using RedBjorn.ProtoTiles.Example;

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
        public bool Pending { get; }
        public string FailureReason { get; }
        public CommandReason Reason { get; }
        public ulong ServerSequence { get; }
        public CommandAcknowledgement Acknowledgement { get; }

        internal MatchCommandResult(CommandAcknowledgement acknowledgement)
        {
            Pending = false;
            Acknowledgement = acknowledgement;
            Succeeded = acknowledgement.Accepted;
            FailureReason = acknowledgement.Detail;
            Reason = acknowledgement.Reason;
            ServerSequence = acknowledgement.ServerSequence;
        }
        private MatchCommandResult(bool pending)
        {
            Pending = pending; Succeeded = false; FailureReason = null; Reason = CommandReason.None;
            ServerSequence = 0; Acknowledgement = null;
        }
        internal static MatchCommandResult Waiting() => new MatchCommandResult(true);
        public static MatchCommandResult Failure(string reason) => new MatchCommandResult(
            new CommandAcknowledgement { Reason = CommandReason.ExecutionRejected, Detail = reason });
    }

    /// <summary>Local adapter and authority executor. Only MatchCommandDto crosses the wire.</summary>
    public static class LocalMatchAuthority
    {
        private static MatchCommandGate gate;
        internal static ulong ServerSequence => gate?.ServerSequence ?? 0;
        public static MatchGameplayTransport Transport { get; private set; }
        public static event System.Action<CommandAcknowledgement> CommandCommitted;
        private static List<System.Action> presentation;
        internal static void PublishAfterCommit(System.Action action)
        {
            if (presentation != null) presentation.Add(action);
            else action();
        }

        private static void Present(List<System.Action> actions)
        {
            foreach (var action in actions)
                try { action(); } catch (System.Exception error) { Debug.LogException(error); }
        }
        public static void AttachTransport(MatchGameplayTransport transport)
        {
            Transport = transport;
            if (!IsAuthoritative)
                Content = new MatchContentRegistry(UnityEngine.Resources.Load<MatchContentCatalog>(MatchContentCatalog.ResourceName));
        }
        private static long nextCommandId;
        private static int diceTurn = -1;
        private static int usedDice;
        private static int aiManaTurn = -1;
        public static int LastDiceValue { get; private set; }
        public static bool IsAuthoritative => Transport != null ? Transport.IsServer :
            Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsListening ||
            Unity.Netcode.NetworkManager.Singleton.IsServer;
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
            diceTurn = -1;
            usedDice = 0;
            aiManaTurn = -1;
        }

        internal static void GrantLocalAIMana(PlayerID actor)
        {
            var network = Unity.Netcode.NetworkManager.Singleton;
            var turn = TurnManager.Instance;
            if ((network != null && network.IsListening) || turn == null || turn.CurrentPlayer != actor ||
                turn.IsTurnTransitionPending || aiManaTurn == turn.TurnCount) return;
            aiManaTurn = turn.TurnCount;
            MPManager.Instance.AddMP(actor, 3);
        }

        public static MatchCommandResult Submit(MatchCommandDto command, PlayerId authenticatedActor)
        {
            return SubmitLocal(command, authenticatedActor, null);
        }

        public static bool TryRollDice(PlayerID actor, int expectedTurn, int diceIndex, out int value)
        {
            value = 0;
            if (diceIndex < 1 || diceIndex > 2) return false;
            var turn = TurnManager.Instance;
            if (!IsAuthoritative || gate == null || MPManager.Instance == null || turn == null || turn.IsTurnTransitionPending ||
                turn.CurrentState == TurnState.Initialization || turn.CurrentState == TurnState.GameEnd ||
                turn.CurrentPlayer != actor || turn.TurnCount != expectedTurn) return false;
            var command = Create(MatchCommandKind.RollDice, actor);
            command.DiceIndex = (byte)diceIndex;
            var result = Submit(command, (PlayerId)actor);
            if (result.Succeeded) value = LastDiceValue;
            return result.Succeeded;
        }

        public static MatchCommandResult SubmitBytes(byte[] payload, PlayerId authenticatedActor)
        {
            if (!MatchProtocol.TryDeserialize(payload, out var command, out var reason))
                return MatchCommandResult.Failure(reason);
            return Submit(command, authenticatedActor);
        }

        private static MatchCommandResult SubmitLocal(MatchCommandDto command, PlayerId actor, System.Action onComplete,
            System.Action<MatchCommandResult> onResult = null)
        {
            if (!IsAuthoritative)
                return Transport != null ? Transport.Send(command, result =>
                {
                    if (result.Succeeded) onComplete?.Invoke();
                    else { Debug.LogWarning(result.FailureReason); AreaPathManager.Instance?.RealeaseSelectedUnit(); }
                    onResult?.Invoke(result);
                })
                    : MatchCommandResult.Failure("Transport gameplay chưa sẵn sàng.");
            if (gate == null) return MatchCommandResult.Failure("Match authority chưa sẵn sàng.");
            var before = CaptureState();
            var rollback = CaptureRollback();
            ulong previousSequence = gate.ServerSequence;
            var priorPresentation = presentation;
            var actions = new List<System.Action>();
            presentation = actions;
            MatchCommandResult result;
            try
            {
                result = new MatchCommandResult(gate.Submit(command, actor, dto => Execute(dto, onComplete),
                    () => CaptureChanges(before), () => command.Kind == MatchCommandKind.RollDice ? LastDiceValue : 0, rollback));
            }
            finally { presentation = priorPresentation; }
            if (result.Succeeded && gate.ServerSequence > previousSequence)
            {
                try { CommandCommitted?.Invoke(result.Acknowledgement); }
                catch (System.Exception error) { Debug.LogException(error); }
                Present(actions);
            }
            return result;
        }

        private static System.Action CaptureRollback()
        {
            var restore = new List<System.Action>();
            restore.Add(Runtime.CaptureRollback());
            if (UnitSpawner.Instance != null)
            {
                for (int player = 1; player <= 2; player++)
                    foreach (var unit in UnitSpawner.Instance.GetPlayerUnits((PlayerID)player))
                        if (unit != null) restore.Add(unit.CaptureRollback());
                foreach (var point in UnitSpawner.Instance.SpawnPoints) restore.Add(point.CaptureRollback());
                restore.Add(UnitSpawner.Instance.CaptureRollback());
            }
            if (MapManager.Instance != null) restore.Add(MapManager.Instance.CaptureRollback());
            if (MPManager.Instance != null) restore.Add(MPManager.Instance.CaptureRollback());
            if (SpellCardManager.Instance != null) restore.Add(SpellCardManager.Instance.CaptureRollback());
            if (TileHazardManager.Instance != null) restore.Add(TileHazardManager.Instance.CaptureRollback());
            if (TurnBasedGame.Capture.CapturePointManager.Instance != null)
                foreach (var point in TurnBasedGame.Capture.CapturePointManager.Instance.CapturePoints)
                    restore.Add(point.CaptureRollback());
            if (TurnManager.Instance != null) restore.Add(TurnManager.Instance.CaptureRollback());
            var rng = random.Capture();
            int rollTurn = diceTurn, dice = usedDice, lastRoll = LastDiceValue, aiTurn = aiManaTurn;
            return () =>
            {
                foreach (var action in restore) action();
                random.Restore(rng); diceTurn = rollTurn; usedDice = dice; LastDiceValue = lastRoll; aiManaTurn = aiTurn;
            };
        }

        private static List<MatchStateChange> CaptureState()
        {
            var changes = new List<MatchStateChange>();
            for (int index = 1; index <= 2; index++)
            {
                var player = (PlayerID)index;
                changes.Add(new MatchStateChange { Kind = StateChangeKind.MP, Player = (PlayerId)player,
                    Value = MPManager.Instance != null ? MPManager.Instance.GetCurrentMP(player) : 0 });
                if (UnitSpawner.Instance == null) continue;
                foreach (var unit in UnitSpawner.Instance.GetPlayerUnits(player))
                {
                    if (unit == null || unit.IsDead()) continue;
                    var pos = unit.currentGridPosition;
                    changes.Add(new MatchStateChange { Kind = StateChangeKind.Unit, Player = (PlayerId)player,
                        Entity = unit.UnitRuntimeId, ContentId = unit.UnitContentId, Position = new GridCoordinate(pos.x, pos.y, pos.z),
                        Value = unit.GetCurrentHealth(), Value2 = unit.IsMoveDone() ? 1 : 0, Value3 = unit.IsActionCommitted ? 1 : 0 });
                    foreach (var entry in unit.AttackComponent.ActiveSkills)
                        if (entry is SkillBase skill)
                            changes.Add(new MatchStateChange { Kind = StateChangeKind.Cooldown, Entity = unit.UnitRuntimeId,
                                ContentId = skill.SkillContentId, Player = (PlayerId)player, Value = skill.CurrentCooldown });
                    if (unit.BuffHandler == null) continue;
                    foreach (var status in unit.BuffHandler.ActiveEffects)
                        changes.Add(new MatchStateChange { Kind = StateChangeKind.Status, Entity = unit.UnitRuntimeId,
                            Player = (PlayerId)status.SourcePlayer, Value = (int)status.Type, Value2 = status.Value,
                            Value3 = status.RemainingTurns, Value4 = status.InitialDuration });
                }
            }
            SpellCardManager.Instance?.AppendHandState(changes);
            if (TileHazardManager.Instance != null)
                foreach (var hazard in TileHazardManager.Instance.Hazards)
                    changes.Add(new MatchStateChange { Kind = StateChangeKind.Hazard, Player = (PlayerId)hazard.Owner,
                        Position = new GridCoordinate(hazard.Position.x, hazard.Position.y, hazard.Position.z),
                        Value = (int)hazard.Type, Value2 = hazard.Value, Value3 = hazard.RemainingTurns,
                        Value4 = hazard.StatusDuration, Scalar = hazard.ApplyChance });
            if (UnitSpawner.Instance != null)
                foreach (var point in UnitSpawner.Instance.SpawnPoints)
                    changes.Add(new MatchStateChange { Kind = StateChangeKind.SpawnPoint, Player = (PlayerId)point.Owner,
                        ContentId = Runtime.GetSpawnPointId(point), Value = point.IsAvailable ? 1 : 0 });
            var capture = TurnBasedGame.Capture.CapturePointManager.Instance;
            if (capture != null)
                foreach (var point in capture.CapturePoints)
                {
                    var pos = point.GridPosition;
                    changes.Add(new MatchStateChange { Kind = StateChangeKind.Capture,
                        Player = point.Owner.HasValue ? (PlayerId)point.Owner.Value : 0,
                        Position = new GridCoordinate(pos.x, pos.y, pos.z) });
                }
            var turn = TurnManager.Instance;
            changes.Add(new MatchStateChange { Kind = StateChangeKind.Dice, Value = diceTurn, Value2 = usedDice });
            if (turn != null) changes.Add(new MatchStateChange { Kind = StateChangeKind.Turn,
                Player = (PlayerId)turn.CurrentPlayer, Value = turn.TurnCount, Value2 = (int)turn.CurrentState,
                Value3 = turn.Winner.HasValue ? (int)turn.Winner.Value : 0 });
            if (random != null)
            {
                var rng = random.Capture();
                changes.Add(new MatchStateChange { Kind = StateChangeKind.Random,
                    Entity = rng.Sequence, Value = unchecked((int)rng.Seed), Value2 = unchecked((int)rng.State) });
            }
            return changes;
        }

        private static MatchStateChange[] CaptureChanges(List<MatchStateChange> before)
        {
            var after = CaptureState();
            // Result carries complete post-command collections; explicit tombstones identify removed units.
            foreach (var previous in before)
            {
                if (previous.Kind != StateChangeKind.Unit) continue;
                bool found = false;
                foreach (var current in after)
                    if (current.Kind == StateChangeKind.Unit && current.Entity == previous.Entity) { found = true; break; }
                if (!found) after.Add(new MatchStateChange { Kind = StateChangeKind.RemovedUnit, Entity = previous.Entity, Player = previous.Player });
            }
            if (after.Count > 2048) throw new System.InvalidOperationException("Command result exceeds state limit.");
            return after.ToArray();
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
            if (UnitSpawner.Instance != null)
                for (int playerIndex = 1; playerIndex <= 2; playerIndex++)
                    foreach (var movingUnit in UnitSpawner.Instance.GetPlayerUnits((PlayerID)playerIndex))
                        if (movingUnit != null && movingUnit.IsMoving)
                            return (CommandReason.InvalidState, "Đang hoàn tất di chuyển.");
            string reason;
            bool success;
            switch (command.Kind)
            {
                case MatchCommandKind.NormalAttack:
                case MatchCommandKind.UseSkill:
                    var source = Runtime.ResolveUnit(command.UnitRuntimeId);
                    if (source == null || source.GetOwner() != actor)
                        return (CommandReason.InvalidOwner, "Source không thuộc actor.");
                    if (!source.CanAct() || source.IsMoving)
                        return (CommandReason.InvalidState, "Unit không thể hành động.");
                    var skill = source.AttackComponent.ResolveSkill(command.SkillContentId);
                    if (skill == null || skill.Type == SkillType.Passive ||
                        (skill.Type == SkillType.Normal) != (command.Kind == MatchCommandKind.NormalAttack))
                        return (CommandReason.InvalidPayload, "Skill không thuộc unit hoặc sai loại command.");
                    var target = new Vector3Int(command.Destination.X, command.Destination.Y, command.Destination.Z);
                    if (MapManager.Instance?.MapEntity?.Tile(target) == null)
                        return (CommandReason.InvalidTarget, "Target tile không tồn tại.");
                    if (skill.CurrentCooldown > 0 && skill.Type != SkillType.Normal)
                        return (CommandReason.Cooldown, "Skill đang cooldown.");
                    if (MapManager.Instance.GetDistance(source.currentGridPosition, target) > skill.Range)
                        return (CommandReason.OutOfRange, "Target ngoài range.");
                    if (!source.AttackComponent.HasLineOfSightFrom(source.currentGridPosition, target))
                        return (CommandReason.BlockedLineOfSight, "Target bị che khuất.");
                    if (skill.MPCost < 0 || MPManager.Instance == null || !MPManager.Instance.HasEnoughMP(actor, skill.MPCost))
                        return (CommandReason.InsufficientMP, "Không đủ MP.");
                    if (!skill.CanUse(source, target)) return (CommandReason.InvalidTarget, "Target hoặc điều kiện skill không hợp lệ.");
                    success = skill.ExecuteAuthorized(source, target);
                    reason = success ? null : "Skill bị từ chối.";
                    break;
                case MatchCommandKind.CastSpell:
                    if (SpellCardManager.Instance == null)
                        return (CommandReason.InvalidState, "Spell manager chưa sẵn sàng.");
                    return SpellCardManager.Instance.CastAuthorized(actor, command.CardInstanceId,
                        new Vector3Int(command.Destination.X, command.Destination.Y, command.Destination.Z));
                case MatchCommandKind.FinishUnit:
                case MatchCommandKind.UndoMove:
                    var actionUnit = Runtime.ResolveUnit(command.UnitRuntimeId);
                    if (actionUnit == null || actionUnit.GetOwner() != actor)
                        return (CommandReason.InvalidOwner, "Unit không thuộc actor.");
                    if (!actionUnit.CanAct() || actionUnit.IsMoving)
                        return (CommandReason.InvalidState, "Unit không thể hành động.");
                    if (command.Kind == MatchCommandKind.UndoMove)
                    {
                        success = actionUnit.TryUndoMoveAuthorized(out reason);
                        break;
                    }
                    actionUnit.FinishTurnActionsAuthorized();
                    return (CommandReason.None, null);
                case MatchCommandKind.RollDice:
                    if (MPManager.Instance == null) return (CommandReason.InvalidState, "MP manager chưa sẵn sàng.");
                    if (diceTurn != turn.TurnCount) { diceTurn = turn.TurnCount; usedDice = 0; }
                    int bit = 1 << command.DiceIndex;
                    if ((usedDice & bit) != 0) return (CommandReason.InvalidState, "Xúc xắc đã dùng trong lượt này.");
                    usedDice |= bit;
                    LastDiceValue = Random.Range(1, 7);
                    MPManager.Instance.AddMP(actor, LastDiceValue);
                    return (CommandReason.None, null);
                case MatchCommandKind.EndTurn:
                    success = turn.TryEndCurrentTurn(actor, command.ExpectedTurn, out reason);
                    break;
                case MatchCommandKind.SpawnUnit:
                    var prefab = Content.ResolveUnitPrefab(command.UnitContentId);
                    if (prefab == null || prefab.UnitData == null || UnitSpawner.Instance == null)
                        return (CommandReason.InvalidPayload, "UnitContentId không tồn tại hoặc spawner chưa sẵn sàng.");
                    SpawnPoint point = null;
                    if (!string.IsNullOrEmpty(command.SpawnPointId))
                    {
                        point = Runtime.ResolveSpawnPoint(command.SpawnPointId);
                        if (point == null) return (CommandReason.InvalidTarget, "SpawnPointId không tồn tại.");
                    }
                    if (point != null && point.Owner != actor)
                        return (CommandReason.InvalidOwner, "Spawn point không thuộc actor.");
                    if (prefab.UnitData.spawnCost < 0) return (CommandReason.InvalidPayload, "Spawn cost không hợp lệ.");
                    if (MPManager.Instance == null || !MPManager.Instance.HasEnoughMP(actor, prefab.UnitData.spawnCost))
                        return (CommandReason.InsufficientMP, "Không đủ MP để spawn unit.");
                    success = UnitSpawner.Instance.TrySpawnUnitAuthorized(prefab, actor, point, out reason);
                    break;
                case MatchCommandKind.MoveUnit:
                    var unit = Runtime.ResolveUnit(command.UnitRuntimeId);
                    if (unit == null || unit.GetOwner() != actor)
                        return (CommandReason.InvalidOwner, "Unit không thuộc actor.");
                    if (unit == null || unit.IsDead() || unit.GetOwner() != actor || !unit.CanMove())
                        return (CommandReason.ExecutionRejected, "Unit không tồn tại, sai owner hoặc không thể di chuyển.");
                    var destination = new Vector3Int(command.Destination.X, command.Destination.Y, command.Destination.Z);
                    var manager = MapManager.Instance;
                    var map = manager != null ? manager.MapEntity : null;
                    if (map == null || !manager.IsTileAvailable(destination))
                        return (CommandReason.InvalidTarget, "Tile đích không hợp lệ hoặc đã bị chiếm.");
                    var path = map.PathTiles(unit.transform.position, map.WorldPosition(destination), unit.GetMoveRange());
                    if (path == null || path.Count == 0 || path[path.Count - 1].Position != destination)
                        return (CommandReason.OutOfRange, "Không có đường đi hợp lệ tới tile đích.");
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

        internal static MatchCommandResult SubmitTimeoutTurn()
        {
            var turn = TurnManager.Instance;
            if (!IsAuthoritative || gate == null || turn == null || turn.Timer > 0) return MatchCommandResult.Failure("Turn chưa timeout.");
            var command = new MatchCommandDto { Kind = MatchCommandKind.EndTurn, Actor = (PlayerId)turn.CurrentPlayer,
                ExpectedTurn = turn.TurnCount };
            var before = CaptureState();
            var priorPresentation = presentation;
            var actions = new List<System.Action>();
            presentation = actions;
            MatchCommandResult result;
            try
            {
                result = new MatchCommandResult(gate.ExecuteSystem(command.Actor, () => Execute(command, null),
                    () => CaptureChanges(before), CaptureRollback()));
            }
            finally { presentation = priorPresentation; }
            if (result.Succeeded)
            {
                try { CommandCommitted?.Invoke(result.Acknowledgement); }
                catch (System.Exception error) { Debug.LogException(error); }
                Present(actions);
            }
            return result;
        }

        public static MatchCommandResult SubmitSkill(UnitController source, SkillBase skill, Vector3Int target,
            System.Action<MatchCommandResult> onResult = null)
        {
            if (source == null || skill == null) return MatchCommandResult.Failure("Source/skill không hợp lệ.");
            var command = Create(skill.Type == SkillType.Normal ? MatchCommandKind.NormalAttack : MatchCommandKind.UseSkill, source.GetOwner());
            if (command == null) return MatchCommandResult.Failure("Authority chưa sẵn sàng.");
            command.UnitRuntimeId = Runtime.GetUnitId(source);
            command.SkillContentId = skill.SkillContentId;
            command.Destination = new GridCoordinate(target.x, target.y, target.z);
            return SubmitLocal(command, (PlayerId)source.GetOwner(), null, onResult);
        }

        public static MatchCommandResult SubmitUnitAction(UnitController unit, bool undo = false)
        {
            if (unit == null) return MatchCommandResult.Failure("Unit không tồn tại.");
            var command = Create(undo ? MatchCommandKind.UndoMove : MatchCommandKind.FinishUnit, unit.GetOwner());
            if (command == null) return MatchCommandResult.Failure("Authority chưa sẵn sàng.");
            command.UnitRuntimeId = Runtime.GetUnitId(unit);
            return Submit(command, (PlayerId)unit.GetOwner());
        }

        public static MatchCommandResult SubmitSpell(PlayerID actor, ulong cardId, Vector3Int target,
            System.Action<MatchCommandResult> onResult = null)
        {
            var command = Create(MatchCommandKind.CastSpell, actor);
            if (command == null) return MatchCommandResult.Failure("Authority chưa sẵn sàng.");
            command.CardInstanceId = cardId;
            command.Destination = new GridCoordinate(target.x, target.y, target.z);
            return SubmitLocal(command, (PlayerId)actor, null, onResult);
        }

        public static MatchCommandResult SubmitRoll(PlayerID actor, int expectedTurn, int diceIndex,
            System.Action<MatchCommandResult> onResult = null)
        {
            if (diceIndex < 1 || diceIndex > 2) return MatchCommandResult.Failure("Dice index không hợp lệ.");
            var command = Create(MatchCommandKind.RollDice, actor);
            if (command == null) return MatchCommandResult.Failure("Authority chưa sẵn sàng hoặc có command đang chờ.");
            command.DiceIndex = (byte)diceIndex;
            command.ExpectedTurn = expectedTurn;
            return SubmitLocal(command, (PlayerId)actor, null, onResult);
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
            if (Transport != null && Transport.LocalPlayer != (PlayerId)actor) return null;
            if (!IsAuthoritative)
                return Transport?.Create(kind, (PlayerId)actor, TurnManager.Instance != null ? TurnManager.Instance.TurnCount : 0);
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
