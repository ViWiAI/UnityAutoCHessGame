using Cinemachine; // ���� Cinemachine �����ռ�
using Game.Animation;
using Game.Core;
using Game.Network;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Data;

namespace Game.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject playerMoveCamera; // ���� CinemachineVirtualCamera �� GameObject
        private PlayerHero playerHero;
        private int currentMapId;
        private bool isLogin;
        private bool isOnline;
        private string loginAccount;
        private float spriteHeightOffset = -0.2f;

        public void setMapId(int mapId)
        {
            currentMapId = mapId;
        }
        public bool GetOnline()
        {
            return isOnline;
        }
        public void SetOnlineGame(bool status)
        {
            isOnline = status;
        }
        public string GetLoginAccount()
        {
            return loginAccount;
        }
        public void SetLoginAccount(string loginAccount)
        {
            this.loginAccount = loginAccount;
        }

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
            isLogin = false;
            isOnline = false;
           
          //  InitializeLocalPlayer(1, HeroRole.Warrior, new Vector3Int(0, 0, 0));
        }

        private void InitializeLocalPlayer(int playerId, HeroRole job, Vector3Int initialCellPos)
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
            worldPos += new Vector3(0, spriteHeightOffset, 0);
            GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"LocalPlayer_{playerId}";
            playerHero = playerObj.GetComponent<PlayerHero>();
            if (playerHero == null)
            {
                Debug.LogError("playerPrefab ȱ�� PlayerHero ���");
                Destroy(playerObj);
                return;
            }

            playerHero.Initialize(playerId, true, job);
            playerHero.SetCurrentMapId(currentMapId);

            PlayerManager.Instance.SetLocalPLayer(playerHero);

            // ���� Cinemachine �� Follow Ŀ��
            PlayerCameraFollow.Instance.SetPlayerTarget(playerHero.transform);
        //    cinemachineCamera.Follow = playerHero.transform; // ֱ�Ӹ��� playerObj �� Transform
            Debug.Log($"Cinemachine ���ø���Ŀ��: {playerObj.name}, λ��: {worldPos}, tilemap={MapManager.Instance.GetTilemap()?.name}");

            // ֪ͨ�������������
            if (WebSocketManager.Instance.IsConnected)
            {
                NetworkMessageHandler.Instance.SendPlayerOnlineRequest(playerId, currentMapId, job, initialCellPos);
                Debug.Log($"��� {playerId} ����������Ϣ����ͼ: {currentMapId}, λ��: {initialCellPos}, ְҵ: {job}");
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

        public void EnterBattle(int battleMapId, int battleRoomId)
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