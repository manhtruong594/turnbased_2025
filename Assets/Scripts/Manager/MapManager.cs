using RedBjorn;
using RedBjorn.ProtoTiles;
using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;

public class MapManager : BaseManager
{
    public static MapManager Instance { get; private set; }
    public MapSettings Map;
    public KeyCode GridToggle = KeyCode.G;
    public MapView MapView;
    public MapEntity MapEntity { get; private set; }

    // Dictionary theo dõi units trên map theo vị trí grid
    private Dictionary<Vector3Int, UnitController> _unitPositions = new Dictionary<Vector3Int, UnitController>();

    void Awake()
    {
        Instance = this;
        if (!MapView)
        {
#if UNITY_2023_1_OR_NEWER
            MapView = FindFirstObjectByType<MapView>();
#else
                MapView = FindObjectOfType<MapView>();
#endif
        }
        MapEntity = new MapEntity(Map, MapView);
        if (MapView)
        {
            MapView.Init(MapEntity);
        }
        else
        {
            Log.E("Can't find MapView. Random errors can occur");
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(GridToggle))
        {
            MapEntity.GridToggle();
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
    #endregion

    #region  Helper Methods
    public float GetDistance(Vector3Int posA, Vector3Int posB)
    {
        return MapEntity.Distance(posA, posB);
    }
    #endregion 
}
