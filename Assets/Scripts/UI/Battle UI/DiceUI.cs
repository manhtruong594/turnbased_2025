using System.Collections;
using UnityEngine;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.UI;
using System;

/// <summary>
/// Điều phối thao tác tung xúc xắc cho BattleHUDToolkit.
/// </summary>
public class DiceUI : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float rollDuration = 0.8f;
    [SerializeField] private float rollInterval = 0.06f;
    [SerializeField] private IntReference _actionsLeft;
   
    private bool isRolling = false;
    private int usedButtons;

    public void BindToolkit(PlayerID player)
    {
        BattleHUDToolkit.Instance?.BindDice(player, () => TriggerRoll(player, 1), () => TriggerRoll(player, 2));
        ResetDiceDisplay(player);
    }

    public void TriggerRoll(int index)
    {
        if (TurnManager.Instance != null)
            TriggerRoll(TurnManager.Instance.CurrentPlayer, index);
    }

    private void TriggerRoll(PlayerID player, int index)
    {
        if (!isRolling && TurnBasedGame.Multiplayer.MatchGameplayBootstrap.CanControl(player))
            StartCoroutine(RollDiceCoroutine(player, index));
    }
    internal void ApplyReplica(bool active, int used)
    {
        usedButtons = used;
        _actionsLeft.Value = 2 - ((used & 2) != 0 ? 1 : 0) - ((used & 4) != 0 ? 1 : 0);
        if (active && !isRolling) EnableButtons(MatchContext.LocalPlayer);
        else DisableAllButtons(MatchContext.LocalPlayer);
    }

    private void OnDestroy()
    {
        if (_actionsLeft != null) _actionsLeft.RemoveListener(UpdateActionsLeft);
    }

    private void Awake()
    {
        _actionsLeft.AddListener(UpdateActionsLeft);
    }

    private void UpdateActionsLeft(int obj)
    {
        if (obj <= 0)
        {
            PlayerID player = TurnManager.Instance != null
                ? TurnManager.Instance.CurrentPlayer
                : MatchContext.LocalPlayer;
            DisableAllButtons(player);
        }
    }

    /// <summary>
    /// Hiển thị panel tung xúc xắc khi bắt đầu lượt
    /// </summary>
    public void ActiveDicePanel(PlayerID player, bool isActive)
    {
        if (isActive)
        {
            usedButtons = 0;
            EnableButtons(player);
            ResetDiceDisplay(player);
        }
        else
        {
            DisableAllButtons(player);
        }
    }

    /// <summary>
    /// Tung xúc xắc với animation
    /// </summary>
    private IEnumerator RollDiceCoroutine(PlayerID actor, int indexOfDice)
    {
        if (isRolling || _actionsLeft.Value <= 0) yield break;
        
        if (TurnManager.Instance == null) yield break;
        if (TurnManager.Instance.CurrentPlayer != actor) yield break;
        int expectedTurn = TurnManager.Instance.TurnCount;
        isRolling = true;
        DisableAllButtons(actor);

        int diceValue = 0;

        // Animation tung xúc xắc
        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            diceValue = UnityEngine.Random.Range(1, 7);
            
            if (indexOfDice == 1)
            {
                BattleHUDToolkit.Instance?.SetDice(actor, 1, diceValue.ToString());
            }
            else if (indexOfDice == 2)
            {
                BattleHUDToolkit.Instance?.SetDice(actor, 2, diceValue.ToString());
            }

            elapsed += rollInterval;
            yield return new WaitForSeconds(rollInterval);
        }

        // Kết quả cuối cùng
        TurnBasedGame.Command.MatchCommandResult? received = null;
        var submitted = TurnBasedGame.Command.LocalMatchAuthority.SubmitHumanRoll(expectedTurn, indexOfDice,
            result => received = result);
        if (!submitted.Pending) received = submitted;
        while (!received.HasValue) yield return null;
        if (!received.Value.Succeeded)
        {
            isRolling = false;
            EnableButtons(actor);
            yield break;
        }
        diceValue = received.Value.Acknowledgement.DiceValue;
        if (TurnManager.Instance == null || TurnManager.Instance.CurrentPlayer != actor ||
            TurnManager.Instance.TurnCount != expectedTurn) { isRolling = false; yield break; }
        usedButtons |= 1 << indexOfDice;
        
        if (indexOfDice == 1)
        {
            BattleHUDToolkit.Instance?.SetDice(actor, 1, diceValue.ToString());
        }
        else if (indexOfDice == 2)
        {
            BattleHUDToolkit.Instance?.SetDice(actor, 2, diceValue.ToString());
        }

        Debug.Log($"Rolled dice {indexOfDice}: {diceValue}");

        // Thêm MP cho người chơi hiện tại

        EnableButtons(actor);
        HideUsedButton(actor, indexOfDice);
        if (TurnBasedGame.Multiplayer.MatchGameplayBootstrap.Active)
            _actionsLeft.Value = 2 - ((usedButtons & 2) != 0 ? 1 : 0) - ((usedButtons & 4) != 0 ? 1 : 0);
        else _actionsLeft.Value--;
        yield return new WaitForSeconds(0.2f);
        isRolling = false;
    }

    /// <summary>
    /// Ẩn button đã được sử dụng
    /// </summary>
    private void HideUsedButton(PlayerID player, int indexOfDice)
    {
        if (indexOfDice == 1)
        {
            BattleHUDToolkit.Instance?.SetDiceEnabled(player, 1, false);
        }
        else if (indexOfDice == 2)
        {
            BattleHUDToolkit.Instance?.SetDiceEnabled(player, 2, false);
        }
    }

    /// <summary>
    /// Reset hiển thị xúc xắc
    /// </summary>
    private void ResetDiceDisplay(PlayerID player)
    {
        BattleHUDToolkit.Instance?.SetDice(player, 1, "?");
        BattleHUDToolkit.Instance?.SetDice(player, 2, "?");
    }

    /// <summary>
    /// Vô hiệu hóa tất cả nút khi đang tung
    /// </summary>
    private void DisableAllButtons(PlayerID player)
    {
        BattleHUDToolkit.Instance?.SetDiceEnabled(player, 1, false);
        BattleHUDToolkit.Instance?.SetDiceEnabled(player, 2, false);
    }

    /// <summary>
    /// Kích hoạt lại các nút
    /// </summary>
    private void EnableButtons(PlayerID player)
    {
        if (TurnManager.Instance != null && !TurnBasedGame.Multiplayer.MatchGameplayBootstrap.CanControl(TurnManager.Instance.CurrentPlayer))
        { DisableAllButtons(player); return; }
        BattleHUDToolkit.Instance?.SetDiceEnabled(player, 1, (usedButtons & (1 << 1)) == 0);
        BattleHUDToolkit.Instance?.SetDiceEnabled(player, 2, (usedButtons & (1 << 2)) == 0);
    }
}
