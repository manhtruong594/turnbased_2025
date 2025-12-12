using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private MeshRenderer healthBarRenderer = null;
    private MaterialPropertyBlock matBlock = null;

    private float newHealthPercentage;
    private float _lastDamageTime = 0f;
    private float _previousTempFill = 1f; // Theo dõi temp fill trước đó
    private float _shrinkDelay;
    private float _shrinkSpeed;

    void Awake()
    {
        matBlock = new MaterialPropertyBlock();
        healthBarRenderer.GetPropertyBlock(matBlock);
    }

    void OnEnable()
    {
        //## Cache shader settings
        CacheShaderSettings();

        void CacheShaderSettings()
        {
            try
            {
                Material mat = healthBarRenderer.sharedMaterial;
                if (mat != null && mat.HasProperty(ShaderIDLib.ShrinkDelay) && mat.HasProperty(ShaderIDLib.ShrinkSpeed))
                {
                    _shrinkDelay = mat.GetFloat(ShaderIDLib.ShrinkDelay);
                    _shrinkSpeed = mat.GetFloat(ShaderIDLib.ShrinkSpeed);
                }
                else
                {
                    // Default values nếu không có material
                    _shrinkDelay = 0.1f;
                    _shrinkSpeed = 2.0f;
                }
            }
            catch
            {
                // Default values nếu có lỗi
                _shrinkDelay = 0.1f;
                _shrinkSpeed = 2.0f;
            }
        }
    }

    public void UpdateHealthBar(float healthPercent)
    {
        float currentTempFill = GetCurrentTempFillValue();

        newHealthPercentage = healthPercent;

        // Kiểm tra xem có nên reset damage time không
        bool shouldResetTime = false;

        // Nếu temp fill hiện tại đã gần bằng với fill trước đó, cho phép reset time
        float previousFill = matBlock.GetFloat(ShaderIDLib.Fill);
        if (Mathf.Abs(currentTempFill - previousFill) < 0.05f) // Threshold 5%
        {
            shouldResetTime = true;
        }

        // Hoặc nếu đây là damage đầu tiên
        if (_lastDamageTime == 0f)
        {
            shouldResetTime = true;
        }

        // Cập nhật các giá trị shader
        matBlock.SetFloat(ShaderIDLib.Fill, newHealthPercentage);
        matBlock.SetFloat(ShaderIDLib.TempFill, _previousTempFill); // Giữ nguyên temp fill trước đó

        // Chỉ reset damage time khi cần thiết
        if (shouldResetTime)
        {
            matBlock.SetFloat(ShaderIDLib.DamageTime, Time.timeSinceLevelLoad);
            _lastDamageTime = Time.timeSinceLevelLoad;
        }
        // Nếu không reset time, giữ nguyên damage time cũ để temp fill tiếp tục chạy

        matBlock.SetFloat(ShaderIDLib.LastTempFill, currentTempFill); // Lưu vị trí temp fill hiện tại
        healthBarRenderer.SetPropertyBlock(matBlock);

        // Cập nhật cho lần damage tiếp theo
        _previousTempFill = newHealthPercentage;
    }

    private float GetCurrentTempFillValue()
    {
        // Lấy giá trị hiện tại từ material property block
        healthBarRenderer.GetPropertyBlock(matBlock);

        float damageTime = matBlock.GetFloat(ShaderIDLib.DamageTime);
        float tempFill = matBlock.GetFloat(ShaderIDLib.TempFill);
        float fill = matBlock.GetFloat(ShaderIDLib.Fill);
        float lastTempFill = matBlock.GetFloat(ShaderIDLib.LastTempFill);

        // Sử dụng cached values thay vì lấy từ material mỗi lần
        float timeSinceDamage = Time.timeSinceLevelLoad - damageTime;

        if (lastTempFill > fill && lastTempFill > 0)
        {
            float startPoint = Mathf.Max(lastTempFill, fill);

            if (timeSinceDamage > _shrinkDelay)
            {
                float shrinkTime = timeSinceDamage - _shrinkDelay;
                float shrinkAmount = Mathf.Clamp01(shrinkTime * _shrinkSpeed);
                return Mathf.Lerp(startPoint, fill, shrinkAmount);
            }
            else
            {
                return startPoint;
            }
        }
        else
        {
            if (timeSinceDamage > _shrinkDelay)
            {
                float shrinkTime = timeSinceDamage - _shrinkDelay;
                float shrinkAmount = Mathf.Clamp01(shrinkTime * _shrinkSpeed);
                return Mathf.Lerp(tempFill, fill, shrinkAmount);
            }
            else
            {
                return tempFill;
            }
        }
    }
}
