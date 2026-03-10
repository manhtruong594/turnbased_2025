using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Core;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Observer Pattern - Event bus riêng cho Spell Card system.
    /// Hỗ trợ debug, logging và reactive UI.
    /// </summary>
    public class SpellCardEventBus : MonoBehaviour
    {
        public static SpellCardEventBus Instance { get; private set; }

        public event System.Action<SpellCardData, UnitMove, PlayerID> OnSpellCardUsed;
        public event System.Action<ActiveBuff, UnitMove> OnBuffApplied;
        public event System.Action<ActiveBuff, UnitMove> OnBuffExpired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void TriggerSpellCardUsed(SpellCardData card, UnitMove target, PlayerID caster)
        {
            OnSpellCardUsed?.Invoke(card, target, caster);
            Debug.Log($"[SpellLog] {caster} dùng [{card.spellName}] | Effect: {card.effectType} ({card.effectValue}) | Target: {target.name}");
        }

        public void TriggerBuffApplied(ActiveBuff buff, UnitMove unit)
        {
            OnBuffApplied?.Invoke(buff, unit);
        }

        public void TriggerBuffExpired(ActiveBuff buff, UnitMove unit)
        {
            OnBuffExpired?.Invoke(buff, unit);
        }
    }
}
