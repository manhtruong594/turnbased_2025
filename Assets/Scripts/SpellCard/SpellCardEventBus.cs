using TurnBasedGame.Core;
using UnityEngine;
using TurnBasedGame.Unit;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Observer Pattern - Event bus riêng cho Spell Card system.
    /// Hỗ trợ debug, logging và reactive UI.
    /// </summary>
    public class SpellCardEventBus : MonoBehaviour
    {
        public static SpellCardEventBus Instance { get; private set; }

        public event System.Action<SpellCardData, UnitController, PlayerID> OnSpellCardUsed;
        public event System.Action<ActiveBuff, UnitController> OnBuffApplied;
        public event System.Action<ActiveBuff, UnitController> OnBuffExpired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void TriggerSpellCardUsed(SpellCardData card, UnitController target, PlayerID caster)
        {
            OnSpellCardUsed?.Invoke(card, target, caster);
            Debug.Log($"[SpellLog] {caster} dùng [{card.spellName}] | Effect: {card.effectType} ({card.effectValue}) | Target: {target.name}");
        }

        public void TriggerBuffApplied(ActiveBuff buff, UnitController unit)
        {
            OnBuffApplied?.Invoke(buff, unit);
        }

        public void TriggerBuffExpired(ActiveBuff buff, UnitController unit)
        {
            OnBuffExpired?.Invoke(buff, unit);
        }
    }
}
