using System.Collections;
using TurnBasedGame.Core;
using TurnBasedGame.SpellCard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellCardItem : CardUI
{
    [SerializeField] protected SpellCardData spellData;
    private PlayerID _ownerPlayer;

    public SpellCardData Data => spellData;

    public override void Setup(SpellCardData data, PlayerID owner)
    {
        base.Setup(data, owner);
        spellData = data;
        _ownerPlayer = owner;
        if (titleText != null) titleText.text = data.spellName;
        if (descText != null) descText.text = data.description;
        if (costText != null) costText.text = data.mpCost.ToString();
        if (_costBg != null) _costBg.color = Color.blue;
        if (_icon != null) _icon.sprite = data.icon;
    }

    public override void OnCardClicked()
    {
        SpellCardManager.Instance?.SelectCard(spellData, _ownerPlayer, _rectTransform);
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }
}
