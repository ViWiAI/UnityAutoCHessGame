using Game.Animation;
using Game.Core;
using Game.Data;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Managers
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        private Dictionary<int, PlayerHero> otherPlayers = new Dictionary<int, PlayerHero>();

        private PlayerHero localPlayer; // 本地玩家

        // 服务器 Role (int) 到 HeroRole 的映射
        private static readonly Dictionary<int, HeroRole> RoleMapping = new Dictionary<int, HeroRole>
        {
            { 1, HeroRole.Warrior }, // 服务器 Role 1 对应 Warrior
            { 2, HeroRole.Mage },    // 服务器 Role 2 对应 Mage
            { 3, HeroRole.Hunter },  // 服务器 Role 3 对应 Hunter
            { 4, HeroRole.Rogue },   // 服务器 Role 4 对应 Rogue
            { 5, HeroRole.Priest }   // 服务器 Role 5 对应 Priest
        };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("PlayerManager 初始化成功");
            }
            else
            {
                Debug.LogWarning("销毁重复的 PlayerManager");
                Destroy(gameObject);
            }

            // 注册场景加载事件
          //  SceneManager.sceneLoaded += OnSceneLoaded;
        }

        //private void OnDestroy()
        //{
        //    SceneManager.sceneLoaded -= OnSceneLoaded;
        //}

        private void Start()
        {
            
        }

        // 将服务器的 Role (int) 转换为 HeroRole
        private HeroRole ConvertRole(int serverRole)
        {
            if (RoleMapping.TryGetValue(serverRole, out HeroRole role))
            {
                return role;
            }
            Debug.LogWarning($"未定义的服务器 Role 值: {serverRole}，默认使用 Warrior");
            return HeroRole.Warrior; // 默认值
        }

        // 场景加载完成时调用
        //private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        //{
        //    Debug.Log($"PlayerManager: 场景 {scene.name} OnSceneLoaded");
        //    if (localPlayer == null && GameManager.Instance.GetOnline())
        //    {
        //        InitializeLocalPlayer(CharacterManager.Instance.selectPLayerCharacter); // 初始化本地玩家
        //    }
        //    Debug.Log($"PlayerManager: 场景 {scene.name} 加载完成");
        //}

        // 初始化本地玩家
        public void InitializeLocalPlayer(PlayerCharacterInfo playerCharacter)
        {
            if (localPlayer != null)
            {
                Debug.LogWarning($"本地玩家 {localPlayer.GetPlayerId()} 已存在");
                return;
            }

            // 确保MapManager已初始化
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogError("MapManager 或 Tilemap 未初始化");
                return;
            }

            // 创建玩家对象
            int playerId = playerCharacter.CharacterId;
            Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(new Vector3Int(playerCharacter.X, playerCharacter.Y, 0));
            HeroRole role = ConvertRole(playerCharacter.Role);
            int mapId = playerCharacter.MapId;

            GameObject playerPrefab = CharacterManager.Instance.InitPlayerObj(role, playerCharacter.SkinId);
            if (playerPrefab == null)
            {
                Debug.LogError("无法获取玩家预制体");
                return;
            }

            GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"LocalPlayer_{playerId}";

            localPlayer = playerObj.GetComponent<PlayerHero>();
            if (localPlayer == null)
            {
                Debug.LogError("玩家对象缺少PlayerHero组件");
                Destroy(playerObj);
                return;
            }

            localPlayer.Initialize(playerId, true, role);
            localPlayer.SetCurrentMapId(mapId);

            // 确保PlayerCameraFollow已初始化
            if (PlayerCameraFollow.Instance == null)
            {
                Debug.LogError("PlayerCameraFollow未初始化");
                return;
            }

            // 设置相机跟随 - 添加延迟确保transform已更新
            StartCoroutine(InitializePlayer());
            Debug.Log($"成功初始化本地玩家 {playerId}");
        }

        private IEnumerator InitializePlayer()
        {

            yield return null; // 等待一帧确保场景加载完成

            if (PlayerCameraFollow.Instance != null)
            {
                PlayerCameraFollow.Instance.SetPlayerTarget(localPlayer.transform);
            }
            else
            {
                Debug.LogError("Camera controller not found");
            }
        }

        public void AddPlayer(int playerId, Vector3Int cellPos, int mapId, HeroRole job = HeroRole.Warrior)
        {
            if (otherPlayers.ContainsKey(playerId))
            {
                Debug.Log($"玩家 {playerId} 已存在");
                return;
            }

            GameObject playerObj = CharacterManager.Instance.InitPlayerObj(HeroRole.Hunter, 1);
            Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(cellPos);
            playerObj.name = $"Player_{playerId}";
            PlayerHero player = playerObj.GetComponent<PlayerHero>();
            if (player == null)
            {
                Debug.LogError($"playerPrefab 缺少 PlayerHero 组件");
                Destroy(playerObj);
                return;
            }

            player.Initialize(playerId, false, job); // 远程玩家
            player.SetCurrentMapId(mapId);
            player.MoveTo(cellPos);
            otherPlayers.Add(playerId, player);
            Debug.Log($"成功添加远程玩家 {playerId} 在位置 {cellPos}, 地图 {mapId}, 职业: {job}");
        }

        public void UpdatePlayerPosition(int playerId, Vector3Int cellPos, int mapId, HeroRole job = HeroRole.Warrior)
        {
            if (localPlayer != null && localPlayer.GetPlayerId() == playerId)
            {
                if (localPlayer.GetCurrentMapId() != mapId)
                {
                    localPlayer.SetCurrentMapId(mapId);
                }
                localPlayer.MoveTo(cellPos);
                if (localPlayer.GetJob() != job)
                {
                    localPlayer.SetJob(job);
                }
            }
            else if (otherPlayers.ContainsKey(playerId))
            {
                var player = otherPlayers[playerId];
                if (player.GetCurrentMapId() != mapId)
                {
                    player.SetCurrentMapId(mapId);
                }
                player.MoveTo(cellPos);
                if (player.GetJob() != job)
                {
                    player.SetJob(job);
                }
            }
            else
            {
                AddPlayer(playerId, cellPos, mapId, job);
            }
        }

        public PlayerHero GetLocalPlayer()
        {
            return localPlayer;
        }

        public void SetLocalPLayer(PlayerHero hero)
        {
            localPlayer = hero;
        }

        public void RemovePlayer(int playerId)
        {
            if (localPlayer != null && localPlayer.GetPlayerId() == playerId)
            {
                Destroy(localPlayer.gameObject);
                localPlayer = null;
                Debug.Log($"移除本地玩家 {playerId}");
            }
            else if (otherPlayers.ContainsKey(playerId))
            {
                Destroy(otherPlayers[playerId].gameObject);
                otherPlayers.Remove(playerId);
                Debug.Log($"移除远程玩家 {playerId}");
            }
        }
    }
}