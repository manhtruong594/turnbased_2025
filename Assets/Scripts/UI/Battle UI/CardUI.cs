using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI costText;
    public Image costBg;

    public void Setup()
    {
    }

    public void OnPointerClick(PointerEventData eventData)
    {
       
    }
}
