using Game.Animation;
using Game.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Managers
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }
        [SerializeField] private GameObject playerPrefab;
        private Dictionary<string, PlayerHero> otherPlayers = new Dictionary<string, PlayerHero>();

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
        }

        private void Start()
        {
            
        }

        public void AddPlayer(string playerId, Vector3Int cellPos, string mapId, HeroJobs job = HeroJobs.Warrior)
        {
            if (otherPlayers.ContainsKey(playerId))
            {
                Debug.Log($"玩家 {playerId} 已存在");
                return;
            }

            if (playerPrefab == null)
            {
                Debug.LogError("playerPrefab 未设置");
                return;
            }

            Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(cellPos);
            GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"Player_{playerId}";
            PlayerHero player = playerObj.GetComponent<PlayerHero>();
            if (player == null)
            {
                Debug.LogError($"playerPrefab 缺少 PlayerHero 组件");
                Destroy(playerObj);
                return;
            }

            player.Initialize(playerId, false, job); // 远程玩家，isLocalPlayer = false
            player.SetCurrentMapId(mapId);
            player.MoveTo(cellPos);
            otherPlayers.Add(playerId, player);
            Debug.Log($"成功添加远程玩家 {playerId} 在位置 {cellPos}，地图 {mapId}, job: {job}");
        }

        public void UpdatePlayerPosition(string playerId, Vector3Int cellPos, string mapId, HeroJobs job = HeroJobs.Warrior)
        {
            if (otherPlayers.ContainsKey(playerId))
            {
                var player = otherPlayers[playerId];
                if (player.GetCurrentMapId() != mapId)
                {
                    player.SetCurrentMapId(mapId);
                }
                player.MoveTo(cellPos);

                if (player.GetJob() != job)
                {
                    player.SetJob(job); // 更新职业
                }
            }
            else
            {
                AddPlayer(playerId, cellPos, mapId, job);
            }
        }

        public void RemovePlayer(string playerId)
        {
            if (otherPlayers.ContainsKey(playerId))
            {
                Destroy(otherPlayers[playerId].gameObject);
                otherPlayers.Remove(playerId);
                Debug.Log($"移除远程玩家 {playerId}");
            }
        }
    }
}