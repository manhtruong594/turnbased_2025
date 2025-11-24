using UnityEngine;

namespace RedBjorn.ProtoTiles.Example
{
    [DefaultExecutionOrder(1000)]
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        float Eps = 0.1f;

        Vector3? HoldPosition;
        Vector3? ClickPosition;

        public static bool IsMovingByPlayer;

        MapEntity CachedMap;

        void OnEnable()
        {
            CachedMap = MapManager.Instance.MapEntity;
        }
        
        void LateUpdate()
        {
            if (MyInput.GetOnWorldDownFree(CachedMap.Settings.Plane()))
            {
                HoldPosition = MyInput.GroundPositionCameraOffset(CachedMap.Settings.Plane());
                ClickPosition = transform.position;
            }
            else if (MyInput.GetOnWorldUpFree(CachedMap.Settings.Plane()))
            {
                HoldPosition = null;
                ClickPosition = null;
            }
            UpdatePosition();
        }

        void OnDisable()
        {
            IsMovingByPlayer = false;
        }

        void UpdatePosition()
        {
            if (HoldPosition.HasValue)
            {
                var delta = HoldPosition.Value - MyInput.GroundPositionCameraOffset(CachedMap.Settings.Plane());
                transform.position += delta;
                transform.position = ClickPosition.Value + delta;
                if (!IsMovingByPlayer)
                {
                    IsMovingByPlayer = delta.sqrMagnitude > Eps;
                }
            }
            else
            {
                IsMovingByPlayer = false;
            }
        }
    }
}
