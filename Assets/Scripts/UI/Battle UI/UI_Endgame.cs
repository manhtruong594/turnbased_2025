using TMPro;
using TurnBasedGame.Core;
using TurnBasedGame.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị màn hình kết thúc trận đấu (Win / Lose).
/// Gắn trên panel GameObject trong HUD Canvas.
/// Lắng nghe sự kiện OnGameEnd từ GameMediator.
/// </summary>
public class UI_Endgame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _subtitleText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Config")]
    [SerializeField] private PlayerID _localPlayerID = PlayerID.Player1;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";
    [SerializeField] private string _battleSceneName = "HUDScene";

    [Header("Text Override")]
    [SerializeField] private string _winText = "CHIẾN THẮNG";
    [SerializeField] private string _loseText = "THẤT BẠI";
    [SerializeField] private string _winSubtitle = "Xuất sắc! Bạn đã giành chiến thắng.";
    [SerializeField] private string _loseSubtitle = "Đừng nản lòng, hãy thử lại!";

    private void Start()
    {
        _panel.SetActive(false);
        _restartButton?.onClick.AddListener(OnRestartClicked);
        _mainMenuButton?.onClick.AddListener(OnMainMenuClicked);

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
        bool isWinner = winner == _localPlayerID;
        _resultText.text = isWinner ? _winText : _loseText;
        _subtitleText.text = isWinner ? _winSubtitle : _loseSubtitle;
        _panel.SetActive(true);
    }

    private void OnRestartClicked()
    {
        SceneLoader.Instance?.LoadSceneAsync(_battleSceneName);
    }

    private void OnMainMenuClicked()
    {
        SceneLoader.Instance?.LoadSceneAsync(_mainMenuSceneName);
    }
}
