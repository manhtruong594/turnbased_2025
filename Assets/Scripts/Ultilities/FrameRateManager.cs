using UnityEngine;

namespace TurnBasedGame.Utilities
{
    public static class FrameRateManager
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeFrameRate()
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
        }
    }
}
