using System;
using System.Linq;
using NUnit.Framework;
using TurnBasedGame.Multiplayer.Protocol;

public sealed class MatchProtocolTests
{
    private const string MatchId = "0123456789abcdef0123456789abcdef";
    private static MatchCompatibility Compatible() => new MatchCompatibility { ContentCatalogHash = new string('a', 64) };
    private static MatchCommandDto Command(MatchCommandKind kind = MatchCommandKind.EndTurn) => new MatchCommandDto
    {
        Compatibility = Compatible(), MatchId = MatchId, Actor = PlayerId.Player1,
        CommandId = 1, ExpectedTurn = 3, ClientSequence = 1, Kind = kind,
        UnitContentId = kind == MatchCommandKind.SpawnUnit ? new string('b', 32) : null,
        SpawnPointId = kind == MatchCommandKind.SpawnUnit ? "1:-3:0:8" : null,
        UnitRuntimeId = kind == MatchCommandKind.MoveUnit || kind == MatchCommandKind.NormalAttack ||
            kind == MatchCommandKind.UseSkill || kind == MatchCommandKind.FinishUnit || kind == MatchCommandKind.UndoMove ? 42ul : 0,
        SkillContentId = kind == MatchCommandKind.NormalAttack || kind == MatchCommandKind.UseSkill ? new string('c', 32) : null,
        CardInstanceId = kind == MatchCommandKind.CastSpell ? 77ul : 0,
        DiceIndex = kind == MatchCommandKind.RollDice ? (byte)2 : (byte)0,
        Destination = kind == MatchCommandKind.MoveUnit || kind == MatchCommandKind.NormalAttack ||
            kind == MatchCommandKind.UseSkill || kind == MatchCommandKind.CastSpell ? new GridCoordinate(-4, 0, 123) : default
    };

    [Test]
    public void HostTimeoutDoesNotConsumeRemoteCommandIdentityOrSequence()
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        var timeout = gate.ExecuteSystem(PlayerId.Player1, () => (CommandReason.None, null),
            () => Array.Empty<MatchStateChange>(), null);
        Assert.That(timeout.CommandId, Is.Zero);
        Assert.That(timeout.ServerSequence, Is.EqualTo(1));
        Assert.That(gate.NextClientSequence(PlayerId.Player1), Is.EqualTo(1));
        Assert.That(gate.Submit(Command(), PlayerId.Player1, _ => (CommandReason.None, null)).ServerSequence, Is.EqualTo(2));
    }

    [TestCase(MatchCommandKind.EndTurn)]
    [TestCase(MatchCommandKind.SpawnUnit)]
    [TestCase(MatchCommandKind.MoveUnit)]
    [TestCase(MatchCommandKind.NormalAttack)]
    [TestCase(MatchCommandKind.UseSkill)]
    [TestCase(MatchCommandKind.CastSpell)]
    [TestCase(MatchCommandKind.FinishUnit)]
    [TestCase(MatchCommandKind.UndoMove)]
    [TestCase(MatchCommandKind.RollDice)]
    public void EveryCommandRoundTripsWithoutLosingFields(MatchCommandKind kind)
    {
        var original = Command(kind);
        var bytes = MatchProtocol.Serialize(original);
        Assert.That(bytes.Length, Is.LessThanOrEqualTo(MatchProtocol.MaxCommandBytes));
        Assert.That(MatchProtocol.TryDeserialize(bytes, out var restored, out _), Is.True);
        CollectionAssert.AreEqual(bytes, MatchProtocol.Serialize(restored));
        Assert.That(restored.MatchId, Is.EqualTo(original.MatchId));
        Assert.That(restored.CommandId, Is.EqualTo(original.CommandId));
        Assert.That(restored.UnitRuntimeId, Is.EqualTo(original.UnitRuntimeId));
        Assert.That(restored.Destination.X, Is.EqualTo(original.Destination.X));
        Assert.That(restored.UnitContentId, Is.EqualTo(original.UnitContentId));
        Assert.That(restored.SpawnPointId, Is.EqualTo(original.SpawnPointId));
        Assert.That(restored.SkillContentId, Is.EqualTo(original.SkillContentId));
        Assert.That(restored.CardInstanceId, Is.EqualTo(original.CardInstanceId));
        Assert.That(restored.DiceIndex, Is.EqualTo(original.DiceIndex));
    }

    [TestCase(MatchCommandKind.NormalAttack)]
    [TestCase(MatchCommandKind.UseSkill)]
    [TestCase(MatchCommandKind.CastSpell)]
    [TestCase(MatchCommandKind.FinishUnit)]
    [TestCase(MatchCommandKind.UndoMove)]
    [TestCase(MatchCommandKind.RollDice)]
    public void NewCommandsRejectEveryTruncationAndTrailingByte(MatchCommandKind kind)
    {
        var bytes = MatchProtocol.Serialize(Command(kind));
        for (int size = 0; size < bytes.Length; size++)
            Assert.That(MatchProtocol.TryDeserialize(bytes.Take(size).ToArray(), out _, out _), Is.False);
        Assert.That(MatchProtocol.TryDeserialize(bytes.Concat(new byte[] { 0 }).ToArray(), out _, out _), Is.False);
    }

    [TestCase(MatchCommandKind.EndTurn)]
    [TestCase(MatchCommandKind.SpawnUnit)]
    [TestCase(MatchCommandKind.MoveUnit)]
    [TestCase(MatchCommandKind.NormalAttack)]
    [TestCase(MatchCommandKind.UseSkill)]
    [TestCase(MatchCommandKind.CastSpell)]
    [TestCase(MatchCommandKind.FinishUnit)]
    [TestCase(MatchCommandKind.UndoMove)]
    [TestCase(MatchCommandKind.RollDice)]
    public void EveryGameplayCommandExecutesOnceAndCachesItsState(MatchCommandKind kind)
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        var command = Command(kind);
        int executions = 0;
        var first = gate.Submit(command, PlayerId.Player1, _ => { executions++; return (CommandReason.None, null); },
            () => new[] { new MatchStateChange { Kind = StateChangeKind.MP, Player = PlayerId.Player1, Value = 7 } }, () => 4);
        first.StateChanges[0].Value = 999;
        var retry = gate.Submit(command, PlayerId.Player1, _ => throw new Exception("Replay executed."));
        Assert.That(executions, Is.EqualTo(1));
        Assert.That(retry.StateChanges[0].Value, Is.EqualTo(7));
        Assert.That(retry.DiceValue, Is.EqualTo(4));
        Assert.That(retry.NextClientSequence, Is.EqualTo(2));
    }

    [TestCase(CommandReason.InvalidOwner)]
    [TestCase(CommandReason.WrongTurn)]
    [TestCase(CommandReason.InvalidTarget)]
    [TestCase(CommandReason.OutOfRange)]
    [TestCase(CommandReason.BlockedLineOfSight)]
    [TestCase(CommandReason.InsufficientMP)]
    [TestCase(CommandReason.Cooldown)]
    [TestCase(CommandReason.InvalidCard)]
    [TestCase(CommandReason.InvalidState)]
    public void GameplayRejectHasNoStateChangesAndRetryKeepsReason(CommandReason reason)
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        var command = Command(MatchCommandKind.UseSkill);
        var result = gate.Submit(command, PlayerId.Player1, _ => (reason, "Rejected"),
            () => throw new Exception("Rejected command captured state."));
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.StateChanges, Is.Empty);
        Assert.That(result.ServerSequence, Is.Zero);
        Assert.That(gate.Submit(command, PlayerId.Player1, _ => throw new Exception()).Reason, Is.EqualTo(reason));
    }

    [Test]
    public void FaultedExecutionStopsFurtherCommandsAndNeverRetriesMutation()
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        Assert.That(gate.Submit(Command(), PlayerId.Player1,
            _ => throw new InvalidOperationException("Fault")).Reason, Is.EqualTo(CommandReason.ExecutionFault));
        Assert.That(gate.Submit(Command(), PlayerId.Player1, _ => throw new Exception("Retried fault")).Accepted, Is.False);
        var next = Command(); next.CommandId = 2; next.ClientSequence = 2;
        Assert.That(gate.Submit(next, PlayerId.Player1, _ => throw new Exception("Continued faulted match")).Accepted, Is.False);
    }

    [Test]
    public void StateResultRoundTripsAllKindsAndRejectsTruncatedData()
    {
        var result = new CommandAcknowledgement { MatchId = MatchId, Actor = PlayerId.Player1,
            Accepted = true, NextClientSequence = 12, DiceValue = 6,
            StateChanges = Enum.GetValues(typeof(StateChangeKind)).Cast<StateChangeKind>().Select(kind =>
                new MatchStateChange { Kind = kind, Entity = 123, Player = PlayerId.Player2, ContentId = new string('a', 32),
                    Position = new GridCoordinate(-1, 0, 4), Value = 7, Value2 = 8, Value3 = 9, Value4 = 10, Scalar = 0.25f }).ToArray() };
        var bytes = MatchProtocol.SerializeAcknowledgement(result);
        var restored = MatchProtocol.DeserializeAcknowledgement(bytes);
        CollectionAssert.AreEqual(bytes, MatchProtocol.SerializeAcknowledgement(restored));
        Assert.That(restored.NextClientSequence, Is.EqualTo(12));
        Assert.That(restored.StateChanges.Length, Is.EqualTo(result.StateChanges.Length));
        Assert.Catch(() => MatchProtocol.DeserializeAcknowledgement(bytes.Take(bytes.Length - 1).ToArray()));
    }

    [Test]
    public void FaultRollsBackBeforeAcknowledgementAndDoesNotPublishPartialState()
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        int mp = 10, hp = 20, cards = 1, rollbacks = 0;
        var result = gate.Submit(Command(MatchCommandKind.CastSpell), PlayerId.Player1, _ =>
        {
            mp -= 3; hp -= 10; cards--;
            throw new InvalidOperationException("Effect failed after partial mutation.");
        }, rollback: () => { mp = 10; hp = 20; cards = 1; rollbacks++; });
        Assert.That(result.Reason, Is.EqualTo(CommandReason.ExecutionFault));
        Assert.That(result.StateChanges, Is.Empty);
        Assert.That(result.ServerSequence, Is.Zero);
        Assert.That(new[] { mp, hp, cards, rollbacks }, Is.EqualTo(new[] { 10, 20, 1, 1 }));
        gate.Submit(Command(MatchCommandKind.CastSpell), PlayerId.Player1, _ => throw new Exception("Replay"));
        Assert.That(rollbacks, Is.EqualTo(1));
    }

    [Test]
    public void NewCommandsRejectMissingIdentityAndSmuggledFields()
    {
        var c = Command(MatchCommandKind.UseSkill); c.SkillContentId = null;
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
        c = Command(MatchCommandKind.CastSpell); c.CardInstanceId = 0;
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
        c = Command(MatchCommandKind.RollDice); c.DiceIndex = 3;
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
        c = Command(MatchCommandKind.NormalAttack); c.CardInstanceId = 77;
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
        c = Command(MatchCommandKind.CastSpell); c.UnitRuntimeId = 42;
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
        c = Command(MatchCommandKind.FinishUnit); c.Destination = new GridCoordinate(1, 0, 0);
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
    }

    [Test]
    public void AutoSpawnPointRoundTrips()
    {
        var command = Command(MatchCommandKind.SpawnUnit);
        command.SpawnPointId = null;
        Assert.That(MatchProtocol.TryDeserialize(MatchProtocol.Serialize(command), out var restored, out _), Is.True);
        Assert.That(restored.SpawnPointId, Is.Empty);
    }

    [Test]
    public void TruncatedTrailingOversizedAndUnknownPayloadsAreRejected()
    {
        byte[] bytes = MatchProtocol.Serialize(Command());
        for (int size = 0; size < bytes.Length; size++)
            Assert.That(MatchProtocol.TryDeserialize(bytes.Take(size).ToArray(), out _, out _), Is.False, $"length={size}");
        Assert.That(MatchProtocol.TryDeserialize(bytes.Concat(new byte[] { 0 }).ToArray(), out _, out _), Is.False);
        Assert.That(MatchProtocol.TryDeserialize(new byte[1025], out _, out _), Is.False);
        bytes[bytes.Length - 1] = 255;
        Assert.That(MatchProtocol.TryDeserialize(bytes, out _, out _), Is.False);
    }

    [Test]
    public void InvalidIdsActorsAndHiddenPayloadAreRejected()
    {
        var c = Command(); c.Actor = (PlayerId)3;
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
        c = Command(); c.MatchId = "not-a-match";
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
        c = Command(); c.UnitRuntimeId = 1;
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
        c = Command(MatchCommandKind.SpawnUnit); c.UnitContentId = "";
        Assert.Throws<ArgumentException>(() => MatchProtocol.Serialize(c));
    }

    [Test]
    public void ProtocolAssemblyHasNoUnityDependency()
    {
        Assert.That(typeof(MatchCommandDto).Assembly.GetReferencedAssemblies().Any(x => x.Name.StartsWith("Unity")), Is.False);
    }

    [Test]
    public void CompatibilityRoundTripAndDistinctMismatchReasons()
    {
        var local = Compatible();
        var peer = MatchProtocol.DeserializeCompatibility(MatchProtocol.SerializeCompatibility(local));
        Assert.That(local.Compare(peer), Is.EqualTo(CommandReason.None));
        peer.ProtocolVersion++; Assert.That(local.Compare(peer), Is.EqualTo(CommandReason.ProtocolMismatch));
        peer = Compatible(); peer.GameplayRulesVersion++;
        Assert.That(local.Compare(peer), Is.EqualTo(CommandReason.RulesMismatch));
        peer = Compatible(); peer.ContentCatalogHash = new string('b', 64);
        Assert.That(local.Compare(peer), Is.EqualTo(CommandReason.ContentMismatch));
    }

    [Test]
    public void RetryReturnsOriginalAcknowledgementWithoutSecondExecution()
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        int calls = 0;
        var command = Command();
        var first = gate.Submit(command, PlayerId.Player1, _ => { calls++; return (CommandReason.None, null); });
        first.ServerSequence = 999; // Caller cannot corrupt cached acknowledgement.
        var retry = gate.Submit(command, PlayerId.Player1, _ => { calls++; return (CommandReason.None, null); });
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(retry.Accepted, Is.True);
        Assert.That(retry.ServerSequence, Is.EqualTo(1));
        command.ExpectedTurn++;
        Assert.That(gate.Submit(command, PlayerId.Player1, _ => (CommandReason.None, null)).Reason,
            Is.EqualTo(CommandReason.ReplayConflict));
    }

    [Test]
    public void RejectionIsCachedAndDoesNotAdvanceServerSequence()
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        var command = Command();
        var result = gate.Submit(command, PlayerId.Player1, _ => (CommandReason.WrongTurn, "Sai lượt."));
        var retry = gate.Submit(command, PlayerId.Player1, _ => throw new Exception("Must not execute."));
        Assert.That(retry.Reason, Is.EqualTo(CommandReason.WrongTurn));
        Assert.That(result.ServerSequence, Is.Zero);
        command = Command(); command.CommandId = 2; command.ClientSequence = 2;
        Assert.That(gate.Submit(command, PlayerId.Player1, _ => (CommandReason.None, null)).ServerSequence, Is.EqualTo(1));
    }

    [Test]
    public void IdentityCompatibilityAndSequenceFailuresNeverReachGameplay()
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        var c = Command();
        (CommandReason, string) Never(MatchCommandDto _) => throw new Exception("Must not execute.");
        Assert.That(gate.Submit(c, PlayerId.Player2, Never).Reason, Is.EqualTo(CommandReason.ActorMismatch));
        c.MatchId = new string('b', 32);
        Assert.That(gate.Submit(c, PlayerId.Player1, Never).Reason, Is.EqualTo(CommandReason.MatchMismatch));
        c = Command(); c.ClientSequence = 2;
        Assert.That(gate.Submit(c, PlayerId.Player1, Never).Reason, Is.EqualTo(CommandReason.InvalidSequence));
        c = Command(); c.AcknowledgedServerSequence = 1;
        Assert.That(gate.Submit(c, PlayerId.Player1, Never).Reason, Is.EqualTo(CommandReason.InvalidSequence));
        c = Command(); c.Compatibility.ProtocolVersion++;
        Assert.That(gate.Submit(c, PlayerId.Player1, Never).Reason, Is.EqualTo(CommandReason.ProtocolMismatch));
        Assert.That(gate.NextClientSequence(PlayerId.Player1), Is.EqualTo(1));
    }

    [Test]
    public void TwoPlayersMayUseSameCommandIdAndHaveIndependentSequences()
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        var first = Command(); var second = Command(); second.Actor = PlayerId.Player2;
        Assert.That(gate.Submit(first, PlayerId.Player1, _ => (CommandReason.None, null)).Accepted, Is.True);
        Assert.That(gate.Submit(second, PlayerId.Player2, _ => (CommandReason.None, null)).ServerSequence, Is.EqualTo(2));
    }

    [Test]
    public void ReentrantCommandCannotMutateState()
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        gate.Submit(Command(), PlayerId.Player1, _ =>
        {
            var nested = Command(); nested.CommandId = 2; nested.ClientSequence = 2;
            Assert.That(gate.Submit(nested, PlayerId.Player1, __ => throw new Exception()).Reason, Is.EqualTo(CommandReason.InvalidState));
            return (CommandReason.None, null);
        });
        Assert.That(gate.NextClientSequence(PlayerId.Player1), Is.EqualTo(2));
    }

    [Test]
    public void AcknowledgementRoundTripsUnicodeReasonAndSequences()
    {
        var gate = new MatchCommandGate(MatchId, Compatible());
        var result = gate.Submit(Command(), PlayerId.Player1, _ => (CommandReason.WrongTurn, "Không đúng lượt."));
        var restored = MatchProtocol.DeserializeAcknowledgement(MatchProtocol.SerializeAcknowledgement(result));
        Assert.That(restored.Detail, Is.EqualTo(result.Detail));
        Assert.That(restored.Reason, Is.EqualTo(result.Reason));
        Assert.That(restored.ClientSequence, Is.EqualTo(1));
        Assert.That(restored.Accepted, Is.False);
    }

    [Test]
    public void RngHasKnownSequenceAndRestoresContinuation()
    {
        var rng = new MatchRandom(1);
        rng.Value(); Assert.That(rng.Capture().State, Is.EqualTo(270369u));
        rng.Value(); Assert.That(rng.Capture().State, Is.EqualTo(67634689u));
        var saved = rng.Capture();
        var expected = Enumerable.Range(0, 100).Select(_ => rng.Range(1, 7)).ToArray();
        var restored = new MatchRandom(99); restored.Restore(saved);
        CollectionAssert.AreEqual(expected, Enumerable.Range(0, 100).Select(_ => restored.Range(1, 7)).ToArray());
        Assert.That(restored.Capture().Sequence, Is.EqualTo(rng.Capture().Sequence));
        Assert.That(expected.All(x => x >= 1 && x <= 6), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => new MatchRandom(0));
        Assert.Throws<ArgumentException>(() => rng.Restore(default));
    }
}
