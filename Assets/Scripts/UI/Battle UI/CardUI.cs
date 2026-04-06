using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Core;

public class CardUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI costText;
    [SerializeField] protected Image _costBg;
    [SerializeField] protected Image _icon;
    public Button button;
    protected RectTransform _rectTransform;

    public virtual void Setup(SpellCardData data, PlayerID owner)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnCardClicked);
        }
        _rectTransform = GetComponent<RectTransform>();
    }

    public virtual void OnCardClicked()
    {
    }
}
