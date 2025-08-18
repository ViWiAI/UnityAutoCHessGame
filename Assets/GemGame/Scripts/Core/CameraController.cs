using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CameraZoomController : MonoBehaviour
{
    public static CameraZoomController Instance { get; private set; }

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 100f;
    [SerializeField] private float minOrthoSize = 4f;
    [SerializeField] private float maxOrthoSize = 6f;

    [Header("Login Screen Shake")]
    [SerializeField] private float shakeAmplitude = 0.5f;
    [SerializeField] private float shakeFrequency = 1f;
    [SerializeField] private Vector2 shakeDirection = new Vector2(1f, 1f);

    private CinemachineVirtualCamera virtualCamera;
    private Transform defaultShakeTarget;
    private CinemachineTransposer transposer;
    private Vector3 originalFollowOffset;
    public bool isInGameScene = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCamera();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void InitializeCamera()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("CinemachineVirtualCamera component not found!");
            return;
        }

        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            originalFollowOffset = transposer.m_FollowOffset;
        }

        // 创建默认的登录界面抖动目标
        //defaultShakeTarget = new GameObject("CameraShakeTarget").transform;
        //defaultShakeTarget.position = Vector3.zero;

        //virtualCamera.Follow = defaultShakeTarget;
        Debug.Log("Camera initialized for login screen");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Detect if we're entering the game scene
        if (scene.name == "NoviceMap") // Adjust to your game scene name
        {
            isInGameScene = true;
            Debug.Log("Entered game scene, ready to follow player");
        }
        else
        {
            isInGameScene = false;
            ResetToLoginMode();
        }
    }

    private void Update()
    {
        if (!isInGameScene)
        {
            HandleLoginShake();
        }
        HandleZoom();
    }

    public void SetFollowTarget(Transform target)
    {
        if (virtualCamera == null)
        {
            Debug.LogError("Virtual camera is not initialized!");
            return;
        }

        if (!isInGameScene)
        {
            Debug.LogWarning("Not in game scene, cannot set follow target");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("Attempted to set null follow target");
            return;
        }

        virtualCamera.Follow = target;

        // 确保transposer存在
        if (transposer == null)
        {
            transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer == null)
            {
                Debug.LogError("Failed to get CinemachineTransposer component!");
                return;
            }
        }

        transposer.m_FollowOffset = new Vector3(0, 0, -10);
        transposer.m_XDamping = 0.5f;
        transposer.m_YDamping = 0.5f;
        transposer.m_ZDamping = 0f;

        Debug.Log($"Camera now following: {target.name} at position {target.position}", target);
        Debug.Log($"Camera status: {GetCameraStatus()}");
    }

    public void ResetToLoginMode()
    {
        isInGameScene = false;
        virtualCamera.Follow = defaultShakeTarget;

        if (transposer != null)
        {
            transposer.m_FollowOffset = originalFollowOffset;
        }

        Debug.Log("Camera reset to login screen mode");
    }

    private void HandleLoginShake()
    {
        if (defaultShakeTarget == null) return;

        float offsetX = Mathf.Sin(Time.time * shakeFrequency) * shakeAmplitude * shakeDirection.x;
        float offsetY = Mathf.Cos(Time.time * shakeFrequency) * shakeAmplitude * shakeDirection.y;

        defaultShakeTarget.localPosition = new Vector3(offsetX, offsetY, 0);
    }

    private void HandleZoom()
    {
        if (virtualCamera == null) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            float newSize = virtualCamera.m_Lens.OrthographicSize - scrollInput * zoomSpeed * Time.deltaTime;
            virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);
        }
    }

    // Debug method to check camera status
    public string GetCameraStatus()
    {
        return $"Camera Status:\n" +
               $"Following: {(virtualCamera.Follow != null ? virtualCamera.Follow.name : "null")}\n" +
               $"In Game Scene: {isInGameScene}\n" +
               $"Ortho Size: {virtualCamera.m_Lens.OrthographicSize}";
    }
}