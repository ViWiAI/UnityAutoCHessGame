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

        [SerializeField] protected Tilemap tilemap; // ����ͼ��Land��
        [SerializeField] protected Tilemap collisionTilemap; // ��ײ��ͼ��Obstacle��
        [SerializeField] private int mapId; // ��ͼ ID
        [SerializeField] private MapType mapType; // ��ͼ����
        [SerializeField] private MapStyle mapStyle; // ��ͼ���
        private int roomId; // ��ǰ���� ID������ս����PVP�ȣ�
        private MapData mapData; // ��ͼ����

        // MapId (int) ���������Ƶ�ӳ��
        private static readonly Dictionary<int, string> MapIdToSceneName = new Dictionary<int, string>
        {
            { 1, "NoviceMap" },    // ������ MapId 1 ��Ӧ WorldMap ����
            { 2, "WorldMapGreen" },  // ������ MapId 2 ��Ӧ DungeonMap ����
            { 3, "SiegeMap" },    // ������ MapId 3 ��Ӧ SiegeMap ����
            // ��Ӹ���ӳ��
        };

        // �¼�������������ˢ��ʱ����
        public delegate void SpawnHandler(Vector3Int cell, string spawnType, string itemId);
        public event SpawnHandler OnSpawn;

        // �¼�������ͼ��ʼ�����л�ʱ����
        public delegate void MapLoadedHandler(MapManager mapManager);
        public event MapLoadedHandler OnMapLoaded;

        private void Awake()
        {
            // ����ģʽ
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"MapManager for mapId {mapId} already exists! Destroying this instance.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ����Ĭ�� mapId
            //if (string.IsNullOrEmpty(mapId))
            //{
            //    mapId = SceneManager.GetActiveScene().name;
            //    Debug.LogWarning($"MapManager {name}: mapId δ���ã�ʹ�ó�������: {mapId}");
            //}

            // ��ʼ�� Tilemap
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
            // Inspector �м������
            if (tilemap == null)
            {
                Debug.LogWarning($"MapManager {name}: Tilemap (Land) δ���䣬��ȷ������������Ϊ 'Land' �� Tilemap��");
            }
            if (collisionTilemap == null)
            {
                Debug.LogWarning($"MapManager {name}: collisionTilemap (Obstacle) δ���䣬��ȷ������������Ϊ 'Obstacle' �� Tilemap��");
            }
            //if (string.IsNullOrEmpty(mapId))
            //{
            //    mapId = SceneManager.GetActiveScene().name;
            //    Debug.LogWarning($"MapManager {name}: mapId δ���ã��Զ�ʹ�ó�������: {mapId}");
            //}
        }

        // ��̬���� Tilemap �� collisionTilemap
        public void UpdateTilemaps()
        {
            // ���ҳ����е����� Tilemap
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            tilemap = null;
            collisionTilemap = null;

            foreach (var tm in tilemaps)
            {
                // ���� Land Tilemap������ͼ��
                if (tm.gameObject.name == "Land" || tm.gameObject.name.Contains("Land"))
                {
                    tilemap = tm;
                }
                // ���� Obstacle Tilemap����ײ/�ϰ����ͼ��
                else if (tm.gameObject.name == "Obstacle" || tm.gameObject.name.Contains("Obstacle"))
                {
                    collisionTilemap = tm;
                }
            }

            // ��� Tilemap ����
            if (tilemap == null)
            {
                Debug.LogError($"MapManager {name} (mapId: {mapId}): δ�ҵ� Land Tilemap����ȷ������������Ϊ 'Land' �� Tilemap��");
            }
            else
            {
                Debug.Log($"MapManager {name} (mapId: {mapId}): �ɹ����� Land Tilemap��");
            }

            if (collisionTilemap == null)
            {
                Debug.LogWarning($"MapManager {name} (mapId: {mapId}): δ�ҵ� Obstacle Tilemap������û����ײ�㡣");
            }
            else
            {
                Debug.Log($"MapManager {name} (mapId: {mapId}): �ɹ����� Obstacle Tilemap��");
            }

            // ���� MapData
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

        // ���������� MapId (int) ת��Ϊ��������
        private string GetSceneNameFromMapId(int mapId)
        {
            if (MapIdToSceneName.TryGetValue(mapId, out string sceneName))
            {
                return sceneName;
            }
            Debug.LogWarning($"δ����ķ����� MapId: {mapId}��Ĭ��ʹ�� WorldMap");
            return "WorldMap"; // Ĭ�ϳ���
        }

        public void SwitchMap(int newMapId, int newRoomId = 1)
        {
            string sceneName = GetSceneNameFromMapId(newMapId);
            if (newMapId == mapId)
            {
                Debug.Log($"MapManager: ���ڵ�ͼ {mapId}������ roomId: {newRoomId}");
                return;
            }

            // �첽�����³���
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single).completed += (op) =>
            {
                mapId = newMapId;
                roomId = newRoomId;
                UpdateTilemaps(); // ���� Tilemap
                InputManager.Instance.GetMouseSprite();
                // ȷ��������ȫ���غ��ٳ�ʼ�����
                StartCoroutine(DelayedPlayerInitialization());
            };
        }

        private IEnumerator DelayedPlayerInitialization()
        {
            // �ȴ�һ֡ȷ�����������ʼ�����
            yield return null;

            Debug.Log($"MapManager: �л����µ�ͼ: {mapId}");
            OnMapLoaded?.Invoke(this);

            // ��ʼ���������
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