using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using System;

/// <summary>
/// Quản lý UI và logic tung xúc xắc
/// - Lượt 1: Tung 2 xúc xắc bắt buộc
/// - Lượt 2+: Chọn tung 1 xúc xắc, 2 xúc xắc, hoặc không tung
/// </summary>
public class DiceUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dice1Text;
    [SerializeField] private TextMeshProUGUI dice2Text;
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private GameObject dicePanel;

    [Header("Action Buttons")]
    [SerializeField] private Button rollDiceButton2;
    [SerializeField] private Button rollDiceButton1;

    [Header("Animation Settings")]
    [SerializeField] private float rollDuration = 0.8f;
    [SerializeField] private float rollInterval = 0.06f;
    [SerializeField] private IntReference _actionsLeft;
   
    private bool isRolling = false;

    private void Awake()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (rollDiceButton2) rollDiceButton2.onClick.AddListener(() => StartCoroutine(RollDiceCoroutine(2)));
        if (rollDiceButton1) rollDiceButton1.onClick.AddListener(() => StartCoroutine(RollDiceCoroutine(1)));
        _actionsLeft.AddListener(UpdateActionsLeft);
    }

    private void UpdateActionsLeft(int obj)
    {
        if (obj <= 0)
        {
            DisableAllButtons();
        }
    }

    /// <summary>
    /// Hiển thị panel tung xúc xắc khi bắt đầu lượt
    /// </summary>
    public void ActiveDicePanel(bool isActive)
    {
        if (isActive)
        {
            EnableButtons();
            ResetDiceDisplay();
        }
        else
        {
            DisableAllButtons();
        }
    }

    /// <summary>
    /// Tung xúc xắc với animation
    /// </summary>
    private IEnumerator RollDiceCoroutine(int indexOfDice)
    {
        if (isRolling || _actionsLeft.Value <= 0) yield break;
        
        isRolling = true;
        DisableAllButtons();

        int diceValue = 0;

        // Animation tung xúc xắc
        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            diceValue = UnityEngine.Random.Range(1, 7);
            
            if (indexOfDice == 1)
            {
                if (dice1Text) dice1Text.text = diceValue.ToString();
            }
            else if (indexOfDice == 2)
            {
                if (dice2Text) dice2Text.text = diceValue.ToString();
            }

            elapsed += rollInterval;
            yield return new WaitForSeconds(rollInterval);
        }

        // Kết quả cuối cùng
        diceValue = UnityEngine.Random.Range(1, 7);
        
        if (indexOfDice == 1)
        {
            if (dice1Text) dice1Text.text = diceValue.ToString();
        }
        else if (indexOfDice == 2)
        {
            if (dice2Text) dice2Text.text = diceValue.ToString();
        }

        if (totalText) totalText.text = $"Total: {diceValue}";

        Debug.Log($"Rolled dice {indexOfDice}: {diceValue}");

        // Thêm MP cho người chơi hiện tại
        AddMPToCurrentPlayer(diceValue);
 
        _actionsLeft.Value--;
        // Ẩn button vừa được sử dụng
        HideUsedButton(indexOfDice);

        yield return new WaitForSeconds(0.5f);
        isRolling = false;
    }

    /// <summary>
    /// Ẩn button đã được sử dụng
    /// </summary>
    private void HideUsedButton(int indexOfDice)
    {
        if (indexOfDice == 1 && rollDiceButton1)
        {
            rollDiceButton1.interactable = false;
        }
        else if (indexOfDice == 2 && rollDiceButton2)
        {
            rollDiceButton2.interactable = false;
        }
    }

    /// <summary>
    /// Thêm MP vào người chơi hiện tại
    /// </summary>
    private void AddMPToCurrentPlayer(int amount)
    {
        if (MPManager.Instance == null || TurnManager.Instance == null) return;

        PlayerID currentPlayer = TurnManager.Instance.CurrentPlayer;
        MPManager.Instance.AddMP(currentPlayer, amount);
    }

    /// <summary>
    /// Reset hiển thị xúc xắc
    /// </summary>
    private void ResetDiceDisplay()
    {
        if (dice1Text) dice1Text.text = "?";
        if (dice2Text) dice2Text.text = "?";
        if (totalText) totalText.text = "Total: 0";
    }

    /// <summary>
    /// Vô hiệu hóa tất cả nút khi đang tung
    /// </summary>
    private void DisableAllButtons()
    {
        if (rollDiceButton2) rollDiceButton2.interactable = false;
        if (rollDiceButton1) rollDiceButton1.interactable = false;
    }

    /// <summary>
    /// Kích hoạt lại các nút
    /// </summary>
    private void EnableButtons()
    {
        if (rollDiceButton2)
        {
            rollDiceButton2.interactable = true;
        }
        if (rollDiceButton1)
        {
            rollDiceButton1.interactable = true;
        }
    }
}
