using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles;
using TurnBasedGame.Core;
using UnityEngine;

namespace TurnBasedGame.Maps
{
    [Serializable]
    public struct BattleSpawnPointData
    {
        public PlayerID Owner;
        public Vector3Int GridPosition;

        public BattleSpawnPointData(PlayerID owner, Vector3Int gridPosition)
        {
            Owner = owner;
            GridPosition = gridPosition;
        }
    }

    [CreateAssetMenu(fileName = "Battle Map", menuName = "TurnBased/Maps/Battle Map")]
    public class BattleMapDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _preview;
        [SerializeField] private MapSettings _mapSettings;
        [SerializeField] private List<BattleSpawnPointData> _spawnPoints = new();
        [SerializeField] private List<Vector3Int> _capturePoints = new();
        [SerializeField] private GameObject _environmentPrefab;
        [SerializeField] private bool _enabled = true;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public Sprite Preview => _preview;
        public MapSettings MapSettings => _mapSettings;
        public IReadOnlyList<BattleSpawnPointData> SpawnPoints => _spawnPoints;
        public IReadOnlyList<Vector3Int> CapturePoints => _capturePoints;
        public GameObject EnvironmentPrefab => _environmentPrefab;
        public bool Enabled => _enabled;

        public bool TryValidate(out string error)
        {
            if (_mapSettings == null || _mapSettings.Tiles == null || _mapSettings.Tiles.Count == 0)
            {
                error = "MapSettings chưa có tile.";
                return false;
            }

            if (_spawnPoints == null ||
                !_spawnPoints.Exists(point => point.Owner == PlayerID.Player1) ||
                !_spawnPoints.Exists(point => point.Owner == PlayerID.Player2))
            {
                error = "Map phải có spawn point cho cả Player1 và Player2.";
                return false;
            }

            if (_capturePoints == null || _capturePoints.Count == 0)
            {
                error = "Map phải có ít nhất một capture point.";
                return false;
            }

            error = null;
            return true;
        }

#if UNITY_EDITOR
        public void ConfigureGenerated(string displayName, MapSettings mapSettings,
            List<BattleSpawnPointData> spawnPoints, List<Vector3Int> capturePoints)
        {
            _displayName = displayName;
            _mapSettings = mapSettings;
            _spawnPoints = spawnPoints;
            _capturePoints = capturePoints;
            _enabled = true;
        }
#endif
    }
}
