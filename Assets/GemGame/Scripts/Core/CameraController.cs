using UnityEngine;
using Cinemachine;
using Game.Managers;

public class CameraZoomController : MonoBehaviour
{
    [SerializeField] private float zoomSpeed = 100f; // 缩放速度
    [SerializeField] private float minOrthoSize = 4f; // 最小视野大小（放大）
    [SerializeField] private float maxOrthoSize = 6f; // 最大视野大小（缩小）

    [Header("Camera Shake Settings")]
    [SerializeField] private Transform shakeCenter; // 晃动的中心点（可以是 Tilemap 中心）
    [SerializeField] private float shakeAmplitude = 0.5f; // 晃动幅度（单位：Unity 单位）
    [SerializeField] private float shakeFrequency = 1f; // 晃动频率（每秒周期数）
    [SerializeField] private Vector2 shakeDirection = new Vector2(1f, 1f); // 晃动方向（X/Y 轴）

    private CinemachineVirtualCamera virtualCamera;
    private Vector3 originalFollowOffset; // 原始 Follow Offset，用于恢复
    private float shakeTimer; // 计时器，用于计算晃动

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("CinemachineVirtualCamera 组件未找到！");
        }

        // 保存原始 Follow Offset（如果使用 Framing Transposer 或 Transposer）
        var transposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (transposer != null)
        {
            originalFollowOffset = transposer.m_TrackedObjectOffset;
        }
        else
        {
            var basicTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (basicTransposer != null)
            {
                originalFollowOffset = basicTransposer.m_FollowOffset;
            }
        }

        // 如果未指定 shakeCenter，尝试使用 Follow 目标或默认场景原点
        if (shakeCenter == null && virtualCamera.Follow != null)
        {
            shakeCenter = virtualCamera.Follow;
        }
        else if (shakeCenter == null)
        {
            Debug.LogWarning("未设置 shakeCenter，将使用场景原点 (0, 0, 0)");
            GameObject center = new GameObject("ShakeCenter");
            shakeCenter = center.transform;
            shakeCenter.position = Vector3.zero;
        }
    }

    private void Update()
    {
        if (GameManager.Instance.GetLoginStatus())
        {
            // 玩家已登录：处理缩放
            HandleZoom();
        }
        else
        {
            // 玩家未登录：处理相机晃动
            HandleCameraShake();
        }
    }

    private void HandleZoom()
    {
        // 获取鼠标滚轮输入
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            // 计算新的 Orthographic Size
            float currentOrthoSize = virtualCamera.m_Lens.OrthographicSize;
            float newOrthoSize = currentOrthoSize - scrollInput * zoomSpeed * Time.deltaTime;

            // 限制缩放范围
            newOrthoSize = Mathf.Clamp(newOrthoSize, minOrthoSize, maxOrthoSize);

            // 应用新的 Orthographic Size
            virtualCamera.m_Lens.OrthographicSize = newOrthoSize;

            Debug.Log($"相机缩放: OrthographicSize = {newOrthoSize}");
        }

        // 停止晃动，恢复原始 Follow Offset
        ResetCameraOffset();
    }

    private void HandleCameraShake()
    {
        // 增加计时器
        shakeTimer += Time.deltaTime * shakeFrequency;

        // 使用正弦函数计算晃动偏移
        float offsetX = Mathf.Sin(shakeTimer) * shakeAmplitude * shakeDirection.x;
        float offsetY = Mathf.Cos(shakeTimer) * shakeAmplitude * shakeDirection.y;

        // 应用偏移到 Follow Offset
        var transposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (transposer != null)
        {
            transposer.m_TrackedObjectOffset = originalFollowOffset + new Vector3(offsetX, offsetY, 0f);
        }
        else
        {
            var basicTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (basicTransposer != null)
            {
                basicTransposer.m_FollowOffset = originalFollowOffset + new Vector3(offsetX, offsetY, 0f);
            }
        }

        // 确保相机始终看向 shakeCenter
        virtualCamera.Follow = shakeCenter;
    }

    private void ResetCameraOffset()
    {
        // 恢复原始 Follow Offset
        var transposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (transposer != null)
        {
            transposer.m_TrackedObjectOffset = originalFollowOffset;
        }
        else
        {
            var basicTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (basicTransposer != null)
            {
                basicTransposer.m_FollowOffset = originalFollowOffset;
            }
        }
    }

    // 可选：公开方法以动态设置晃动中心
    public void SetShakeCenter(Transform newCenter)
    {
        shakeCenter = newCenter;
    }
}