using RedBjorn;
using RedBjorn.ProtoTiles;
using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Maps;
using TurnBasedGame.Unit;

public class MapManager : BaseManager
{
    public static MapManager Instance { get; private set; }
    public MapSettings Map;
    public KeyCode GridToggle = KeyCode.G;
    public MapView MapView;
    public MapEntity MapEntity { get; private set; }
    public bool IsReady { get; private set; }

#if UNITY_EDITOR
    [Header("Editor Test")]
    [SerializeField] private BattleMapDefinitionSO _editorTestMap;
#endif

    // Dictionary theo dõi units trên map theo vị trí grid
    private Dictionary<Vector3Int, UnitController> _unitPositions = new Dictionary<Vector3Int, UnitController>();

    void Awake()
    {
        Instance = this;
        var selectedMap = BattleLaunchContext.SelectedMap;
#if UNITY_EDITOR
        if (selectedMap == null)
        {
            selectedMap = _editorTestMap;
        }
#endif
        if (selectedMap != null)
        {
            Map = selectedMap.MapSettings;
        }

        if (!Map)
        {
            Debug.LogError("[MapManager] MapSettings chưa được gán.", this);
            enabled = false;
            return;
        }

        if (!MapView)
        {
#if UNITY_2023_1_OR_NEWER
            MapView = FindFirstObjectByType<MapView>();
#else
                MapView = FindObjectOfType<MapView>();
#endif
        }
        if (!MapView)
        {
            Debug.LogError("[MapManager] Không tìm thấy MapView.", this);
            enabled = false;
            return;
        }

        if (selectedMap != null)
        {
            ClearMapView();
        }

        MapEntity = new MapEntity(Map, MapView);
        MapView.Init(MapEntity);

        if (selectedMap != null && !RuntimeMapBuilder.TryBuild(selectedMap, MapEntity, MapView, out var error))
        {
            Debug.LogError($"[MapManager] Không thể dựng map '{selectedMap.DisplayName}': {error}", selectedMap);
            enabled = false;
            return;
        }

        IsReady = true;
    }

    private void ClearMapView()
    {
        for (var index = MapView.transform.childCount - 1; index >= 0; index--)
        {
            var child = MapView.transform.GetChild(index).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(GridToggle))
        {
            MapEntity?.GridToggle();
        }
    }

    #region Unit Tracking
    /// <summary>
    /// Đăng ký unit tại vị trí grid
    /// </summary>
    public void RegisterUnit(Vector3Int gridPos, UnitController unit)
    {
        if (_unitPositions.ContainsKey(gridPos))
        {
            Debug.LogWarning($"Vị trí {gridPos} đã có unit {_unitPositions[gridPos].name}. Ghi đè bằng {unit.name}");
        }
        _unitPositions[gridPos] = unit;
    }

    /// <summary>
    /// Hủy đăng ký unit khỏi vị trí grid
    /// </summary>
    public void UnregisterUnit(Vector3Int gridPos)
    {
        _unitPositions.Remove(gridPos);
    }

    /// <summary>
    /// Lấy unit tại vị trí grid cụ thể
    /// </summary>
    public UnitController GetUnitAtTile(Vector3Int gridPos)
    {
        return _unitPositions.TryGetValue(gridPos, out var unit) ? unit : null;
    }

    public List<UnitController> GetUnitsInRange(TileEntity centerTile, int range)
    {
        var unitsInRange = new List<UnitController>();
        var tilesInRange = MapEntity.Area(centerTile.Position, range);
        foreach (var tilePos in tilesInRange)
        {
            var unit = GetUnitAtTile(tilePos);
            if (unit != null && !unit.IsDead())
            {
                unitsInRange.Add(unit);
            }
        }
        return unitsInRange;
    }
 
    /// <summary>
    /// Kiểm tra tile có unit hay không
    /// </summary>
    public bool HasUnitAtTile(Vector3Int gridPos)
    {
        return _unitPositions.ContainsKey(gridPos);
    }

    public bool IsTileAvailable(Vector3Int gridPos)
    {
        var tile = MapEntity?.Tile(gridPos);
        return tile != null && tile.Vacant && !HasUnitAtTile(gridPos);
    }
    #endregion

    #region  Helper Methods
    public float GetDistance(Vector3Int posA, Vector3Int posB)
    {
        return MapEntity.Distance(posA, posB);
    }
    #endregion 
}
