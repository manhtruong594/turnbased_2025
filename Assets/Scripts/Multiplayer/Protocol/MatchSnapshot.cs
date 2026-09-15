using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace TurnBasedGame.Multiplayer.Protocol
{
    public sealed class MatchSnapshot
    {
        public MatchCompatibility Compatibility;
        public string SceneHash;
        public long NextCommandId;
        public double Deadline;
        public CommandAcknowledgement State;
        public string Hash;
    }

    // Full replacement state, shared by bootstrap, command commits and recovery.
    public static class MatchSnapshotProtocol
    {
        public const int MaxBytes = 256 * 1024;
        public const ushort Version = 1;

        public static MatchStateChange[] Visible(MatchStateChange[] state, PlayerId viewer)
        {
            var result = new List<MatchStateChange>();
            foreach (var entry in state)
                if (entry.Kind != StateChangeKind.Random && entry.Kind != StateChangeKind.RemovedUnit &&
                    (entry.Kind != StateChangeKind.HandCard || entry.Player == viewer)) result.Add(entry);
            return result.ToArray();
        }

        public static string Hash(MatchStateChange[] state, double deadline)
        {
            // Sort encoded records, not culture-sensitive strings or Unity object ordering.
            var records = new List<byte[]>();
            foreach (var entry in state)
                records.Add(MatchProtocol.SerializeAcknowledgement(new CommandAcknowledgement {
                    MatchId = new string('0', 32), Actor = PlayerId.Player1, StateChanges = new[] { entry } }));
            records.Sort((a, b) => {
                for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
                    if (a[i] != b[i]) return a[i].CompareTo(b[i]);
                return a.Length.CompareTo(b.Length);
            });
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(deadline);
            foreach (var record in records) { writer.Write(record.Length); writer.Write(record); }
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }

        public static byte[] Serialize(MatchSnapshot value)
        {
            if (value == null || !MatchProtocol.IsHex(value.SceneHash, 64) || value.NextCommandId <= 0 ||
                double.IsNaN(value.Deadline) || double.IsInfinity(value.Deadline) || value.Deadline < 0 ||
                value.State == null || value.State.NextClientSequence == 0 || value.State.StateChanges == null || value.State.StateChanges.Length > 2048)
                throw new ArgumentException("Invalid snapshot envelope.");
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Version);
            writer.Write(MatchProtocol.SerializeCompatibility(value.Compatibility));
            writer.Write(value.SceneHash);
            writer.Write(value.NextCommandId);
            writer.Write(value.Deadline);
            writer.Write(Hash(value.State.StateChanges, value.Deadline));
            var bytes = MatchProtocol.SerializeAcknowledgement(value.State);
            writer.Write(bytes.Length); writer.Write(bytes);
            if (stream.Length > MaxBytes) throw new ArgumentException("Snapshot exceeds size limit.");
            return stream.ToArray();
        }

        public static MatchSnapshot Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length > MaxBytes) throw new ArgumentException("Invalid snapshot size.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt16() != Version) throw new ArgumentException("Snapshot version mismatch.");
            var result = new MatchSnapshot { Compatibility = MatchProtocol.DeserializeCompatibility(reader.ReadBytes(70)),
                SceneHash = reader.ReadString(), NextCommandId = reader.ReadInt64(), Deadline = reader.ReadDouble(),
                Hash = reader.ReadString() };
            int count = reader.ReadInt32();
            if (count < 0 || count > MaxBytes || count != stream.Length - stream.Position)
                throw new ArgumentException("Invalid snapshot length.");
            result.State = MatchProtocol.DeserializeAcknowledgement(reader.ReadBytes(count));
            Serialize(result);
            if (result.Hash != Hash(result.State.StateChanges, result.Deadline)) throw new ArgumentException("Snapshot hash mismatch.");
            return result;
        }
    }

    public sealed class MatchReplicaSequence
    {
        public ulong Applied { get; private set; }
        public bool NeedsSnapshot { get; private set; } = true;
        public bool CanApply(ulong sequence)
        {
            if (NeedsSnapshot || sequence <= Applied) return false;
            if (Applied == ulong.MaxValue || sequence != Applied + 1) { NeedsSnapshot = true; return false; }
            return true;
        }
        public void Commit(ulong sequence) { Applied = sequence; NeedsSnapshot = false; }
        public void Invalidate() => NeedsSnapshot = true;
    }
}
