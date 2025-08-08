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
        // ����ʵ��
        public static MapManager Instance { get; private set; }

        public enum MapType
        {
            WorldMap,    // �����ͼ
            DungeonMap,  // ����ս����ͼ
            PVPMap,      // PVP��ͼ
            SiegeMap     // ���ǵ�ͼ
        }

        public enum MapStyle
        {
            Desert,      // ɳĮ
            Grassland,   // �̵�
            Snow,        // ��ѩ
            Lost,        // ʧ��
            Exotic,      // ����
            Wasteland,   // ����
            Swamp,       // ����
            Volcano      // ��ɽ
        }

        [SerializeField] protected Tilemap tilemap; // ����ͼ
        [SerializeField] protected Tilemap collisionTilemap; // ��ײ��ͼ
        [SerializeField] private string mapId; // ��ͼ ID
        [SerializeField] private MapType mapType; // ��ͼ����
        [SerializeField] private MapStyle mapStyle; // ��ͼ���
        private string roomId; // ��ǰ���� ID������ս����PVP�ȣ�
        private MapData mapData; // ��ͼ����

        // �¼�������������ˢ��ʱ����
        public delegate void SpawnHandler(Vector3Int cell, string spawnType, string itemId);
        public event SpawnHandler OnSpawn;

        // �¼�������ͼ��ʼ�����л�ʱ����
        public delegate void MapLoadedHandler(MapManager mapManager);
        public event MapLoadedHandler OnMapLoaded;

        private void Awake()
        {
            // ����ģʽ��ȷ��ֻ��һ�� MapManager ʵ��
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"MapManager for mapId {mapId} already exists! Destroying this instance.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // ��ѡ��������������Ƿ�糡������

            // ����Ĭ�� mapId
            if (string.IsNullOrEmpty(mapId))
            {
                mapId = SceneManager.GetActiveScene().name;
                Debug.LogWarning($"MapManager {name}: mapId δ���ã�ʹ�ó�������: {mapId}");
            }

            // ��� Tilemap ����
            if (tilemap == null)
            {
                Debug.LogError($"MapManager {name} (mapId: {mapId}): δ���� Tilemap������ Inspector �����á�");
                return;
            }
            if (collisionTilemap == null)
            {
                Debug.LogWarning($"MapManager {name} (mapId: {mapId}): δ���� collisionTilemap������û����ײ�㡣");
            }

            // ��ʼ�� MapData
            mapData = new MapData
            {
                tilemap = tilemap,
                collisionTilemap = collisionTilemap,
                tileContents = new Dictionary<Vector3Int, TileContent>()
            };

            // ���ݵ�ͼ���Ӧ�û���Ч��
            ApplyMapStyleEffects();

            Debug.Log($"MapManager initialized for mapId: {mapId}, Type: {mapType}, Style: {mapStyle}");
            OnMapLoaded?.Invoke(this);
        }

        private void OnDestroy()
        {
            // �Ƴ� WebSocket �¼�����
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.OnMessageReceived -= HandleServerMessage;
            }

            // �����ǰʵ���ǵ�������� Instance
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            // ���� WebSocket �¼�
            WebSocketManager.Instance.OnMessageReceived += HandleServerMessage;

            // �������� PlayerHero
         //   UpdatePlayerHeroes();
        }

        private void OnValidate()
        {
            // �� Inspector �б༭ʱ�������
            if (tilemap == null)
            {
                Debug.LogWarning($"MapManager {name}: Tilemap δ���䣬���� Inspector �����á�");
            }
            if (collisionTilemap == null)
            {
                Debug.LogWarning($"MapManager {name}: collisionTilemap δ���䣬����û����ײ�㡣");
            }
            if (string.IsNullOrEmpty(mapId))
            {
                mapId = SceneManager.GetActiveScene().name;
                Debug.LogWarning($"MapManager {name}: mapId δ���ã��Զ�ʹ�ó�������: {mapId}");
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
                    Debug.Log($"��ͼ {mapId}: Ӧ������Ч���������ƶ��ٶ�");
                    break;
                case MapStyle.Volcano:
                    StartCoroutine(ApplyVolcanoDamage());
                    Debug.Log($"��ͼ {mapId}: Ӧ�û�ɽЧ�����������˺�");
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
                Debug.Log($"MapManager: ���ڵ�ͼ {mapId}������ roomId: {newRoomId}");
                return;
            }

            SceneManager.LoadSceneAsync(newMapId, LoadSceneMode.Single).completed += (op) =>
            {
                Debug.Log($"MapManager: �л����µ�ͼ: {newMapId}, ����: {newRoomId}");
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
                    Debug.Log($"��ͼ {mapId} �� {cell} ������ {spawnType}: {itemId}");
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
                    return $"ʰȡ����: {content.treasureId}";
                }
                if (content.hasMonster && mapType != MapType.PVPMap)
                {
                    NotifyServerInteract(cell, "Monster", content.monsterId);
                    return $"���ֹ���: {content.monsterId}������ս����";
                }
                if (content.hasCastle && mapType == MapType.SiegeMap)
                {
                    NotifyServerInteract(cell, "Castle", content.castleId);
                    return $"���𹥳�: {content.castleId}��";
                }
                return "�˸���Ϊ�գ��ޱ�������Ǳ�";
            }
            return $"��ͼ {mapId} ���������ݣ�";
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