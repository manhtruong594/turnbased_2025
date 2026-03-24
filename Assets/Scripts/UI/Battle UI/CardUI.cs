using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using TurnBasedGame.SpellCard;

public class CardUI : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI costText;
    public Image costBg;

    public virtual void Setup(SpellCardData data)
    {
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
       
    }
}
