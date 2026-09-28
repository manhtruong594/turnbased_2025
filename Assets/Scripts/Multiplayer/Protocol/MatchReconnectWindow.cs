namespace TurnBasedGame.Multiplayer.Protocol
{
    public enum MatchDisconnectReason
    {
        None, TemporaryNetwork, ClientLeft, Kicked, HostLost, HostLeft, ServiceError, ReconnectExpired, Rejected
    }

    // Uses elapsed monotonic time supplied by the session owner, never wall-clock time.
    public sealed class MatchReconnectWindow
    {
        public const double Duration = 30;
        public bool Active { get; private set; }
        public double Deadline { get; private set; }
        public string Player { get; private set; }
        public void Begin(double now, string player)
        {
            if (string.IsNullOrEmpty(player)) throw new System.ArgumentException("Player identity required.", nameof(player));
            if (Active) return;
            Active = true;
            Player = player;
            Deadline = now + Duration;
        }
        public bool Expired(double now) => Active && now >= Deadline;
        public bool Allows(double now) => Active && now < Deadline;
        public bool Allows(string player, double now) => Player == player && Allows(now);
        public void Complete() { Active = false; Deadline = 0; Player = null; }
    }
}
