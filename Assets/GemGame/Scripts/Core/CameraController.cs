using UnityEngine;
using Cinemachine;
using Game.Managers;

public class CameraZoomController : MonoBehaviour
{
    [SerializeField] private float zoomSpeed = 100f; // �����ٶ�
    [SerializeField] private float minOrthoSize = 4f; // ��С��Ұ��С���Ŵ�
    [SerializeField] private float maxOrthoSize = 6f; // �����Ұ��С����С��

    [Header("Camera Shake Settings")]
    [SerializeField] private Transform shakeCenter; // �ζ������ĵ㣨������ Tilemap ���ģ�
    [SerializeField] private float shakeAmplitude = 0.5f; // �ζ����ȣ���λ��Unity ��λ��
    [SerializeField] private float shakeFrequency = 1f; // �ζ�Ƶ�ʣ�ÿ����������
    [SerializeField] private Vector2 shakeDirection = new Vector2(1f, 1f); // �ζ�����X/Y �ᣩ

    private CinemachineVirtualCamera virtualCamera;
    private Vector3 originalFollowOffset; // ԭʼ Follow Offset�����ڻָ�
    private float shakeTimer; // ��ʱ�������ڼ���ζ�

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("CinemachineVirtualCamera ���δ�ҵ���");
        }

        // ����ԭʼ Follow Offset�����ʹ�� Framing Transposer �� Transposer��
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

        // ���δָ�� shakeCenter������ʹ�� Follow Ŀ���Ĭ�ϳ���ԭ��
        if (shakeCenter == null && virtualCamera.Follow != null)
        {
            shakeCenter = virtualCamera.Follow;
        }
        else if (shakeCenter == null)
        {
            Debug.LogWarning("δ���� shakeCenter����ʹ�ó���ԭ�� (0, 0, 0)");
            GameObject center = new GameObject("ShakeCenter");
            shakeCenter = center.transform;
            shakeCenter.position = Vector3.zero;
        }
    }

    private void Update()
    {
        if (GameManager.Instance.GetLoginStatus())
        {
            // ����ѵ�¼����������
            HandleZoom();
        }
        else
        {
            // ���δ��¼����������ζ�
            HandleCameraShake();
        }
    }

    private void HandleZoom()
    {
        // ��ȡ����������
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            // �����µ� Orthographic Size
            float currentOrthoSize = virtualCamera.m_Lens.OrthographicSize;
            float newOrthoSize = currentOrthoSize - scrollInput * zoomSpeed * Time.deltaTime;

            // �������ŷ�Χ
            newOrthoSize = Mathf.Clamp(newOrthoSize, minOrthoSize, maxOrthoSize);

            // Ӧ���µ� Orthographic Size
            virtualCamera.m_Lens.OrthographicSize = newOrthoSize;

            Debug.Log($"�������: OrthographicSize = {newOrthoSize}");
        }

        // ֹͣ�ζ����ָ�ԭʼ Follow Offset
        ResetCameraOffset();
    }

    private void HandleCameraShake()
    {
        // ���Ӽ�ʱ��
        shakeTimer += Time.deltaTime * shakeFrequency;

        // ʹ�����Һ�������ζ�ƫ��
        float offsetX = Mathf.Sin(shakeTimer) * shakeAmplitude * shakeDirection.x;
        float offsetY = Mathf.Cos(shakeTimer) * shakeAmplitude * shakeDirection.y;

        // Ӧ��ƫ�Ƶ� Follow Offset
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

        // ȷ�����ʼ�տ��� shakeCenter
        virtualCamera.Follow = shakeCenter;
    }

    private void ResetCameraOffset()
    {
        // �ָ�ԭʼ Follow Offset
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

    // ��ѡ�����������Զ�̬���ûζ�����
    public void SetShakeCenter(Transform newCenter)
    {
        shakeCenter = newCenter;
    }
}