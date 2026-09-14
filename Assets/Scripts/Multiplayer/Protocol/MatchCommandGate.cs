using System;
using System.Collections.Generic;

namespace TurnBasedGame.Multiplayer.Protocol
{
    public sealed class MatchCommandGate
    {
        private readonly string matchId;
        private readonly MatchCompatibility compatibility;
        private readonly Dictionary<(PlayerId, long), (byte[] Payload, CommandAcknowledgement Result)> processed = new();
        private readonly ulong[] clientSequences = new ulong[3];
        private bool executing;
        private bool faulted;
        public ulong ServerSequence { get; private set; }
        public ulong NextClientSequence(PlayerId actor) => MatchProtocol.IsPlayer(actor)
            ? checked(clientSequences[(int)actor] + 1) : throw new ArgumentException("Invalid actor.");

        public MatchCommandGate(string matchId, MatchCompatibility compatibility)
        {
            if (!MatchProtocol.IsHex(matchId, 32)) throw new ArgumentException("Invalid MatchId.");
            this.matchId = matchId;
            this.compatibility = MatchProtocol.DeserializeCompatibility(MatchProtocol.SerializeCompatibility(compatibility));
        }

        // authenticatedActor is supplied by the session, never taken on trust from the payload.
        public CommandAcknowledgement Submit(MatchCommandDto command, PlayerId authenticatedActor,
            Func<MatchCommandDto, (CommandReason Reason, string Detail)> execute,
            Func<MatchStateChange[]> captureChanges = null, Func<int> diceValue = null, Action rollback = null)
        {
            byte[] payload;
            try { payload = MatchProtocol.Serialize(command); }
            catch (ArgumentException) { return Result(command, CommandReason.InvalidPayload, "Invalid command payload."); }
            var mismatch = compatibility.Compare(command.Compatibility);
            if (mismatch != CommandReason.None) return Result(command, mismatch, mismatch.ToString());
            if (command.MatchId != matchId) return Result(command, CommandReason.MatchMismatch, "Command belongs to another match.");
            if (!MatchProtocol.IsPlayer(authenticatedActor) || command.Actor != authenticatedActor)
                return Result(command, CommandReason.ActorMismatch, "Actor does not match authenticated player.");
            var key = (command.Actor, command.CommandId);
            if (processed.TryGetValue(key, out var cached))
            {
                if (!Equal(payload, cached.Payload)) return Result(command, CommandReason.ReplayConflict, "CommandId reused with different payload.");
                return Copy(cached.Result);
            }
            int actorIndex = (int)command.Actor;
            if (executing || faulted) return Result(command, CommandReason.InvalidState, "Authority is busy or faulted.");
            if (command.ClientSequence != clientSequences[actorIndex] + 1 ||
                command.AcknowledgedServerSequence > ServerSequence)
                return Result(command, CommandReason.InvalidSequence, "Invalid client sequence or server acknowledgement.");

            // Consume before calling gameplay, including reentrant calls from mediator subscribers.
            clientSequences[actorIndex] = command.ClientSequence;
            processed.Add(key, (payload, Result(command, CommandReason.InvalidState, "Command is executing.")));
            (CommandReason Reason, string Detail) outcome;
            executing = true;
            MatchStateChange[] changes = Array.Empty<MatchStateChange>();
            int rolledValue = 0;
            try
            {
                outcome = execute(command);
                if (outcome.Reason == CommandReason.None)
                {
                    changes = captureChanges?.Invoke() ?? changes;
                    rolledValue = diceValue?.Invoke() ?? 0;
                }
            }
            catch
            {
                faulted = true;
                rollback?.Invoke();
                var failure = Result(command, CommandReason.ExecutionFault, "Gameplay transaction failed; match paused.");
                processed[key] = (payload, failure);
                return Copy(failure);
            }
            finally { executing = false; }
            if (outcome.Reason == CommandReason.None) ServerSequence = checked(ServerSequence + 1);
            var result = Result(command, outcome.Reason, outcome.Detail);
            result.StateChanges = changes;
            result.DiceValue = rolledValue;
            processed[key] = (payload, result);
            return Copy(result);
        }

        // Host-only timer/system work advances server order without consuming a player's command ID or sequence.
        public CommandAcknowledgement ExecuteSystem(PlayerId actor, Func<(CommandReason Reason, string Detail)> execute,
            Func<MatchStateChange[]> captureChanges, Action rollback)
        {
            var identity = new MatchCommandDto { Actor = actor };
            if (!MatchProtocol.IsPlayer(actor) || executing || faulted)
                return Result(identity, CommandReason.InvalidState, "Authority is busy or faulted.");
            executing = true;
            try
            {
                var outcome = execute();
                var result = Result(identity, outcome.Reason, outcome.Detail);
                if (result.Accepted)
                {
                    result.StateChanges = captureChanges();
                    result.ServerSequence = ServerSequence = checked(ServerSequence + 1);
                }
                return result;
            }
            catch
            {
                faulted = true;
                rollback?.Invoke();
                return Result(identity, CommandReason.ExecutionFault, "System transaction failed; match paused.");
            }
            finally { executing = false; }
        }

        private CommandAcknowledgement Result(MatchCommandDto c, CommandReason reason, string detail) => new CommandAcknowledgement
        {
            MatchId = matchId, CommandId = c?.CommandId ?? 0, Actor = c?.Actor ?? 0,
            ClientSequence = c?.ClientSequence ?? 0, ServerSequence = ServerSequence,
            Accepted = reason == CommandReason.None, Reason = reason, Detail = detail,
            NextClientSequence = c != null && MatchProtocol.IsPlayer(c.Actor) ? NextClientSequence(c.Actor) : 0
        };
        private static CommandAcknowledgement Copy(CommandAcknowledgement r) => new CommandAcknowledgement
        {
            MatchId = r.MatchId, CommandId = r.CommandId, Actor = r.Actor, ClientSequence = r.ClientSequence,
            ServerSequence = r.ServerSequence, Accepted = r.Accepted, Reason = r.Reason, Detail = r.Detail,
            DiceValue = r.DiceValue, NextClientSequence = r.NextClientSequence, StateChanges = (MatchStateChange[])r.StateChanges.Clone()
        };
        private static bool Equal(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
