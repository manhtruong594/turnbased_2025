using UnityEngine;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using RedBjorn.ProtoTiles.Example;
using System;
using TurnBasedGame.Unit;

/// <summary>
/// Mediator Pattern: Trung gian giao tiếp giữa các Manager
/// Giảm sự phụ thuộc trực tiếp giữa các Manager với nhau
/// </summary>
public class GameMediator : MonoBehaviour
{
    public static GameMediator Instance { get; private set; }

    [Header("Manager References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private MPManager mpManager;
    [SerializeField] private MapManager mapManager;
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private AreaPathManager areaPathManager;

    #region Turn Events
    public event Action<PlayerID> OnPlayerTurnStarted;
    public event Action<PlayerID> OnPlayerTurnEnded;
    #endregion

    #region  MP Events
    public event Action<PlayerID, int, int> OnMPChanged; // (player, currentMP, maxMP)
    #endregion

    #region  Visualize Path Events
    public event Action<UnitMove> OnUnitSelected;
    public event Action<UnitMove> OnUnitDeselected;
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        turnManager.Initialize(this);
        mpManager.Initialize(this);
        mapManager.Initialize(this);
        unitSpawner.Initialize(this);
        areaPathManager.Initialize(this);
        areaPathManager.SetCachedMap(mapManager.MapEntity);
    }

    #region  Notify Methods

    public void NotifyPlayerTurnStarted(PlayerID player)
    {
        OnPlayerTurnStarted?.Invoke(player);
    }

    public void NotifyPlayerTurnEnded(PlayerID player)
    {
        OnPlayerTurnEnded?.Invoke(player);
    }

    public void NotifyMPChanged(PlayerID player, int currentMP, int maxMP)
    {
        OnMPChanged?.Invoke(player, currentMP, maxMP);
    }

    public void NotifySpawnUnit(UnitMove unit, int mpSpent)
    {
        mpManager.SpendMP(unit.GetOwner(), mpSpent);
    }

    public void NotifyUnitSelected(UnitMove unit)
    {
        OnUnitSelected?.Invoke(unit);
    }

    public void NotifyUnitDeselected(UnitMove unit)
    {
        OnUnitDeselected?.Invoke(unit);
    }

    #endregion
}