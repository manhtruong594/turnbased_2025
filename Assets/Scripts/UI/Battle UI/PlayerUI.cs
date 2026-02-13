using TMPro;
using TurnBasedGame.Core;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Info Panel")]
    public Image avatarImage;
    public TextMeshProUGUI nameText;
    [SerializeField] private GameObject _actionPanel;

    [Header("Panels")]
    [SerializeField] private GameObject _indicatorObj;
    [SerializeField] private GameObject _spawnPanel;
    [SerializeField]private GameObject _spellPanel;
    
    [SerializeField] Button _spawnUnitButton;
    [SerializeField] Button _spellButton;

    [SerializeField] IntReference _actionsLeft;

    void Start()
    {
        _spawnUnitButton?.onClick.AddListener(ToggleSpawnPanel);
        _spellButton?.onClick.AddListener(ToggleSpellPanel);
    }
 
    private void ToggleSpawnPanel()
    {
       _spawnPanel.SetActive(!_spawnPanel.activeSelf);
    }

    private void ToggleSpellPanel()
    {
        _spellPanel.SetActive(!_spellPanel.activeSelf);
    }
    
    public void Setup(string playerName)
    {
        if (nameText) nameText.text = playerName;
    }

    public void ShowActionPanel(bool show)
    {
        if (_actionPanel != null)
        {
            _actionPanel.SetActive(show);
        }
        _indicatorObj.SetActive(show);
        _spawnUnitButton.interactable = show;
        _spellButton.interactable = show;
        if (!show)
        {
            _spawnPanel.SetActive(false);
            _spellPanel.SetActive(false);
        }
    }
}
