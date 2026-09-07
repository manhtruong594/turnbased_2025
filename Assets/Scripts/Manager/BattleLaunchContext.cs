using TurnBasedGame.Maps;

namespace TurnBasedGame.Core
{
    public static class BattleLaunchContext
    {
        public static BattleMapDefinitionSO SelectedMap { get; private set; }

        public static void SelectMap(BattleMapDefinitionSO map)
        {
            SelectedMap = map;
        }

        public static void Clear()
        {
            SelectedMap = null;
        }
    }
}
