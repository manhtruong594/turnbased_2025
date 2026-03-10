using UnityEngine;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using RedBjorn.ProtoTiles.Example;
using System;
using TurnBasedGame.Unit;
using TurnBasedGame.Capture;
using TurnBasedGame.SpellCard;

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
    [SerializeField] private CapturePointManager capturePointManager;

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

    #region Unit Movement Events
    public event Action<UnitMove, Vector3Int, Vector3Int> OnUnitMoved;
    #endregion

    #region Capture Point Events
    public event Action<Vector3Int, PlayerID> OnCapturePointCaptured;
    public event Action<PlayerID> OnGameEnd;
    #endregion

    #region Spell Card Events
    public event Action<SpellCardData, UnitMove, PlayerID> OnSpellCardUsed;
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

        if (capturePointManager != null)
            capturePointManager.Initialize(this);
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

    public void NotifyUnitMoved(UnitMove unit, Vector3Int oldPos, Vector3Int newPos)
    {
        OnUnitMoved?.Invoke(unit, oldPos, newPos);
    }

    public void NotifyCapturePointCaptured(Vector3Int position, PlayerID newOwner)
    {
        OnCapturePointCaptured?.Invoke(position, newOwner);
    }

    public void NotifyGameEnd(PlayerID winner)
    {
        turnManager.TriggerGameEnd(winner);
        OnGameEnd?.Invoke(winner);
    }

    public void NotifySpellCardUsed(SpellCardData card, UnitMove target, PlayerID caster)
    {
        OnSpellCardUsed?.Invoke(card, target, caster);
    }

    #endregion
}