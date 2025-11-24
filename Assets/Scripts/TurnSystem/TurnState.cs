namespace TurnBasedGame.Core
{
    /// <summary>
    /// Định nghĩa các trạng thái trong game turn-based
    /// </summary>
    public enum TurnState
    {
        Initialization,     // Khởi tạo trận đấu
        Player1Turn,        // Lượt của Player 1
        Player2Turn,        // Lượt của Player 2
        GameEnd            // Kết thúc trận đấu
    }

    /// <summary>
    /// Định nghĩa ID người chơi
    /// </summary>
    public enum PlayerID
    {
        Player1 = 1,
        Player2 = 2
    }
}
