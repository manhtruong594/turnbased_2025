using TurnBasedGame.Core;
using TurnBasedGame.SpellCard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellCardBase : CardUI
{
    [SerializeField] protected SpellCardData spellData;
    private PlayerID _ownerPlayer;
    private Button _button;

    public SpellCardData Data => spellData;

    public void Setup(SpellCardData data, PlayerID owner)
    {
        spellData = data;
        _ownerPlayer = owner;
        if (titleText != null) titleText.text = data.spellName;
        if (descText != null) descText.text = data.description;
        if (costText != null) costText.text = data.mpCost.ToString();
        if (costBg != null) costBg.color = Color.blue;
    }

    public override void Setup(SpellCardData data)
    {
        Setup(data, PlayerID.Player1);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        SpellCardManager.Instance?.SelectCard(spellData, _ownerPlayer);
    }

    public void SetInteractable(bool interactable)
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_button != null) _button.interactable = interactable;
    }
}
