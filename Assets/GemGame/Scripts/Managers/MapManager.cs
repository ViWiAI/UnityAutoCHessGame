using Game.Combat;
using Game.Core;
using Game.Network;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game.Managers
{
    public class MapManager : MonoBehaviour
    {
        // 单例实例
        public static MapManager Instance { get; private set; }

        public enum MapType
        {
            WorldMap,    // 世界地图
            DungeonMap,  // 副本战斗地图
            PVPMap,      // PVP地图
            SiegeMap     // 攻城地图
        }

        public enum MapStyle
        {
            Desert,      // 沙漠
            Grassland,   // 绿地
            Snow,        // 冰雪
            Lost,        // 失落
            Exotic,      // 异域
            Wasteland,   // 荒芜
            Swamp,       // 沼泽
            Volcano      // 火山
        }

        [SerializeField] protected Tilemap tilemap; // 主地图
        [SerializeField] protected Tilemap collisionTilemap; // 碰撞地图
        [SerializeField] private string mapId; // 地图 ID
        [SerializeField] private MapType mapType; // 地图类型
        [SerializeField] private MapStyle mapStyle; // 地图风格
        private string roomId; // 当前房间 ID（用于战斗、PVP等）
        private MapData mapData; // 地图数据

        // 事件：当怪物、宝物等刷新时触发
        public delegate void SpawnHandler(Vector3Int cell, string spawnType, string itemId);
        public event SpawnHandler OnSpawn;

        // 事件：当地图初始化或切换时触发
        public delegate void MapLoadedHandler(MapManager mapManager);
        public event MapLoadedHandler OnMapLoaded;

        private void Awake()
        {
            // 单例模式：确保只有一个 MapManager 实例
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"MapManager for mapId {mapId} already exists! Destroying this instance.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可选：根据需求决定是否跨场景保留

            // 设置默认 mapId
            if (string.IsNullOrEmpty(mapId))
            {
                mapId = SceneManager.GetActiveScene().name;
                Debug.LogWarning($"MapManager {name}: mapId 未设置，使用场景名称: {mapId}");
            }

            // 检查 Tilemap 配置
            if (tilemap == null)
            {
                Debug.LogError($"MapManager {name} (mapId: {mapId}): 未分配 Tilemap！请在 Inspector 中设置。");
                return;
            }
            if (collisionTilemap == null)
            {
                Debug.LogWarning($"MapManager {name} (mapId: {mapId}): 未分配 collisionTilemap，可能没有碰撞层。");
            }

            // 初始化 MapData
            mapData = new MapData
            {
                tilemap = tilemap,
                collisionTilemap = collisionTilemap,
                tileContents = new Dictionary<Vector3Int, TileContent>()
            };

            // 根据地图风格应用环境效果
            ApplyMapStyleEffects();

            Debug.Log($"MapManager initialized for mapId: {mapId}, Type: {mapType}, Style: {mapStyle}");
            OnMapLoaded?.Invoke(this);
        }

        private void OnDestroy()
        {
            // 移除 WebSocket 事件订阅
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.OnMessageReceived -= HandleServerMessage;
            }

            // 如果当前实例是单例，清除 Instance
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            // 订阅 WebSocket 事件
            WebSocketManager.Instance.OnMessageReceived += HandleServerMessage;

            // 更新所有 PlayerHero
         //   UpdatePlayerHeroes();
        }

        private void OnValidate()
        {
            // 在 Inspector 中编辑时检查配置
            if (tilemap == null)
            {
                Debug.LogWarning($"MapManager {name}: Tilemap 未分配，请在 Inspector 中设置。");
            }
            if (collisionTilemap == null)
            {
                Debug.LogWarning($"MapManager {name}: collisionTilemap 未分配，可能没有碰撞层。");
            }
            if (string.IsNullOrEmpty(mapId))
            {
                mapId = SceneManager.GetActiveScene().name;
                Debug.LogWarning($"MapManager {name}: mapId 未设置，自动使用场景名称: {mapId}");
            }
        }

        private void ApplyMapStyleEffects()
        {
            switch (mapStyle)
            {
                case MapStyle.Swamp:
                    foreach (var hero in FindObjectsOfType<PlayerHero>())
                    {
                        hero.stats.ModifyStat("movespeed", -0.5f);
                    }
                    Debug.Log($"地图 {mapId}: 应用沼泽效果，降低移动速度");
                    break;
                case MapStyle.Volcano:
                    StartCoroutine(ApplyVolcanoDamage());
                    Debug.Log($"地图 {mapId}: 应用火山效果，周期性伤害");
                    break;
            }
        }

        private IEnumerator ApplyVolcanoDamage()
        {
            while (true)
            {
                foreach (var hero in FindObjectsOfType<PlayerHero>())
                {
                    hero.TakeDamage(5f, false);
                }
                yield return new WaitForSeconds(1f);
            }
        }

        public void SwitchMap(string newMapId, string newRoomId = null)
        {
            if (newMapId == mapId)
            {
                roomId = newRoomId;
             //   UpdatePlayerHeroes();
                Debug.Log($"MapManager: 已在地图 {mapId}，更新 roomId: {newRoomId}");
                return;
            }

            SceneManager.LoadSceneAsync(newMapId, LoadSceneMode.Single).completed += (op) =>
            {
                Debug.Log($"MapManager: 切换到新地图: {newMapId}, 房间: {newRoomId}");
            };
        }

        //private void UpdatePlayerHeroes()
        //{
        //    var heroes = FindObjectsOfType<PlayerHero>();
        //    foreach (var hero in heroes)
        //    {
        //        hero.SetCurrentMapId(mapId);
        //        Debug.Log($"Updated PlayerHero {hero.gameObject.name} to mapId: {mapId}, Type: {mapType}, Style: {mapStyle}");
        //    }

        //    bool isBattleMap = mapType == MapType.DungeonMap || mapType == MapType.PVPMap || mapType == MapType.SiegeMap;
        //    // UIManager.Instance.ShowBattleUI(isBattleMap);
        //}

        public Tilemap GetTilemap() => mapData.tilemap;
        public Tilemap GetCollisionTilemap() => mapData.collisionTilemap;
        public string GetMapId() => mapId;
        public string GetRoomId() => roomId;
        public MapType GetMapType() => mapType;
        public MapStyle GetMapStyle() => mapStyle;

        private void HandleServerMessage(Dictionary<string, object> data)
        {
            if (data.ContainsKey("map_id") && data["map_id"].ToString() != mapId)
            {
                return;
            }

            string type = data["type"].ToString();
            switch (type)
            {
                case "spawn":
                    var cellData = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonConvert.SerializeObject(data["cell"]));
                    Vector3Int cell = new Vector3Int(cellData["x"], cellData["y"], cellData["z"]);
                    string spawnType = data["spawn_type"].ToString();
                    string itemId = data["item_id"].ToString();
                    if (!mapData.tileContents.ContainsKey(cell))
                    {
                        mapData.tileContents.Add(cell, new TileContent());
                    }
                    var content = mapData.tileContents[cell];
                    if (spawnType == "Monster")
                    {
                        if (mapType == MapType.DungeonMap || mapType == MapType.WorldMap)
                        {
                            content.hasMonster = true;
                            content.monsterId = itemId;
                        }
                    }
                    else if (spawnType == "Treasure")
                    {
                        content.hasTreasure = true;
                        content.treasureId = itemId;
                    }
                    else if (spawnType == "Castle")
                    {
                        if (mapType == MapType.SiegeMap)
                        {
                            content.hasCastle = true;
                            content.castleId = itemId;
                        }
                    }
                    Debug.Log($"地图 {mapId} 在 {cell} 生成了 {spawnType}: {itemId}");
                    OnSpawn?.Invoke(cell, spawnType, itemId);
                    break;
                case "interact_result":
                    //UIManager.Instance.ShowTreasurePrompt(data["result"].ToString());
                    break;
                case "battle_start":
                    string battleMapId = data["battle_map_id"].ToString();
                    string battleRoomId = data["battle_room_id"].ToString();
                    SwitchMap(battleMapId, battleRoomId);
                    break;
                case "pvp_match":
                    string pvpMapId = data["pvp_map_id"].ToString();
                    string pvpRoomId = data["pvp_room_id"].ToString();
                    SwitchMap(pvpMapId, pvpRoomId);
                    break;
                case "siege_start":
                    string siegeMapId = data["siege_map_id"].ToString();
                    string siegeRoomId = data["siege_room_id"].ToString();
                    SwitchMap(siegeMapId, siegeRoomId);
                    break;
                case "battle_countdown":
                    int countdown = int.Parse(data["countdown"].ToString());
                    //UIManager.Instance.ShowCountdown(countdown);
                    break;
            }
        }

        public string GetTreasureInfo(Vector3Int cell)
        {
            if (mapData.tileContents.TryGetValue(cell, out TileContent content))
            {
                if (content.hasTreasure && mapType != MapType.PVPMap)
                {
                    NotifyServerInteract(cell, "Treasure", content.treasureId);
                    return $"拾取宝物: {content.treasureId}";
                }
                if (content.hasMonster && mapType != MapType.PVPMap)
                {
                    NotifyServerInteract(cell, "Monster", content.monsterId);
                    return $"发现怪物: {content.monsterId}，触发战斗！";
                }
                if (content.hasCastle && mapType == MapType.SiegeMap)
                {
                    NotifyServerInteract(cell, "Castle", content.castleId);
                    return $"发起攻城: {content.castleId}！";
                }
                return "此格子为空，无宝物、怪物或城堡";
            }
            return $"地图 {mapId} 不存在内容！";
        }

        private void NotifyServerInteract(Vector3Int cell, string type, string itemId)
        {
            var heroes = FindObjectsOfType<PlayerHero>();
            foreach (var hero in heroes)
            {
                WebSocketManager.Instance.Send(new Dictionary<string, object>
                {
                    { "type", "interact" },
                    { "map_id", mapId },
                    { "cell", new { x = cell.x, y = cell.y, z = cell.z } },
                    { "interact_type", type },
                    { "item_id", itemId },
                    { "player_id", hero.GetPlayerId() },
                    { "team_id", hero.GetTeamId() }
                });
            }
        }

        [System.Serializable]
        public class MapData
        {
            public Tilemap tilemap;
            public Tilemap collisionTilemap;
            public Dictionary<Vector3Int, TileContent> tileContents;
        }

        [System.Serializable]
        public class TileContent
        {
            public bool hasMonster;
            public string monsterId;
            public bool hasTreasure;
            public string treasureId;
            public bool hasCastle;
            public string castleId;
        }
    }
}