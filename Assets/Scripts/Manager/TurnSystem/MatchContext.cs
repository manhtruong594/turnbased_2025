using UnityEngine;

namespace TurnBasedGame.Core
{
    public enum MatchMode
    {
        VersusAI,
        NetworkPvP
    }

    /// <summary>Match identity is independent from the network transport lifecycle.</summary>
    public static class MatchContext
    {
        public static MatchMode Mode { get; private set; } = MatchMode.VersusAI;
        public static PlayerID LocalPlayer { get; private set; } = PlayerID.Player1;
        public static PlayerID? BotPlayer { get; private set; } = PlayerID.Player2;
        public static bool IsAuthoritative { get; private set; } = true;
        public static bool IsVersusAI => Mode == MatchMode.VersusAI;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => ConfigureVersusAI(PlayerID.Player1);

        public static void ConfigureVersusAI(PlayerID localPlayer)
        {
            ValidatePlayer(localPlayer);
            Mode = MatchMode.VersusAI;
            LocalPlayer = localPlayer;
            BotPlayer = OpponentOf(localPlayer);
            IsAuthoritative = true;
        }

        public static void ConfigureNetworkPvP(PlayerID localPlayer, bool isAuthoritative)
        {
            ValidatePlayer(localPlayer);
            Mode = MatchMode.NetworkPvP;
            LocalPlayer = localPlayer;
            BotPlayer = null;
            IsAuthoritative = isAuthoritative;
        }

        public static bool CanHumanControl(PlayerID player) => player == LocalPlayer;

        public static bool IsBot(PlayerID player) => IsVersusAI && BotPlayer == player;

        public static PlayerID OpponentOf(PlayerID player)
        {
            ValidatePlayer(player);
            return player == PlayerID.Player1 ? PlayerID.Player2 : PlayerID.Player1;
        }

        private static void ValidatePlayer(PlayerID player)
        {
            if (player != PlayerID.Player1 && player != PlayerID.Player2)
                throw new System.ArgumentOutOfRangeException(nameof(player), player, "Expected Player1 or Player2.");
        }
    }
}
