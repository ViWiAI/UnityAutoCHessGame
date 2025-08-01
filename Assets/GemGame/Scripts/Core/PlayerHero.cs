using Game.Animation;
using Game.Combat;
using Game.Managers;
using Game.Network;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game.Core
{
    public class PlayerHero : Hero
    {
        [SerializeField] private HeroJobs currentJob = HeroJobs.Warrior;
        private string playerId;
        private string teamId;
        private Vector3Int currentPosition;
        private float lastSkeletonScaleX = 1f;
        private bool isLocalPlayer;

        public void Initialize(string playerId, bool isLocalPlayer, HeroJobs job = HeroJobs.Warrior)
        {
            this.playerId = playerId.ToLower();
            this.isLocalPlayer = isLocalPlayer;
            this.currentJob = job;
            gameObject.name = $"Player_{playerId}";
            Debug.Log($"PlayerHero 初始化: {gameObject.name}, playerId: {playerId}, isLocalPlayer: {isLocalPlayer}, job: {currentJob}");
        }

        protected override void Awake()
        {
            base.Awake();
            if (string.IsNullOrEmpty(currentMapId))
            {
                currentMapId = SceneManager.GetActiveScene().name;
            }
        }

        protected override void Start()
        {
            base.Start();
            if (isLocalPlayer)
            {
                Debug.Log($"WebSocketManager OnMessageReceived");
                WebSocketManager.Instance.OnMessageReceived += HandleServerMessage;
               // RequestMapData();
            }
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (isLocalPlayer)
            {
                WebSocketManager.Instance.OnMessageReceived -= HandleServerMessage;
            }
            Debug.Log($"PlayerHero {playerId} 销毁，isLocalPlayer: {isLocalPlayer}, job: {currentJob}");
        }

        public void SetJob(HeroJobs newJob)
        {
            if (currentJob != newJob)
            {
                currentJob = newJob;
                Debug.Log($"玩家 {playerId} 设置职业: {newJob}");
            }
        }

        public HeroJobs GetJob() => currentJob;

        public bool IsDead() => isDead;

        public bool IsMoving() => isMoving;

        public void StopMoving()
        {
            isMoving = false;
            path.Clear();
            pathIndex = 0;
        }

        public override void MoveTo(Vector3Int cellPos)
        {
            if (isLocalPlayer)
            {
                if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
                {
                    Debug.LogError($"MapManager 或 Tilemap 未设置，玩家: {playerId}");
                    return;
                }
                WebSocketManager.Instance.Send(new Dictionary<string, object>
                {
                    { "type", "move_request" },
                    { "player_id", playerId },
                    { "map_id", currentMapId },
                    { "cell", new Dictionary<string, int>
                        {
                            { "x", cellPos.x },
                            { "y", cellPos.y },
                            { "z", cellPos.z }
                        }
                    }
                });
                Debug.Log($"本地玩家 {playerId} 发送移动请求到 {cellPos}");
            }
            else
            {
                base.MoveTo(cellPos);
            }
        }

        public override void UpdateOrientation()
        {
            if (!isMoving || path == null || pathIndex >= path.Count)
            {
                return;
            }

            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogWarning($"MapManager 或 Tilemap 未初始化，玩家: {playerId}");
                return;
            }

            Vector3 targetWorldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(path[pathIndex]);
            Vector3 direction = targetWorldPos - transform.position;

            PlayerAnimator animator = GetComponent<PlayerAnimator>();
            if (animator != null)
            {
                animator.SetOrientation(direction);
                lastSkeletonScaleX = animator.characterSkeleton != null ? animator.characterSkeleton.skeleton.ScaleX : lastSkeletonScaleX;
            }
            else
            {
                Debug.LogWarning($"未找到 PlayerAnimator 组件，玩家: {playerId}");
            }
        }

        public override void PlayAnimation(string animationName, HeroJobs job = HeroJobs.Warrior)
        {
            PlayerAnimator animator = GetComponent<PlayerAnimator>();
            if (animator != null)
            {
                animator.ChangeAnimation(animationName, job == HeroJobs.Warrior ? currentJob : job);
            }
            else
            {
                Debug.LogWarning($"未找到 PlayerAnimator 组件，玩家: {playerId}");
            }
        }

        public string GetPlayerId() => playerId;
        public string GetTeamId() => teamId;

        public void SetPlayerId(string playerId)
        {
            this.playerId = playerId.ToLower();
            gameObject.name = $"Player_{playerId}";
            Debug.Log($"设置玩家 ID: {playerId}, isLocalPlayer: {isLocalPlayer}, job: {currentJob}");
        }

        public void JoinTeam(string newTeamId)
        {
            teamId = newTeamId;
            WebSocketManager.Instance.Send(new Dictionary<string, object>
            {
                { "type", "team_join" },
                { "player_id", playerId },
                { "team_id", teamId }
            });
        }

        public void RequestPVPMatch(string mode)
        {
            WebSocketManager.Instance.Send(new Dictionary<string, object>
            {
                { "type", "pvp_match" },
                { "player_id", playerId },
                { "team_id", teamId },
                { "mode", mode }
            });
        }

        private void HandleServerMessage(Dictionary<string, object> data)
        {
            if (!data.ContainsKey("type"))
            {
                Debug.LogWarning("收到无效的服务器消息，缺少 type 字段");
                return;
            }

            string type = data["type"].ToString();

            if (data.ContainsKey("player_id") && data["player_id"].ToString() != playerId)
            {
                if (type == "player_move")
                {
                    if (data.ContainsKey("map_id") && data.ContainsKey("position"))
                    {
                        HeroJobs job = data.ContainsKey("job") ?
                            (HeroJobs)System.Enum.Parse(typeof(HeroJobs), data["job"].ToString()) :
                            HeroJobs.Warrior;
                        UpdateOtherPlayerPosition(data, job);
                    }
                    else
                    {
                        Debug.LogWarning($"player_move 消息缺少 map_id 或 position 字段: {JsonConvert.SerializeObject(data)}");
                    }
                }
                return;
            }

            switch (type)
            {
                case "player_map":
                    if (data.ContainsKey("map_id") && data.ContainsKey("position"))
                    {
                        currentMapId = data["map_id"].ToString();
                        var mapPosition = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonConvert.SerializeObject(data["position"]));
                        currentPosition = new Vector3Int(mapPosition["x"], mapPosition["y"], mapPosition["z"]);
                        if (MapManager.Instance != null)
                        {
                            transform.position = MapManager.Instance.GetTilemap().GetCellCenterWorld(currentPosition);
                            SetTilemap(MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap());
                            if (data.ContainsKey("job"))
                            {
                                HeroJobs job = (HeroJobs)System.Enum.Parse(typeof(HeroJobs), data["job"].ToString());
                                SetJob(job);
                            }
                            Debug.Log($"玩家 {playerId} 初始化到地图 {currentMapId}，位置 {currentPosition}, 职业: {currentJob}");
                        }
                        else
                        {
                            Debug.LogWarning($"MapManager 单例未初始化，地图: {currentMapId}");
                        }
                    }
                    break;
                case "move_confirmed":
                    if (data.ContainsKey("position"))
                    {
                        var movePosition = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonConvert.SerializeObject(data["position"]));
                        Vector3Int cellPos = new Vector3Int(movePosition["x"], movePosition["y"], movePosition["z"]);
                        Debug.Log($"{playerId} move_confirmed");
                        base.MoveTo(cellPos);
                    }
                    break;
                case "team_created":
                case "team_joined":
                    teamId = data["team_id"].ToString();
                    UIManager.Instance.UpdateTeamUI(data["team_members"] as List<object>);
                    break;
                case "battle_start":
                    string battleMapId = data["battle_map_id"].ToString();
                    string battleRoomId = data["battle_room_id"].ToString();
                    MapManager.Instance?.SwitchMap(battleMapId, battleRoomId);
                    List<Hero> enemies = new List<Hero>();
                    foreach (var enemyData in data["monsters"] as List<object>)
                    {
                        // 实现怪物实例化逻辑
                    }
                    BattleManager.Instance.StartBattle(this, enemies, battleMapId, battleRoomId, data["team_members"] as List<object>);
                    break;
                case "battle_countdown":
                    int countdown = int.Parse(data["countdown"].ToString());
                    UIManager.Instance.ShowCountdown(countdown);
                    break;
                case "pvp_match":
                    string pvpMapId = data["pvp_map_id"].ToString();
                    string pvpRoomId = data["pvp_room_id"].ToString();
                    MapManager.Instance?.SwitchMap(pvpMapId, pvpRoomId);
                    List<Hero> opponents = new List<Hero>();
                    foreach (var opponentData in data["opponents"] as List<object>)
                    {
                        // 实现对手实例化逻辑
                    }
                    // BattleManager.Instance.StartPVP(this, opponents, pvpMapId, pvpRoomId, data["team_members"] as List<object>);
                    break;
                case "siege_start":
                    string siegeMapId = data["siege_map_id"].ToString();
                    string siegeRoomId = data["siege_room_id"].ToString();
                    MapManager.Instance?.SwitchMap(siegeMapId, siegeRoomId);
                    List<Hero> defenders = new List<Hero>();
                    foreach (var defenderData in data["defenders"] as List<object>)
                    {
                        // 实现防守者实例化逻辑
                    }
                    // BattleManager.Instance.StartSiege(this, defenders, siegeMapId, siegeRoomId, data["team_members"] as List<object>);
                    break;
            }
        }

        private void UpdateOtherPlayerPosition(Dictionary<string, object> data, HeroJobs job)
        {
            string playerId = data["player_id"].ToString();
            string mapId = data["map_id"].ToString();
            var position = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonConvert.SerializeObject(data["position"]));
            Vector3Int cellPos = new Vector3Int(position["x"], position["y"], position["z"]);
            PlayerManager.Instance.UpdatePlayerPosition(playerId, cellPos, mapId, job);
        }

        private void RequestMapData()
        {
            WebSocketManager.Instance.Send(new Dictionary<string, object>
            {
                { "type", "get_player_map" },
                { "player_id", playerId }
            });
        }
    }
}