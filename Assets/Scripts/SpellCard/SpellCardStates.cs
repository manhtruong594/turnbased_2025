using RedBjorn.ProtoTiles.Example;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// State Pattern — Base class cho các trạng thái của SpellCardManager.
    /// Mỗi state tự quản lý input, hiển thị và transition logic riêng,
    /// loại bỏ boolean flags (_isTargeting, _isConfirming).
    /// </summary>
    public abstract class SpellCardState
    {
        public virtual void Enter(SpellCardManager ctx) { }
        public virtual void Update(SpellCardManager ctx) { }
        public virtual void Exit(SpellCardManager ctx) { }
        public virtual void OnConfirm(SpellCardManager ctx) { }
        public virtual void OnCancel(SpellCardManager ctx) { }
    }

    /// <summary>Trạng thái chờ — player chưa chọn card nào.</summary>
    public class SpellIdleState : SpellCardState
    {
        public override void Enter(SpellCardManager ctx)
        {
            ctx.SelectedCard = null;
            AreaPathManager.Instance?.HideSpellArea();
            ctx.NotifyCardDeselected();
        }
    }

    /// <summary>Trạng thái chọn target — hiển thị phạm vi spell, chờ click tile.</summary>
    public class SpellTargetingState : SpellCardState
    {
        public override void Enter(SpellCardManager ctx)
        {
            ctx.NotifyCardSelected();
        }

        public override void Update(SpellCardManager ctx)
        {
            var map = ctx.CachedMap;
            if (map == null) return;

            var mousePos = MyInput.GroundPosition(map.Settings.Plane());

            if (MyInput.GetOnWorldUp(map.Settings.Plane()) && !EventSystem.current.IsPointerOverGameObject())
            {
                var tileClicked = map.Tile(mousePos);
                if (tileClicked == null) return;

                ctx.CachedTile = tileClicked;
                ctx.SetState(new SpellConfirmingState());
                return;
            }

            ctx.ShowSpellRange();
        }
    }

    /// <summary>Trạng thái xác nhận — hiển thị ConfirmUI, chờ Confirm/Cancel.</summary>
    public class SpellConfirmingState : SpellCardState
    {
        public override void Enter(SpellCardManager ctx)
        {
            ctx.ShowConfirmUI();
        }

        public override void OnConfirm(SpellCardManager ctx)
        {
            var command = new CastSpellCommand(ctx.SelectedCard, ctx.CurrentCaster, ctx.CachedTile, ctx);
            if (command.CanExecute())
            {
                command.Execute();
            }
            else
            {
                Debug.Log("[Spell] Cannot execute spell on this tile.");
            }
            ctx.SetState(new SpellIdleState());
        }

        public override void OnCancel(SpellCardManager ctx)
        {
            ctx.SetState(new SpellIdleState());
        }

        public override void Exit(SpellCardManager ctx)
        {
            ctx.ConfirmUI?.Hide();
        }
    }
}
