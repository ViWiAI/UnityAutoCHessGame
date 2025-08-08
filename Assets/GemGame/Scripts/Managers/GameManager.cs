using Cinemachine; // ���� Cinemachine �����ռ�
using Game.Animation;
using Game.Core;
using Game.Network;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject playerMoveCamera; // ���� CinemachineVirtualCamera �� GameObject
        private PlayerHero playerHero;
        private string currentMapId;
        private bool isLogin;

        public bool GetLoginStatus()
        {
            return isLogin;
        }

        public void SetLoginStatus(bool status)
        {
            isLogin = status;
        }

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

        private void Start()
        {
            if (string.IsNullOrEmpty(currentMapId))
            {
                currentMapId = SceneManager.GetActiveScene().name;
            }
            InitializeLocalPlayer(GridUtility.GenerateRandomPlayerId(), HeroJobs.Warrior, new Vector3Int(-1, 0, 0));
        }

        private void InitializeLocalPlayer(string playerId, HeroJobs job, Vector3Int initialCellPos)
        {
            if (playerHero != null)
            {
                Debug.LogWarning($"��������Ѵ���: {playerId}");
                return;
            }

            if (playerPrefab == null)
            {
                Debug.LogError("playerPrefab δ����");
                return;
            }

            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogError($"MapManager �� Tilemap δ��ʼ��������: {currentMapId}");
                return;
            }

            if (playerMoveCamera == null)
            {
                Debug.LogError("playerMoveCamera δ����");
                return;
            }

            // ��ȡ CinemachineVirtualCamera ���
            CinemachineVirtualCamera cinemachineCamera = playerMoveCamera.GetComponent<CinemachineVirtualCamera>();
            if (cinemachineCamera == null)
            {
                Debug.LogError("playerMoveCamera ȱ�� CinemachineVirtualCamera ���");
                return;
            }

            //Vector3Int initialCellPos = new Vector3Int(-1,0, 0);
            Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(initialCellPos);
            GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"{playerId}";
            playerHero = playerObj.GetComponent<PlayerHero>();
            if (playerHero == null)
            {
                Debug.LogError("playerPrefab ȱ�� PlayerHero ���");
                Destroy(playerObj);
                return;
            }

            playerHero.Initialize(playerId, true, job);
            playerHero.SetCurrentMapId(currentMapId);
            InputManager.Instance.SetPlayer(playerHero);

            // ���� Cinemachine �� Follow Ŀ��
            cinemachineCamera.Follow = playerObj.transform; // ֱ�Ӹ��� playerObj �� Transform
            Debug.Log($"Cinemachine ���ø���Ŀ��: {playerObj.name}, λ��: {worldPos}, tilemap={MapManager.Instance.GetTilemap()?.name}");

            // ֪ͨ�������������
            if (WebSocketManager.Instance.IsConnected())
            {
                WebSocketManager.Instance.Send(new Dictionary<string, object>
                {
                    { "type", "player_online" },
                    { "player_id", playerId },
                    { "map_id", currentMapId },
                    { "job", job.ToString() },
                    { "position", new Dictionary<string, int>
                        {
                            { "x", initialCellPos.x },
                            { "y", initialCellPos.y },
                            { "z", initialCellPos.z }
                        }
                    }
                });
                Debug.Log($"�������������Ϣ: {playerId}, ��ͼ: {currentMapId}, λ��: {initialCellPos}, ְҵ: {job}");
            }
            else
            {
                Debug.LogWarning($"WebSocket δ���ӣ���� {playerId} ������Ϣ���Ŷӷ���");
            }

            Debug.Log($"������ҳ�ʼ��: {playerId}, job: {job}, position: {worldPos}, tilemap={MapManager.Instance.GetTilemap()?.name}");
        }

        public PlayerHero GetLocalPlayer()
        {
            return playerHero;
        }

        public void EnterBattle(string battleMapId, string battleRoomId)
        {
            if (MapManager.Instance == null)
            {
                Debug.LogError($"MapManager ����δ��ʼ�����޷��л���ս����ͼ: {battleMapId}");
                return;
            }
            MapManager.Instance.SwitchMap(battleMapId, battleRoomId);
            currentMapId = battleMapId;
            if (playerHero != null)
            {
                playerHero.SetCurrentMapId(battleMapId);
                // ���� Cinemachine �� Follow Ŀ�꣨�����Ҫ��
                CinemachineVirtualCamera cinemachineCamera = playerMoveCamera?.GetComponent<CinemachineVirtualCamera>();
                if (cinemachineCamera != null)
                {
                    cinemachineCamera.Follow = playerHero.transform;
                    Debug.Log($"Cinemachine ���¸���Ŀ�굽ս����ͼ: {battleMapId}");
                }
            }
            Debug.Log($"GameManager: ����ս����ͼ {battleMapId}, ����: {battleRoomId}");
        }
    }
}