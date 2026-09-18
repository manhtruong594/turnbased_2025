using TurnBasedGame.Core;
using TurnBasedGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hiển thị màn hình kết thúc trận đấu (Win / Lose).
/// Lắng nghe sự kiện OnGameEnd từ GameMediator.
/// </summary>
public class UI_Endgame : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerID _localPlayerID = PlayerID.Player1;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    [Header("Text Override")]
    [SerializeField] private string _winText = "CHIẾN THẮNG";
    [SerializeField] private string _loseText = "THẤT BẠI";
    [SerializeField] private string _winSubtitle = "Xuất sắc! Bạn đã giành chiến thắng.";
    [SerializeField] private string _loseSubtitle = "Đừng nản lòng, hãy thử lại!";

    private void Start()
    {
        if (GameMediator.Instance != null)
            GameMediator.Instance.OnGameEnd += HandleGameEnd;
    }

    private void OnDestroy()
    {
        if (GameMediator.Instance != null)
            GameMediator.Instance.OnGameEnd -= HandleGameEnd;
    }

    private void HandleGameEnd(PlayerID winner)
    {
        _localPlayerID = MatchContext.LocalPlayer;
        bool isWinner = winner == _localPlayerID;
        BattleHUDToolkit.Instance?.ShowEndgame(
            isWinner ? _winText : _loseText,
            isWinner ? _winSubtitle : _loseSubtitle,
            OnRestartClicked,
            OnMainMenuClicked);
    }

    private async void OnRestartClicked()
    {
        if (TurnBasedGame.Multiplayer.MatchSessionController.Instance != null)
        {
            await TurnBasedGame.Multiplayer.MatchSessionController.Instance.SetReady();
            return;
        }
        if (TurnBasedGame.Multiplayer.MatchGameplayBootstrap.Active)
        {
            Debug.LogWarning("LAN development entry point: start a new pair of processes, or use a service session for rematch.");
            return;
        }
        SceneLoader.Instance?.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }

    private async void OnMainMenuClicked()
    {
        if (TurnBasedGame.Multiplayer.MatchSessionController.Instance != null)
        {
            await TurnBasedGame.Multiplayer.MatchSessionController.Instance.Leave();
            return;
        }
        SceneLoader.Instance?.LoadSceneAsync(_mainMenuSceneName);
    }
}
