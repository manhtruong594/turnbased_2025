using System;
using System.IO;
using System.Text;

namespace TurnBasedGame.Multiplayer.Protocol
{
    public enum PlayerId : byte { Player1 = 1, Player2 = 2 }
    public enum MatchCommandKind : byte { EndTurn = 1, SpawnUnit = 2, MoveUnit = 3 }
    public enum CommandReason : byte
    {
        None, InvalidPayload, ProtocolMismatch, RulesMismatch, ContentMismatch,
        MatchMismatch, ActorMismatch, InvalidSequence, ReplayConflict,
        InvalidState, WrongTurn, ExecutionRejected
    }

    [Serializable]
    public struct GridCoordinate
    {
        public int X, Y, Z;
        public GridCoordinate(int x, int y, int z) { X = x; Y = y; Z = z; }
    }

    [Serializable]
    public sealed class MatchCompatibility
    {
        public ushort ProtocolVersion = MatchProtocol.ProtocolVersion;
        public int GameplayRulesVersion = MatchProtocol.GameplayRulesVersion;
        public string ContentCatalogHash;

        public CommandReason Compare(MatchCompatibility peer)
        {
            if (peer == null) return CommandReason.InvalidPayload;
            if (peer.ProtocolVersion != ProtocolVersion) return CommandReason.ProtocolMismatch;
            if (peer.GameplayRulesVersion != GameplayRulesVersion) return CommandReason.RulesMismatch;
            if (!MatchProtocol.IsHex(ContentCatalogHash, 64) ||
                !string.Equals(ContentCatalogHash, peer.ContentCatalogHash, StringComparison.Ordinal))
                return CommandReason.ContentMismatch;
            return CommandReason.None;
        }
    }

    // ID wire types: MatchId/content IDs = canonical lowercase GUID; UnitRuntimeId = host counter;
    // SpawnPointId = owner:x:y:z (invariant culture). No Unity objects or local callbacks.
    [Serializable]
    public sealed class MatchCommandDto
    {
        public MatchCompatibility Compatibility;
        public string MatchId;
        public long CommandId;
        public PlayerId Actor;
        public int ExpectedTurn;
        public ulong ClientSequence;
        public ulong AcknowledgedServerSequence;
        public MatchCommandKind Kind;
        public string UnitContentId;
        public string SpawnPointId;
        public ulong UnitRuntimeId;
        public GridCoordinate Destination;
    }

    [Serializable]
    public sealed class CommandAcknowledgement
    {
        public string MatchId;
        public long CommandId;
        public PlayerId Actor;
        public ulong ClientSequence;
        public ulong ServerSequence;
        public bool Accepted;
        public CommandReason Reason;
        public string Detail;
    }

    public static class MatchProtocol
    {
        public const ushort ProtocolVersion = 1;
        public const int GameplayRulesVersion = 1;
        public const int MaxCommandBytes = 1024;

        public static bool IsHex(string value, int length)
        {
            if (value == null || value.Length != length) return false;
            foreach (char c in value)
                if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            return true;
        }

        public static bool IsPlayer(PlayerId player) => player == PlayerId.Player1 || player == PlayerId.Player2;

        public static byte[] Serialize(MatchCommandDto command)
        {
            if (command == null || command.Compatibility == null || !IsHex(command.MatchId, 32) ||
                !IsHex(command.Compatibility.ContentCatalogHash, 64) || !IsPlayer(command.Actor) ||
                command.CommandId <= 0 || command.ClientSequence == 0 || command.ExpectedTurn < 0)
                throw new ArgumentException("Invalid command envelope.");
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            WriteCompatibility(writer, command.Compatibility);
            WriteFixed(writer, command.MatchId);
            writer.Write(command.CommandId);
            writer.Write((byte)command.Actor);
            writer.Write(command.ExpectedTurn);
            writer.Write(command.ClientSequence);
            writer.Write(command.AcknowledgedServerSequence);
            writer.Write((byte)command.Kind);
            switch (command.Kind)
            {
                case MatchCommandKind.EndTurn:
                    if (!string.IsNullOrEmpty(command.UnitContentId) || !string.IsNullOrEmpty(command.SpawnPointId))
                        throw new ArgumentException("Unexpected end turn payload.");
                    break;
                case MatchCommandKind.SpawnUnit:
                    if (!IsHex(command.UnitContentId, 32)) throw new ArgumentException("Invalid UnitContentId.");
                    WriteFixed(writer, command.UnitContentId);
                    WriteShortString(writer, command.SpawnPointId ?? "", 64);
                    break;
                case MatchCommandKind.MoveUnit:
                    if (command.UnitRuntimeId == 0 || !string.IsNullOrEmpty(command.UnitContentId) ||
                        !string.IsNullOrEmpty(command.SpawnPointId)) throw new ArgumentException("Invalid move payload.");
                    writer.Write(command.UnitRuntimeId);
                    writer.Write(command.Destination.X);
                    writer.Write(command.Destination.Y);
                    writer.Write(command.Destination.Z);
                    break;
                default: throw new ArgumentException("Unknown command kind.");
            }
            if (command.Kind != MatchCommandKind.MoveUnit &&
                (command.UnitRuntimeId != 0 || command.Destination.X != 0 ||
                 command.Destination.Y != 0 || command.Destination.Z != 0))
                throw new ArgumentException("Unexpected movement payload.");
            return stream.ToArray();
        }

        public static bool TryDeserialize(byte[] bytes, out MatchCommandDto command, out string reason)
        {
            command = null;
            reason = "Invalid command payload.";
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaxCommandBytes) return false;
            try
            {
                using var stream = new MemoryStream(bytes, false);
                using var reader = new BinaryReader(stream, Encoding.UTF8);
                var value = new MatchCommandDto
                {
                    Compatibility = ReadCompatibility(reader), MatchId = ReadFixed(reader, 32),
                    CommandId = reader.ReadInt64(), Actor = (PlayerId)reader.ReadByte(),
                    ExpectedTurn = reader.ReadInt32(), ClientSequence = reader.ReadUInt64(),
                    AcknowledgedServerSequence = reader.ReadUInt64(), Kind = (MatchCommandKind)reader.ReadByte()
                };
                switch (value.Kind)
                {
                    case MatchCommandKind.EndTurn: break;
                    case MatchCommandKind.SpawnUnit:
                        value.UnitContentId = ReadFixed(reader, 32);
                        value.SpawnPointId = ReadShortString(reader, 64);
                        break;
                    case MatchCommandKind.MoveUnit:
                        value.UnitRuntimeId = reader.ReadUInt64();
                        value.Destination = new GridCoordinate(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                        break;
                    default: return false;
                }
                if (stream.Position != stream.Length) return false;
                Serialize(value); // Enforce the same canonical shape on both boundaries.
                command = value;
                reason = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException) { return false; }
        }

        public static byte[] SerializeCompatibility(MatchCompatibility value)
        {
            if (value == null || !IsHex(value.ContentCatalogHash, 64)) throw new ArgumentException("Invalid catalog hash.");
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            WriteCompatibility(writer, value);
            return stream.ToArray();
        }

        public static MatchCompatibility DeserializeCompatibility(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 70) throw new ArgumentException("Invalid compatibility payload.");
            using var reader = new BinaryReader(new MemoryStream(bytes, false));
            var value = ReadCompatibility(reader);
            if (!IsHex(value.ContentCatalogHash, 64)) throw new ArgumentException("Invalid catalog hash.");
            return value;
        }

        public static byte[] SerializeAcknowledgement(CommandAcknowledgement value)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            WriteFixed(writer, value.MatchId);
            writer.Write(value.CommandId);
            writer.Write((byte)value.Actor);
            writer.Write(value.ClientSequence);
            writer.Write(value.ServerSequence);
            writer.Write(value.Accepted);
            writer.Write((byte)value.Reason);
            WriteShortString(writer, value.Detail ?? "", 512);
            return stream.ToArray();
        }

        public static CommandAcknowledgement DeserializeAcknowledgement(byte[] bytes)
        {
            if (bytes == null || bytes.Length > 1024) throw new ArgumentException("Invalid acknowledgement.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            var value = new CommandAcknowledgement
            {
                MatchId = ReadFixed(reader, 32), CommandId = reader.ReadInt64(), Actor = (PlayerId)reader.ReadByte(),
                ClientSequence = reader.ReadUInt64(), ServerSequence = reader.ReadUInt64(),
                Accepted = reader.ReadBoolean(), Reason = (CommandReason)reader.ReadByte(),
                Detail = ReadShortString(reader, 512)
            };
            if (stream.Position != stream.Length || !IsHex(value.MatchId, 32) || !IsPlayer(value.Actor) ||
                !Enum.IsDefined(typeof(CommandReason), value.Reason)) throw new ArgumentException("Invalid acknowledgement.");
            return value;
        }

        private static void WriteCompatibility(BinaryWriter writer, MatchCompatibility value)
        {
            writer.Write(value.ProtocolVersion);
            writer.Write(value.GameplayRulesVersion);
            WriteFixed(writer, value.ContentCatalogHash);
        }
        private static MatchCompatibility ReadCompatibility(BinaryReader reader) => new MatchCompatibility
        {
            ProtocolVersion = reader.ReadUInt16(), GameplayRulesVersion = reader.ReadInt32(),
            ContentCatalogHash = ReadFixed(reader, 64)
        };
        private static void WriteFixed(BinaryWriter writer, string value) => writer.Write(Encoding.ASCII.GetBytes(value));
        private static string ReadFixed(BinaryReader reader, int count)
        {
            var bytes = reader.ReadBytes(count);
            if (bytes.Length != count) throw new EndOfStreamException();
            return Encoding.ASCII.GetString(bytes);
        }
        private static void WriteShortString(BinaryWriter writer, string value, int limit)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > limit) throw new ArgumentException("String exceeds protocol limit.");
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }
        private static string ReadShortString(BinaryReader reader, int limit)
        {
            int length = reader.ReadUInt16();
            if (length > limit) throw new ArgumentException("String exceeds protocol limit.");
            var bytes = reader.ReadBytes(length);
            if (bytes.Length != length) throw new EndOfStreamException();
            return new UTF8Encoding(false, true).GetString(bytes);
        }
    }
}
