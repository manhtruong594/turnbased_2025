using UnityEngine;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using System;
using TurnBasedGame.Unit;
using TurnBasedGame.Capture;
using TurnBasedGame.SpellCard;
using RedBjorn.ProtoTiles.Example;

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
    [SerializeField] private SpellCardManager spellCardManager;

    #region Turn Events
    public event Action<PlayerID> OnPlayerTurnStarted;
    public event Action<PlayerID> OnPlayerTurnEnded;
    #endregion

    #region  MP Events
    public event Action<PlayerID, int, int> OnMPChanged; // (player, currentMP, maxMP)
    #endregion

    #region  Visualize Path Events
    public event Action<UnitController> OnUnitSelected;
    public event Action<UnitController> OnUnitDeselected;
    #endregion

    #region Unit Movement Events
    public event Action<UnitController, Vector3Int, Vector3Int> OnUnitMoved;
    #endregion

    #region Capture Point Events
    public event Action<Vector3Int, PlayerID> OnCapturePointCaptured;
    public event Action<PlayerID> OnGameEnd;
    #endregion

    #region Spell Card Events
    public event Action<SpellCardData, PlayerID> OnSpellCardUsed;
    public event Action<PlayerID> OnHandChanged;
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
        if (mapManager == null || !mapManager.IsReady)
        {
            Debug.LogError("[GameMediator] Map chưa sẵn sàng. Dừng khởi tạo trận đấu.", this);
            enabled = false;
            return;
        }

        turnManager.Initialize(this);
        mpManager.Initialize(this);
        mapManager.Initialize(this);
        unitSpawner.Initialize(this);
        areaPathManager.Initialize(this);
        spellCardManager.SetCachedMap(mapManager.MapEntity);    // spell card sẽ được initialize trong PlayerController
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

    public void NotifySpawnUnit(UnitController unit, int mpSpent)
    {
        mpManager.SpendMP(unit.GetOwner(), mpSpent);
    }

    public void NotifyUnitSelected(UnitController unit)
    {
        OnUnitSelected?.Invoke(unit);
    }

    public void NotifyUnitDeselected(UnitController unit)
    {
        OnUnitDeselected?.Invoke(unit);
    }

    public void NotifyUnitMoved(UnitController unit, Vector3Int oldPos, Vector3Int newPos)
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

    public void NotifySpellCardUsed(SpellCardData card, PlayerID caster)
    {
        OnSpellCardUsed?.Invoke(card, caster);
    }
    
    public void NotifyHandChanged(PlayerID player)
    {
        OnHandChanged?.Invoke(player);
    }

    #endregion
}
