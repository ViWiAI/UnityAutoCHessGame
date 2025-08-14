using Cinemachine; // 引入 Cinemachine 命名空间
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
        [SerializeField] private GameObject playerMoveCamera; // 挂载 CinemachineVirtualCamera 的 GameObject
        private PlayerHero playerHero;
        private string currentMapId;
        private bool isLogin;
        private float spriteHeightOffset = -0.2f;

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
            InitializeLocalPlayer(GridUtility.GenerateRandomPlayerId(), HeroRole.Warrior, new Vector3Int(0, 0, 0));
        }

        private void InitializeLocalPlayer(string playerId, HeroRole job, Vector3Int initialCellPos)
        {
            if (playerHero != null)
            {
                Debug.LogWarning($"本地玩家已存在: {playerId}");
                return;
            }

            if (playerPrefab == null)
            {
                Debug.LogError("playerPrefab 未设置");
                return;
            }

            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogError($"MapManager 或 Tilemap 未初始化，场景: {currentMapId}");
                return;
            }

            if (playerMoveCamera == null)
            {
                Debug.LogError("playerMoveCamera 未设置");
                return;
            }

            // 获取 CinemachineVirtualCamera 组件
            CinemachineVirtualCamera cinemachineCamera = playerMoveCamera.GetComponent<CinemachineVirtualCamera>();
            if (cinemachineCamera == null)
            {
                Debug.LogError("playerMoveCamera 缺少 CinemachineVirtualCamera 组件");
                return;
            }

            //Vector3Int initialCellPos = new Vector3Int(-1,0, 0);
            Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(initialCellPos);
            worldPos += new Vector3(0, spriteHeightOffset, 0);
            GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"{playerId}";
            playerHero = playerObj.GetComponent<PlayerHero>();
            if (playerHero == null)
            {
                Debug.LogError("playerPrefab 缺少 PlayerHero 组件");
                Destroy(playerObj);
                return;
            }

            playerHero.Initialize(playerId, true, job);
            playerHero.SetCurrentMapId(currentMapId);
            InputManager.Instance.SetPlayer(playerHero);

            // 设置 Cinemachine 的 Follow 目标
            cinemachineCamera.Follow = playerObj.transform; // 直接跟随 playerObj 的 Transform
            Debug.Log($"Cinemachine 设置跟随目标: {playerObj.name}, 位置: {worldPos}, tilemap={MapManager.Instance.GetTilemap()?.name}");

            // 通知服务器玩家上线
            if (WebSocketManager.Instance.IsConnected())
            {
                NetworkMessageHandler.Instance.SendPlayerOnlineRequest(playerId, currentMapId, job, initialCellPos);
                Debug.Log($"玩家 {playerId} 发送上线消息，地图: {currentMapId}, 位置: {initialCellPos}, 职业: {job}");
            }
            else
            {
                Debug.LogWarning($"WebSocket 未连接，玩家 {playerId} 上线消息将排队发送");
            }

            Debug.Log($"本地玩家初始化: {playerId}, job: {job}, position: {worldPos}, tilemap={MapManager.Instance.GetTilemap()?.name}");
        }

        public PlayerHero GetLocalPlayer()
        {
            return playerHero;
        }

        public void EnterBattle(string battleMapId, string battleRoomId)
        {
            if (MapManager.Instance == null)
            {
                Debug.LogError($"MapManager 单例未初始化，无法切换到战斗地图: {battleMapId}");
                return;
            }
            MapManager.Instance.SwitchMap(battleMapId, battleRoomId);
            currentMapId = battleMapId;
            if (playerHero != null)
            {
                playerHero.SetCurrentMapId(battleMapId);
                // 更新 Cinemachine 的 Follow 目标（如果需要）
                CinemachineVirtualCamera cinemachineCamera = playerMoveCamera?.GetComponent<CinemachineVirtualCamera>();
                if (cinemachineCamera != null)
                {
                    cinemachineCamera.Follow = playerHero.transform;
                    Debug.Log($"Cinemachine 更新跟随目标到战斗地图: {battleMapId}");
                }
            }
            Debug.Log($"GameManager: 进入战斗地图 {battleMapId}, 房间: {battleRoomId}");
        }
    }
}