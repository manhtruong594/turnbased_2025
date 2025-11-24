using RedBjorn;
using RedBjorn.ProtoTiles;
using RedBjorn.ProtoTiles.Example;
using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }
    public MapSettings Map;
    public KeyCode GridToggle = KeyCode.G;
    public MapView MapView;
    public MapEntity MapEntity { get; private set; }

    // Dictionary theo dõi units trên map theo vị trí grid
    private Dictionary<Vector3Int, UnitMove> _unitPositions = new Dictionary<Vector3Int, UnitMove>();

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

    void Start()
    {

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
    public void RegisterUnit(Vector3Int gridPos, UnitMove unit)
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
    public UnitMove GetUnitAtTile(Vector3Int gridPos)
    {
        return _unitPositions.TryGetValue(gridPos, out var unit) ? unit : null;
    }

    /// <summary>
    /// Di chuyển unit từ vị trí cũ sang vị trí mới
    /// </summary>
    public void MoveUnit(UnitMove unit, Vector3Int fromPos, Vector3Int toPos)
    {
        UnregisterUnit(fromPos);
        RegisterUnit(toPos, unit);
        //unit.UpdateGridPosition(toPos);
    }

    /// <summary>
    /// Kiểm tra tile có unit hay không
    /// </summary>
    public bool HasUnitAtTile(Vector3Int gridPos)
    {
        return _unitPositions.ContainsKey(gridPos);
    }
    #endregion
}
