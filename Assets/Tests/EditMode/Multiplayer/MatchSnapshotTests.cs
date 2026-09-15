using System;
using System.Linq;
using NUnit.Framework;
using TurnBasedGame.Multiplayer.Protocol;

public sealed class MatchSnapshotTests
{
    private static MatchSnapshot Snapshot() => new MatchSnapshot {
        Compatibility = new MatchCompatibility { ContentCatalogHash = new string('a', 64) },
        SceneHash = new string('b', 64), NextCommandId = 9, Deadline = 1234.56789,
        State = new CommandAcknowledgement { MatchId = new string('c', 32), Actor = PlayerId.Player2,
            Accepted = true, ServerSequence = 7, NextClientSequence = 5,
            StateChanges = new[] {
                new MatchStateChange { Kind = StateChangeKind.MP, Player = PlayerId.Player1, Value = 4 },
                new MatchStateChange { Kind = StateChangeKind.Unit, Player = PlayerId.Player2, Entity = 77,
                    ContentId = new string('d', 32), Position = new GridCoordinate(-3, 0, 7), Value = 8, Value4 = 1 },
                new MatchStateChange { Kind = StateChangeKind.Status, Entity = 77, Player = PlayerId.Player1,
                    Value = 3, Value2 = 2, Value3 = 0, Value4 = 4 },
                new MatchStateChange { Kind = StateChangeKind.HandCard, Player = PlayerId.Player2,
                    Entity = 19, ContentId = new string('e', 32) }
            } }
    };

    [Test]
    public void SnapshotRoundTripPreservesStateDeadlineAndCommandCounters()
    {
        var input = Snapshot();
        var output = MatchSnapshotProtocol.Deserialize(MatchSnapshotProtocol.Serialize(input));
        Assert.That(output.NextCommandId, Is.EqualTo(9));
        Assert.That(output.State.NextClientSequence, Is.EqualTo(5));
        Assert.That(output.Deadline, Is.EqualTo(input.Deadline));
        CollectionAssert.AreEqual(MatchProtocol.SerializeAcknowledgement(input.State), MatchProtocol.SerializeAcknowledgement(output.State));
        Assert.That(output.Hash, Is.EqualTo(MatchSnapshotProtocol.Hash(output.State.StateChanges, output.Deadline)));
    }

    [Test]
    public void EmptyCollectionsAreExplicitAndRoundTrip()
    {
        var input = Snapshot(); input.State.StateChanges = Array.Empty<MatchStateChange>();
        Assert.That(MatchSnapshotProtocol.Deserialize(MatchSnapshotProtocol.Serialize(input)).State.StateChanges, Is.Empty);
    }

    [Test]
    public void HashIgnoresCollectionEnumerationOrderButIncludesDeadlineAndValues()
    {
        var s = Snapshot(); var hash = MatchSnapshotProtocol.Hash(s.State.StateChanges, s.Deadline);
        Assert.That(MatchSnapshotProtocol.Hash(s.State.StateChanges.Reverse().ToArray(), s.Deadline), Is.EqualTo(hash));
        Assert.That(MatchSnapshotProtocol.Hash(s.State.StateChanges, s.Deadline + 1), Is.Not.EqualTo(hash));
        s.State.StateChanges[0].Value++;
        Assert.That(MatchSnapshotProtocol.Hash(s.State.StateChanges, s.Deadline), Is.Not.EqualTo(hash));
    }

    [TestCase(PlayerId.Player1)]
    [TestCase(PlayerId.Player2)]
    public void ViewerNeverReceivesOpponentCardsOrHostRandom(PlayerId viewer)
    {
        var state = new[] {
            new MatchStateChange { Kind = StateChangeKind.Random, Value = 123 },
            new MatchStateChange { Kind = StateChangeKind.HandCard, Player = PlayerId.Player1, Entity = 1 },
            new MatchStateChange { Kind = StateChangeKind.HandCard, Player = PlayerId.Player2, Entity = 2 },
            new MatchStateChange { Kind = StateChangeKind.RemovedUnit, Entity = 3 }
        };
        var visible = MatchSnapshotProtocol.Visible(state, viewer);
        Assert.That(visible.Length, Is.EqualTo(1)); Assert.That(visible[0].Player, Is.EqualTo(viewer));
        Assert.That(state.Length, Is.EqualTo(4));
    }

    [Test]
    public void SnapshotRejectsCorruptionTruncationAndTrailingBytes()
    {
        var bytes = MatchSnapshotProtocol.Serialize(Snapshot());
        Assert.Throws<ArgumentException>(() => MatchSnapshotProtocol.Deserialize(bytes.Concat(new byte[] { 0 }).ToArray()));
        for (int length = 0; length < bytes.Length; length += 17)
        {
            var truncated = bytes.Take(length).ToArray();
            Assert.Catch(() => MatchSnapshotProtocol.Deserialize(truncated));
        }
        bytes[bytes.Length - 1] ^= 1;
        Assert.Catch(() => MatchSnapshotProtocol.Deserialize(bytes));
    }

    [Test]
    public void SnapshotRejectsOversizeAndInvalidEnvelope()
    {
        Assert.Throws<ArgumentException>(() => MatchSnapshotProtocol.Deserialize(new byte[MatchSnapshotProtocol.MaxBytes + 1]));
        var s = Snapshot(); s.NextCommandId = 0;
        Assert.Throws<ArgumentException>(() => MatchSnapshotProtocol.Serialize(s));
        s.NextCommandId = 1; s.Deadline = double.NaN;
        Assert.Throws<ArgumentException>(() => MatchSnapshotProtocol.Serialize(s));
        s.Deadline = 0; s.State.StateChanges = new MatchStateChange[2049];
        Assert.Throws<ArgumentException>(() => MatchSnapshotProtocol.Serialize(s));
    }

    [Test]
    public void ReplicaRequiresSnapshotAndIgnoresDuplicateOrOldCommit()
    {
        var order = new MatchReplicaSequence();
        Assert.That(order.CanApply(1), Is.False);
        order.Commit(7);
        Assert.That(order.CanApply(7), Is.False); Assert.That(order.CanApply(6), Is.False);
        Assert.That(order.NeedsSnapshot, Is.False); Assert.That(order.CanApply(8), Is.True);
        Assert.That(order.Applied, Is.EqualTo(7)); // Receiving alone must not acknowledge application.
    }

    [Test]
    public void GapLocksUntilSnapshotAndDoesNotAdvanceAppliedCursor()
    {
        var order = new MatchReplicaSequence(); order.Commit(7);
        Assert.That(order.CanApply(9), Is.False); Assert.That(order.Applied, Is.EqualTo(7));
        Assert.That(order.CanApply(8), Is.False); Assert.That(order.NeedsSnapshot, Is.True);
        order.Commit(10); Assert.That(order.CanApply(11), Is.True);
    }

    [Test]
    public void RestoredIdentityCannotCollideWithAdmittedRejectionOrReplayCache()
    {
        var s = Snapshot(); var gate = new MatchCommandGate(s.State.MatchId, s.Compatibility);
        var command = new MatchCommandDto { Compatibility = s.Compatibility, MatchId = s.State.MatchId,
            CommandId = 42, Actor = PlayerId.Player2, ClientSequence = 1, Kind = MatchCommandKind.EndTurn };
        gate.Submit(command, PlayerId.Player2, _ => (CommandReason.WrongTurn, "reject"));
        Assert.That(gate.NextCommandId(PlayerId.Player2), Is.EqualTo(43));
        Assert.That(gate.NextClientSequence(PlayerId.Player2), Is.EqualTo(2));
        Assert.That(gate.NextCommandId(PlayerId.Player1), Is.EqualTo(1));
        var replay = gate.Submit(command, PlayerId.Player2, _ => throw new Exception("Must not execute"));
        Assert.That(replay.Reason, Is.EqualTo(CommandReason.WrongTurn));
    }

    [Test]
    public void TwentyTurnReplacementSimulationKeepsViewerHashAfterGapsAndDuplicates()
    {
        var order = new MatchReplicaSequence(); order.Commit(0);
        for (ulong turn = 1; turn <= 20; turn++)
        {
            var s = Snapshot(); s.State.ServerSequence = turn; s.State.StateChanges[0].Value = (int)turn;
            s.Deadline += turn;
            if (turn % 4 == 0) { Assert.That(order.CanApply(turn + 1), Is.False); }
            else Assert.That(order.CanApply(turn), Is.True);
            var wire = MatchSnapshotProtocol.Deserialize(MatchSnapshotProtocol.Serialize(s));
            order.Commit(wire.State.ServerSequence);
            Assert.That(MatchSnapshotProtocol.Hash(wire.State.StateChanges, wire.Deadline), Is.EqualTo(wire.Hash));
            Assert.That(order.CanApply(turn), Is.False);
        }
    }
}
