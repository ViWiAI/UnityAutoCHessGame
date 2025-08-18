using Cinemachine;
using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    public static PlayerCameraFollow Instance { get; private set; }

    [SerializeField] private Transform playerTransform; // ��ҵ� Transform����ʼ���ã�
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // ����������ҵ�ƫ��
    [SerializeField] private float smoothTime = 0.3f; // ƽ������ʱ��

    private CinemachineVirtualCamera virtualCamera;
    private CinemachineTransposer transposer;

    private void Awake()
    {
        // ����ģʽ
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // ��ȡ����������
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("Cinemachine Virtual Camera δ���ӵ��� GameObject!");
            return;
        }

        // ��ȡ Cinemachine �� Transposer ���
        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer == null)
        {
            Debug.LogError("Cinemachine Virtual Camera δ���� Transposer ���!");
            return;
        }
    }

    private void Start()
    {
        // ������ҳ�ʼλ��Ϊ (0, 0, 0)
        if (playerTransform != null)
        {
            playerTransform.position = Vector3.zero;
        }

    }

    private void Update()
    {
        // ƽ������ƫ��
        if (transposer != null && playerTransform != null)
        {
            transposer.m_FollowOffset = Vector3.Lerp(transposer.m_FollowOffset, offset, Time.deltaTime / smoothTime);
        }
    }

    // ��̬���ø�������
    public void SetPlayerTarget(Transform newPlayerTransform)
    {
        if (newPlayerTransform == null)
        {
            Debug.LogWarning("�������ÿյ���� Transform!");
            return;
        }

        virtualCamera.Follow = newPlayerTransform;

    }
}