using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace TurnBasedGame.Multiplayer.Protocol
{
    // One bucket per authenticated connection. Charge before reading/allocating payloads.
    public sealed class MatchMessageBudget
    {
        public const int Burst = 20;
        public const double PerSecond = 10;
        private double tokens = Burst;
        private double last;
        public MatchMessageBudget(double now) { last = now; }
        public bool TryConsume(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || now < last) return false;
            tokens = Math.Min(Burst, tokens + (now - last) * PerSecond);
            last = now;
            if (tokens < 1) return false;
            tokens -= 1;
            return true;
        }
    }

    public static class MatchAudit
    {
        // Only canonical IDs, numbers and a validated enum. Never accept payload/detail/credentials.
        public static string Command(string matchId, PlayerId player, long commandId, int turn,
            ulong sequence, CommandReason reason) => string.Format(CultureInfo.InvariantCulture,
            "[MP-AUDIT] match={0} player={1} command={2} turn={3} sequence={4} reason={5}",
            MatchProtocol.IsHex(matchId, 32) ? matchId : "unknown",
            MatchProtocol.IsPlayer(player) ? (int)player : 0, commandId, turn, sequence,
            Enum.IsDefined(typeof(CommandReason), reason) ? reason : CommandReason.InvalidPayload);
    }

    [Serializable]
    public sealed class MatchFinalResult
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public int RulesVersion = MatchProtocol.GameplayRulesVersion;
        public string MatchId;
        public PlayerId Winner;
        public int Turn;
        public int EndReason;
        public ulong ServerSequence;
    }

    // Recorded only after a successful authority commit, retained for the lifetime of the match.
    public sealed class MatchResultRecorder
    {
        public MatchFinalResult Result { get; private set; }
        public bool TryRecord(bool authority, CommandAcknowledgement commit)
        {
            if (!authority || Result != null || commit == null || !commit.Accepted ||
                !MatchProtocol.IsHex(commit.MatchId, 32)) return false;
            foreach (var state in commit.StateChanges)
                if (state.Kind == StateChangeKind.Turn && MatchProtocol.IsPlayer((PlayerId)state.Value3))
                {
                    Result = new MatchFinalResult { MatchId = commit.MatchId, Winner = (PlayerId)state.Value3,
                        Turn = state.Value, EndReason = state.Value4, ServerSequence = commit.ServerSequence };
                    return true;
                }
            return false;
        }
    }

    public static class MatchServiceWait
    {
        // SDK requests may not support cancellation. Observe late faults; callers own late resources.
        public static async Task<T> Run<T>(Task<T> task, CancellationToken cancellation, int milliseconds = 30000)
        {
            using var timer = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var timeout = Task.Delay(milliseconds, timer.Token);
            if (await Task.WhenAny(task, timeout) != task)
            {
                _ = Observe(task);
                cancellation.ThrowIfCancellationRequested();
                throw new TimeoutException("Dịch vụ quá hạn (30s). Kiểm tra mạng rồi thử lại.");
            }
            timer.Cancel();
            if (cancellation.IsCancellationRequested)
            {
                _ = Observe(task);
                cancellation.ThrowIfCancellationRequested();
            }
            return await task;
        }
        public static async Task Run(Task task, CancellationToken cancellation, int milliseconds = 30000)
        {
            await Run(Complete(task), cancellation, milliseconds);
        }
        private static async Task<bool> Complete(Task task) { await task; return true; }
        private static async Task Observe(Task task) { try { await task; } catch { } }
    }
}
