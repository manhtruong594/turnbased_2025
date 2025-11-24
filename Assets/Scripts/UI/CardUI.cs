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
    private CardData data;
    private UIManager uiManager;

    public void Setup(CardData d, UIManager manager)
    {
        data = d;
        uiManager = manager;
        if (titleText) titleText.text = d.title;
        if (descText) descText.text = d.description;
        if (costText) costText.text = d.cost.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (uiManager == null || data == null) return;
        // simple interaction: attempt to spend MP and play card
        if (uiManager.HasMP(data.cost))
        {
            uiManager.SpendMP(data.cost);
            uiManager.Log($"Played card: {data.title} (cost {data.cost})");
            // visual feedback: destroy or disable
            Destroy(gameObject);
        }
        else
        {
            uiManager.Log($"Not enough MP for {data.title} (cost {data.cost}).");
            // maybe pulse the costBg or play an animation (left as exercise)
        }
    }
}
