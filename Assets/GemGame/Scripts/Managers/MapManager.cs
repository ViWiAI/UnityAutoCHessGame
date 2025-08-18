using Game.Core;
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

        [SerializeField] protected Tilemap tilemap; // 主地图（Land）
        [SerializeField] protected Tilemap collisionTilemap; // 碰撞地图（Obstacle）
        [SerializeField] private int mapId; // 地图 ID
        [SerializeField] private MapType mapType; // 地图类型
        [SerializeField] private MapStyle mapStyle; // 地图风格
        private int roomId; // 当前房间 ID（用于战斗、PVP等）
        private MapData mapData; // 地图数据

        // MapId (int) 到场景名称的映射
        private static readonly Dictionary<int, string> MapIdToSceneName = new Dictionary<int, string>
        {
            { 1, "NoviceMap" },    // 服务器 MapId 1 对应 WorldMap 场景
            { 2, "WorldMapGreen" },  // 服务器 MapId 2 对应 DungeonMap 场景
            { 3, "SiegeMap" },    // 服务器 MapId 3 对应 SiegeMap 场景
            // 添加更多映射
        };

        // 事件：当怪物、宝物等刷新时触发
        public delegate void SpawnHandler(Vector3Int cell, string spawnType, string itemId);
        public event SpawnHandler OnSpawn;

        // 事件：当地图初始化或切换时触发
        public delegate void MapLoadedHandler(MapManager mapManager);
        public event MapLoadedHandler OnMapLoaded;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"MapManager for mapId {mapId} already exists! Destroying this instance.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 设置默认 mapId
            //if (string.IsNullOrEmpty(mapId))
            //{
            //    mapId = SceneManager.GetActiveScene().name;
            //    Debug.LogWarning($"MapManager {name}: mapId 未设置，使用场景名称: {mapId}");
            //}

            // 初始化 Tilemap
            UpdateTilemaps();

            Debug.Log($"MapManager initialized for mapId: {mapId}, Type: {mapType}, Style: {mapStyle}");
            OnMapLoaded?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnValidate()
        {
            // Inspector 中检查配置
            if (tilemap == null)
            {
                Debug.LogWarning($"MapManager {name}: Tilemap (Land) 未分配，请确保场景中有名为 'Land' 的 Tilemap。");
            }
            if (collisionTilemap == null)
            {
                Debug.LogWarning($"MapManager {name}: collisionTilemap (Obstacle) 未分配，请确保场景中有名为 'Obstacle' 的 Tilemap。");
            }
            //if (string.IsNullOrEmpty(mapId))
            //{
            //    mapId = SceneManager.GetActiveScene().name;
            //    Debug.LogWarning($"MapManager {name}: mapId 未设置，自动使用场景名称: {mapId}");
            //}
        }

        // 动态更新 Tilemap 和 collisionTilemap
        public void UpdateTilemaps()
        {
            // 查找场景中的所有 Tilemap
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            tilemap = null;
            collisionTilemap = null;

            foreach (var tm in tilemaps)
            {
                // 查找 Land Tilemap（主地图）
                if (tm.gameObject.name == "Land" || tm.gameObject.name.Contains("Land"))
                {
                    tilemap = tm;
                }
                // 查找 Obstacle Tilemap（碰撞/障碍物地图）
                else if (tm.gameObject.name == "Obstacle" || tm.gameObject.name.Contains("Obstacle"))
                {
                    collisionTilemap = tm;
                }
            }

            // 检查 Tilemap 配置
            if (tilemap == null)
            {
                Debug.LogError($"MapManager {name} (mapId: {mapId}): 未找到 Land Tilemap！请确保场景中有名为 'Land' 的 Tilemap。");
            }
            else
            {
                Debug.Log($"MapManager {name} (mapId: {mapId}): 成功加载 Land Tilemap。");
            }

            if (collisionTilemap == null)
            {
                Debug.LogWarning($"MapManager {name} (mapId: {mapId}): 未找到 Obstacle Tilemap，可能没有碰撞层。");
            }
            else
            {
                Debug.Log($"MapManager {name} (mapId: {mapId}): 成功加载 Obstacle Tilemap。");
            }

            // 更新 MapData
            mapData = new MapData
            {
                tilemap = tilemap,
                collisionTilemap = collisionTilemap,
                tileContents = new Dictionary<Vector3Int, TileContent>()
            };
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

        // 将服务器的 MapId (int) 转换为场景名称
        private string GetSceneNameFromMapId(int mapId)
        {
            if (MapIdToSceneName.TryGetValue(mapId, out string sceneName))
            {
                return sceneName;
            }
            Debug.LogWarning($"未定义的服务器 MapId: {mapId}，默认使用 WorldMap");
            return "WorldMap"; // 默认场景
        }

        public void SwitchMap(int newMapId, int newRoomId = 1)
        {
            string sceneName = GetSceneNameFromMapId(newMapId);
            if (newMapId == mapId)
            {
                Debug.Log($"MapManager: 已在地图 {mapId}，更新 roomId: {newRoomId}");
                return;
            }

            // 异步加载新场景
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single).completed += (op) =>
            {
                mapId = newMapId;
                roomId = newRoomId;
                UpdateTilemaps(); // 更新 Tilemap
                InputManager.Instance.GetMouseSprite();
                // 确保场景完全加载后再初始化玩家
                StartCoroutine(DelayedPlayerInitialization());
            };
        }

        private IEnumerator DelayedPlayerInitialization()
        {
            // 等待一帧确保所有组件初始化完成
            yield return null;

            Debug.Log($"MapManager: 切换到新地图: {mapId}");
            OnMapLoaded?.Invoke(this);

            // 初始化本地玩家
            PlayerManager.Instance.InitializeLocalPlayer(CharacterManager.Instance.selectPLayerCharacter);
        }

        public Tilemap GetTilemap() => mapData.tilemap;
        public Tilemap GetCollisionTilemap() => mapData.collisionTilemap;
        public int GetMapId() => mapId;
        public int GetRoomId() => roomId;
        public MapType GetMapType() => mapType;
        public MapStyle GetMapStyle() => mapStyle;

        

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
                // WebSocketManager.Instance.Send(new Dictionary<string, object>
                // {
                //     { "type", "interact" },
                //     { "map_id", mapId },
                //     { "cell", new { x = cell.x, y = cell.y, z = cell.z } },
                //     { "interact_type", type },
                //     { "item_id", itemId },
                //     { "player_id", hero.GetPlayerId() },
                //     { "team_id", hero.GetTeamId() }
                // });
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