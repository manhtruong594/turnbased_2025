using RedBjorn.ProtoTiles;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Command Pattern — Interface cho các lệnh spell execution.
    /// Cho phép encapsulate, queue, và (tương lai) undo/replay spell actions.
    /// </summary>
    public interface ISpellCommand
    {
        bool CanExecute();
        void Execute();
    }

    /// <summary>
    /// Command cụ thể: cast spell card lên tile/target.
    /// Đóng gói toàn bộ logic: trừ MP, validate target, apply effect, tiêu hao card, notify.
    /// </summary>
    public class CastSpellCommand : ISpellCommand
    {
        private readonly SpellCardData _card;
        private readonly PlayerID _caster;
        private readonly TileEntity _targetTile;
        private readonly SpellCardManager _manager;

        public CastSpellCommand(SpellCardData card, PlayerID caster, TileEntity targetTile, SpellCardManager manager)
        {
            _card = card;
            _caster = caster;
            _targetTile = targetTile;
            _manager = manager;
        }

        public bool CanExecute()
        {
            return _card != null
                && _targetTile != null
                && MPManager.Instance.HasEnoughMP(_caster, _card.mpCost);
        }

        public void Execute()
        {
            if (!MPManager.Instance.SpendMP(_caster, _card.mpCost))
            {
                Debug.LogWarning($"[Spell] Không đủ MP để dùng {_card.spellName}");
                return;
            }

            ExecuteOnTargets();

            if (_card.consumeOnUse)
                _manager.RemoveCardFromHand(_caster, _card);

            GameMediator.Instance?.NotifySpellCardUsed(_card, _caster);
            Debug.Log($"[Spell] {_caster} dùng {_card.spellName}");
        }

        private void ExecuteOnTargets()
        {
            if (_card.range == 0)
            {
                var allUnits = _manager.GetAllUnitsOnMap();
                foreach (var unit in allUnits)
                {
                    if (_manager.ValidateTarget(_card, _caster, unit))
                        ExecuteOnUnit(unit);
                }
            }
            else if (IsMultiTarget(_card.targetType))
            {
                var unitsInRange = MapManager.Instance.GetUnitsInRange(_targetTile, _card.range);
                foreach (var unit in unitsInRange)
                {
                    if (_manager.ValidateTarget(_card, _caster, unit))
                        ExecuteOnUnit(unit);
                }
            }
            else
            {
                var target = MapManager.Instance.GetUnitAtTile(_targetTile.Position);
                if (target != null)
                    ExecuteOnUnit(target);
            }
        }

        private void ExecuteOnUnit(UnitController target)
        {
            _manager.SpawnSpellVfx(_card, target);
            _card.Cast(_caster, target);
        }

        private static bool IsMultiTarget(SpellTargetType type)
        {
            return type == SpellTargetType.AllAllies
                || type == SpellTargetType.AllEnemies
                || type == SpellTargetType.AnyUnit;
        }
    }
}
