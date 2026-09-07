using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedGame.Maps
{
    [CreateAssetMenu(fileName = ResourceName, menuName = "TurnBased/Maps/Battle Map Catalog")]
    public class BattleMapCatalogSO : ScriptableObject
    {
        public const string ResourceName = "BattleMapCatalog";

        [SerializeField] private List<BattleMapDefinitionSO> _maps = new();

        public IReadOnlyList<BattleMapDefinitionSO> Maps => _maps;

        public static BattleMapCatalogSO LoadDefault()
        {
            return UnityEngine.Resources.Load<BattleMapCatalogSO>(ResourceName);
        }

#if UNITY_EDITOR
        public void Add(BattleMapDefinitionSO map)
        {
            if (map != null && !_maps.Contains(map))
            {
                _maps.Add(map);
            }
        }
#endif
    }
}
