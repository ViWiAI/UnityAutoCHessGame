using Cinemachine;
using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    public static PlayerCameraFollow Instance { get; private set; }

    [SerializeField] private Transform playerTransform; // 玩家的 Transform（初始设置）
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // 相机相对于玩家的偏移
    [SerializeField] private float smoothTime = 0.3f; // 平滑跟随时间

    private CinemachineVirtualCamera virtualCamera;
    private CinemachineTransposer transposer;

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 获取虚拟相机组件
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("Cinemachine Virtual Camera 未附加到此 GameObject!");
            return;
        }

        // 获取 Cinemachine 的 Transposer 组件
        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer == null)
        {
            Debug.LogError("Cinemachine Virtual Camera 未包含 Transposer 组件!");
            return;
        }
    }

    private void Start()
    {
        // 设置玩家初始位置为 (0, 0, 0)
        if (playerTransform != null)
        {
            playerTransform.position = Vector3.zero;
        }

    }

    private void Update()
    {
        // 平滑调整偏移
        if (transposer != null && playerTransform != null)
        {
            transposer.m_FollowOffset = Vector3.Lerp(transposer.m_FollowOffset, offset, Time.deltaTime / smoothTime);
        }
    }

    // 动态设置跟随的玩家
    public void SetPlayerTarget(Transform newPlayerTransform)
    {
        if (newPlayerTransform == null)
        {
            Debug.LogWarning("尝试设置空的玩家 Transform!");
            return;
        }

        virtualCamera.Follow = newPlayerTransform;

    }
}