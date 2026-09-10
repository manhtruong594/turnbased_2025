using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    public enum TileHazardType
    {
        BurningGround
    }

    public sealed class TileHazardInstance
    {
        public TileHazardType Type { get; }
        public Vector3Int Position { get; }
        public PlayerID Owner { get; }
        public int Value { get; }
        public float ApplyChance { get; }
        public int StatusDuration { get; }
        public int RemainingTurns { get; private set; }

        public TileHazardInstance(
            TileHazardType type,
            Vector3Int position,
            int duration,
            PlayerID owner,
            int value,
            float applyChance,
            int statusDuration)
        {
            Type = type;
            Position = position;
            RemainingTurns = duration;
            Owner = owner;
            Value = value;
            ApplyChance = applyChance;
            StatusDuration = statusDuration;
        }

        public bool TickTurn()
        {
            RemainingTurns--;
            return RemainingTurns <= 0;
        }
    }

    public class TileHazardManager : MonoBehaviour
    {
        private readonly List<TileHazardInstance> _hazards = new();
        private bool _subscribed;

        public static TileHazardManager Instance { get; private set; }

        public static TileHazardManager InstanceOrCreate
        {
            get
            {
                if (Instance != null)
                    return Instance;

                var gameObject = new GameObject(nameof(TileHazardManager));
                return gameObject.AddComponent<TileHazardManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SubscribeMediatorEvents();
        }

        private void OnEnable()
        {
            SubscribeMediatorEvents();
        }

        private void Start()
        {
            SubscribeMediatorEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeMediatorEvents();
            if (Instance == this)
                Instance = null;
        }

        public void AddHazard(TileHazardType type, Vector3Int position, int duration, PlayerID owner, int value)
        {
            AddHazard(type, position, duration, owner, value, 0.5f, 2);
        }

        public void AddHazard(
            TileHazardType type,
            Vector3Int position,
            int duration,
            PlayerID owner,
            int value,
            float applyChance,
            int statusDuration)
        {
            SubscribeMediatorEvents();

            if (MapManager.Instance?.MapEntity?.Tile(position) == null)
                return;

            RemoveHazardAt(type, position);
            _hazards.Add(new TileHazardInstance(type, position, duration, owner, value, applyChance, statusDuration));
        }

        public void TickHazards(PlayerID activePlayer)
        {
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                if (_hazards[i].TickTurn())
                    _hazards.RemoveAt(i);
            }
        }

        public void TryApplyEnterTileEffect(UnitController unit, Vector3Int tilePos)
        {
            TryApplyHazards(unit, tilePos);
        }

        public void TryApplyTurnStartEffect(UnitController unit)
        {
            if (unit == null)
                return;

            TryApplyHazards(unit, unit.currentGridPosition);
        }

        private void SubscribeMediatorEvents()
        {
            if (_subscribed || GameMediator.Instance == null)
                return;

            GameMediator.Instance.OnUnitMoved += OnUnitMoved;
            GameMediator.Instance.OnPlayerTurnEnded += TickHazards;
            _subscribed = true;
        }

        private void UnsubscribeMediatorEvents()
        {
            if (!_subscribed || GameMediator.Instance == null)
                return;

            GameMediator.Instance.OnUnitMoved -= OnUnitMoved;
            GameMediator.Instance.OnPlayerTurnEnded -= TickHazards;
            _subscribed = false;
        }

        private void OnUnitMoved(UnitController unit, Vector3Int oldPos, Vector3Int newPos)
        {
            TryApplyEnterTileEffect(unit, newPos);
        }

        private void RemoveHazardAt(TileHazardType type, Vector3Int position)
        {
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                if (_hazards[i].Type == type && _hazards[i].Position == position)
                    _hazards.RemoveAt(i);
            }
        }

        private void TryApplyHazards(UnitController unit, Vector3Int tilePos)
        {
            if (unit == null || unit.IsDead())
                return;

            foreach (var hazard in _hazards)
            {
                if (hazard.Position == tilePos)
                    ApplyHazardEffect(unit, hazard);
            }
        }

        private void ApplyHazardEffect(UnitController unit, TileHazardInstance hazard)
        {
            if (hazard.Type != TileHazardType.BurningGround || unit.GetOwner() == hazard.Owner)
                return;

            if (TurnBasedGame.Command.LocalMatchAuthority.Random.Value() > hazard.ApplyChance)
                return;

            unit.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.Burn,
                hazard.Value,
                hazard.StatusDuration,
                hazard.Owner));
        }
    }
}
