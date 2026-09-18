using System;
using System.Collections.Generic;

namespace TurnBasedGame.Multiplayer.Protocol
{
    [Serializable]
    public sealed class MatchLoadout
    {
        public string[] Units = Array.Empty<string>();
        public string[] Spells = Array.Empty<string>();
    }

    public static class MatchLobbyRules
    {
        public static bool ValidLoadout(MatchLoadout loadout, int maxUnits, int maxSpells,
            Func<string, bool> unitExists, Func<string, bool> spellExists)
        {
            return loadout != null && loadout.Units != null && loadout.Units.Length > 0 &&
                ValidIds(loadout.Units, maxUnits, unitExists) && ValidIds(loadout.Spells, maxSpells, spellExists);
        }

        private static bool ValidIds(string[] ids, int maximum, Func<string, bool> exists)
        {
            if (ids == null || ids.Length > maximum) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
                if (!MatchProtocol.IsHex(id, 32) || !seen.Add(id) || !exists(id)) return false;
            return true;
        }

        public static bool CanStart(int playerCount, int round, int hostReadyRound, int guestReadyRound,
            bool hostLoadoutValid, bool guestLoadoutValid, bool compatible)
        {
            return playerCount == 2 && round > 0 && hostReadyRound == round && guestReadyRound == round &&
                hostLoadoutValid && guestLoadoutValid && compatible;
        }
    }
}
