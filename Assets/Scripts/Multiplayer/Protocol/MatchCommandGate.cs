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
            Func<MatchCommandDto, (CommandReason Reason, string Detail)> execute)
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
            if (executing) return Result(command, CommandReason.InvalidState, "Authority is executing another command.");
            if (command.ClientSequence != clientSequences[actorIndex] + 1 ||
                command.AcknowledgedServerSequence > ServerSequence)
                return Result(command, CommandReason.InvalidSequence, "Invalid client sequence or server acknowledgement.");

            // Consume before calling gameplay, including reentrant calls from mediator subscribers.
            clientSequences[actorIndex] = command.ClientSequence;
            processed.Add(key, (payload, Result(command, CommandReason.InvalidState, "Command is executing.")));
            (CommandReason Reason, string Detail) outcome;
            executing = true;
            try { outcome = execute(command); }
            finally { executing = false; }
            if (outcome.Reason == CommandReason.None) ServerSequence = checked(ServerSequence + 1);
            var result = Result(command, outcome.Reason, outcome.Detail);
            processed[key] = (payload, result);
            return Copy(result);
        }

        private CommandAcknowledgement Result(MatchCommandDto c, CommandReason reason, string detail) => new CommandAcknowledgement
        {
            MatchId = matchId, CommandId = c?.CommandId ?? 0, Actor = c?.Actor ?? 0,
            ClientSequence = c?.ClientSequence ?? 0, ServerSequence = ServerSequence,
            Accepted = reason == CommandReason.None, Reason = reason, Detail = detail
        };
        private static CommandAcknowledgement Copy(CommandAcknowledgement r) => new CommandAcknowledgement
        {
            MatchId = r.MatchId, CommandId = r.CommandId, Actor = r.Actor, ClientSequence = r.ClientSequence,
            ServerSequence = r.ServerSequence, Accepted = r.Accepted, Reason = r.Reason, Detail = r.Detail
        };
        private static bool Equal(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
