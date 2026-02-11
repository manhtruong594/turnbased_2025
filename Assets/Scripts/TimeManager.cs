using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý thời gian trong game (freeze time, slow motion)
/// Singleton pattern để dễ truy cập
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    private float normalTimeScale = 1f;
    private Coroutine timeScaleCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Freeze time hoàn toàn (timeScale = 0)
    /// </summary>
    public void FreezeTime()
    {
        StopTimeScaleCoroutine();
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Freeze time trong khoảng thời gian nhất định
    /// </summary>
    /// <param name="duration">Thời gian freeze (giây thực)</param>
    public void FreezeTime(float duration)
    {
        StopTimeScaleCoroutine();
        Time.timeScale = 0f;
        timeScaleCoroutine = StartCoroutine(ResumeTimeAfterDelay(duration));
    }

    /// <summary>
    /// Resume time về bình thường
    /// </summary>
    public void ResumeTime()
    {
        StopTimeScaleCoroutine();
        Time.timeScale = normalTimeScale;
    }

    /// <summary>
    /// Slow motion với tốc độ tùy chỉnh
    /// </summary>
    /// <param name="slowFactor">Tốc độ chậm (0.0 - 1.0), ví dụ: 0.5 = 50% tốc độ</param>
    public void SlowMotion(float slowFactor)
    {
        StopTimeScaleCoroutine();
        Time.timeScale = Mathf.Clamp(slowFactor, 0f, 1f);
    }

    /// <summary>
    /// Slow motion trong khoảng thời gian nhất định
    /// </summary>
    /// <param name="slowFactor">Tốc độ chậm (0.0 - 1.0)</param>
    /// <param name="duration">Thời gian slow motion (giây thực)</param>
    public void SlowMotion(float slowFactor, float duration)
    {
        StopTimeScaleCoroutine();
        timeScaleCoroutine = StartCoroutine(SlowMotionCoroutine(slowFactor, duration));
    }

    /// <summary>
    /// Set time scale mặc định
    /// </summary>
    /// <param name="timeScale">Time scale mới</param>
    public void SetNormalTimeScale(float timeScale)
    {
        normalTimeScale = Mathf.Max(0f, timeScale);
        Time.timeScale = normalTimeScale;
    }

    /// <summary>
    /// Lấy time scale hiện tại
    /// </summary>
    public float GetCurrentTimeScale() => Time.timeScale;

    /// <summary>
    /// Lấy time scale mặc định
    /// </summary>
    public float GetNormalTimeScale() => normalTimeScale;

    private void StopTimeScaleCoroutine()
    {
        if (timeScaleCoroutine != null)
        {
            StopCoroutine(timeScaleCoroutine);
            timeScaleCoroutine = null;
        }
    }

    private IEnumerator ResumeTimeAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = normalTimeScale;
        timeScaleCoroutine = null;
    }

    private IEnumerator SlowMotionCoroutine(float slowFactor, float duration)
    {
        Time.timeScale = Mathf.Clamp(slowFactor, 0f, 1f);
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = normalTimeScale;
        timeScaleCoroutine = null;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
