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

    [Header("Action Buttons")]
    public Button EndTurnButton;
    [SerializeField] DiceUI _diceUI;
    [SerializeField] Button _spawnUnitButton;
    [SerializeField] Button _spellButton;

    [SerializeField] IntReference _actionsLeft;

    void Start()
    {
        if (EndTurnButton != null)
        {
            EndTurnButton.onClick.AddListener(() =>
            {
                TurnManager.Instance?.EndCurrentTurn();
            });
        }
        _spawnUnitButton?.onClick.AddListener(SpawnUnitButtonHandler);
        _spellButton?.onClick.AddListener(SpellButtonHandler);
    }

    void OnEnable()
    {

    }

    private void SpawnUnitButtonHandler()
    {
        if (_actionsLeft.Value <= 0) return;
        _spawnUnitButton.interactable = false;
        _actionsLeft.Value -= 1;
    }

    private void SpellButtonHandler()
    {
        if (_actionsLeft.Value <= 0) return;
        _spellButton.interactable = false;  
        _actionsLeft.Value -= 1;
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
        if (EndTurnButton != null)
        {
            EndTurnButton.interactable = show;
        }
        _spawnUnitButton.interactable = show;
        _spellButton.interactable = show;
        _diceUI.ActiveDicePanel(show);
    }
}
