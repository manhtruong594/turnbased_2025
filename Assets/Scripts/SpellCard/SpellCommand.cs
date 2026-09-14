using RedBjorn.ProtoTiles;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
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
            if (!CanExecute()) return;
            var result = TurnBasedGame.Command.LocalMatchAuthority.SubmitSpell(_caster,
                _manager.GetCardInstanceId(_caster, _card), _targetTile.Position);
            if (!result.Succeeded) Debug.LogWarning(result.FailureReason);
        }

    }
}
