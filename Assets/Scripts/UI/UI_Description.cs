using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_Description : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Button _myButton;
    [SerializeField] GameObject _descriptionPanel;

    void Start()
    {
        if (_descriptionPanel != null)
        {
            _descriptionPanel.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_descriptionPanel != null)
        {
            _descriptionPanel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_descriptionPanel != null)
        {
            _descriptionPanel.SetActive(false);
        }
    }
}
