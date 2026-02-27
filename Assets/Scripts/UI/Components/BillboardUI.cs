using UnityEngine;

/// <summary>
/// Làm cho UI world-space luôn hướng về camera (billboard effect).
/// Gắn vào Canvas hoặc GameObject chứa UI cần billboard.
/// </summary>
public class BillboardUI : MonoBehaviour
{
    public enum BillboardMode
    {
        /// <summary>Xoay hoàn toàn theo camera (cả 3 trục).</summary>
        Full,
        /// <summary>Chỉ xoay trục Y, giữ UI đứng thẳng.</summary>
        YAxisOnly
    }

    [SerializeField] private BillboardMode mode = BillboardMode.Full;

    private Transform _cameraTransform;

    void Start()
    {
        _cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (_cameraTransform == null) return;

        switch (mode)
        {
            case BillboardMode.Full:
                transform.rotation = _cameraTransform.rotation;
                break;

            case BillboardMode.YAxisOnly:
                Vector3 forward = _cameraTransform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(forward);
                break;
        }
    }
}
