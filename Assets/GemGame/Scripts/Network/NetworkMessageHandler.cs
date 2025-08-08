using Game.Animation;
using Game.Combat;
using Game.Core;
using Game.Managers;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Network
{
    public class NetworkMessageHandler : MonoBehaviour
    {
        public static NetworkMessageHandler Instance { get; private set; }

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
            WebSocketManager.Instance.OnMessageReceived += HandleServerMessage;
        }

        private void OnDestroy()
        {
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.OnMessageReceived -= HandleServerMessage;
            }
        }

        private void HandleServerMessage(Dictionary<string, object> data)
        {
            if (!data.ContainsKey("type"))
            {
                Debug.LogWarning("�յ���Ч�ķ�������Ϣ��ȱ�� type �ֶ�");
                return;
            }

            string type = data["type"].ToString();
            string playerId = data.ContainsKey("player_id") ? data["player_id"].ToString() : null;

            // ����������ҵ���Ϣ
            if (playerId != null && playerId != GameManager.Instance.GetLocalPlayer()?.GetPlayerId())
            {
                switch (type)
                {
                    case "player_online":
                        if (data.ContainsKey("map_id") && data.ContainsKey("position") && data.ContainsKey("job"))
                        {
                            HeroJobs job = (HeroJobs)System.Enum.Parse(typeof(HeroJobs), data["job"].ToString());
                            var position = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonConvert.SerializeObject(data["position"]));
                            Vector3Int cellPos = new Vector3Int(position["x"], position["y"], position["z"]);
                            string mapId = data["map_id"].ToString();
                            PlayerManager.Instance.AddPlayer(playerId, cellPos, mapId, job);
                            Debug.Log($"�յ��������������Ϣ: {playerId}, ��ͼ: {mapId}, λ��: {cellPos}, ְҵ: {job}");
                        }
                        else
                        {
                            Debug.LogWarning($"player_online ��Ϣȱ�ٱ�Ҫ�ֶ�: {JsonConvert.SerializeObject(data)}");
                        }
                        break;
                    case "player_move":
                        if (data.ContainsKey("map_id") && data.ContainsKey("position"))
                        {
                            HeroJobs job = data.ContainsKey("job") ?
                                (HeroJobs)System.Enum.Parse(typeof(HeroJobs), data["job"].ToString()) :
                                HeroJobs.Warrior;
                            var position = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonConvert.SerializeObject(data["position"]));
                            Vector3Int cellPos = new Vector3Int(position["x"], position["y"], position["z"]);
                            string mapId = data["map_id"].ToString();
                            PlayerManager.Instance.UpdatePlayerPosition(playerId, cellPos, mapId, job);
                        }
                        else
                        {
                            Debug.LogWarning($"player_move ��Ϣȱ�� map_id �� position �ֶ�: {JsonConvert.SerializeObject(data)}");
                        }
                        break;
                    case "player_offline":
                        PlayerManager.Instance.RemovePlayer(playerId);
                        Debug.Log($"�յ����������Ϣ: {playerId}");
                        break;
                }
                return;
            }



            // ��������ҵ���Ϣ
            PlayerHero localPlayer = GameManager.Instance.GetLocalPlayer();
            if (localPlayer == null && GameManager.Instance.GetLoginStatus() == true)
            {
                Debug.LogWarning("�������δ��ʼ�����޷�������Ϣ");
                return;
            }

            switch (type)
            {
                case "player_login_result":
                    if(data.ContainsKey("status"))
                    {
                        string status = data["status"].ToString();
                        if(status == "0")
                        {
                            UIManager.Instance.ShowTipsMessage("��¼�ɹ�");
                            UIManager.Instance.Close_Login();
                            UIManager.Instance.ShowUIButton(false);
                            UIManager.Instance.ShowCharacterSelectPanel(true);
                        }
                        else if(status == "1")
                        {
                            UIManager.Instance.ShowErrorMessage("����˺ű�����,����ϵ�ͷ�");
                        }
                    }
                    break;
                case "player_map":
                    if (data.ContainsKey("map_id") && data.ContainsKey("position"))
                    {
                        string mapId = data["map_id"].ToString();
                        var mapPosition = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonConvert.SerializeObject(data["position"]));
                        Vector3Int position = new Vector3Int(mapPosition["x"], mapPosition["y"], mapPosition["z"]);
                        localPlayer.SetCurrentMapId(mapId);
                        localPlayer.transform.position = MapManager.Instance.GetTilemap().GetCellCenterWorld(position);
                        if (data.ContainsKey("job"))
                        {
                            HeroJobs job = (HeroJobs)System.Enum.Parse(typeof(HeroJobs), data["job"].ToString());
                            localPlayer.SetJob(job);
                        }
                        Debug.Log($"��� {playerId} ��ʼ������ͼ {mapId}��λ�� {position}, ְҵ: {localPlayer.GetJob()}");
                    }
                    break;
                case "move_confirmed":
                    if (data.ContainsKey("position"))
                    {
                        var movePosition = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonConvert.SerializeObject(data["position"]));
                        Vector3Int cellPos = new Vector3Int(movePosition["x"], movePosition["y"], movePosition["z"]);
                        Debug.Log($"{playerId} move_confirmed");
                        localPlayer.moveToBase(cellPos);
                    }
                    break;
                case "team_created":
                case "team_joined":
                    string teamId = data["team_id"].ToString();
                    localPlayer.JoinTeam(teamId);
                    //UIManager.Instance.UpdateTeamUI(data["team_members"] as List<object>);
                    break;
                case "battle_start":
                    string battleMapId = data["battle_map_id"].ToString();
                    string battleRoomId = data["battle_room_id"].ToString();
                    MapManager.Instance?.SwitchMap(battleMapId, battleRoomId);
                    List<Hero> enemies = new List<Hero>();
                    foreach (var enemyData in data["monsters"] as List<object>)
                    {
                        // ʵ�ֹ���ʵ�����߼�
                    }
                    BattleManager.Instance.StartBattle(localPlayer, enemies, battleMapId, battleRoomId, data["team_members"] as List<object>);
                    break;
                case "battle_countdown":
                    int countdown = int.Parse(data["countdown"].ToString());
                    //UIManager.Instance.ShowCountdown(countdown);
                    break;
                case "pvp_match":
                    string pvpMapId = data["pvp_map_id"].ToString();
                    string pvpRoomId = data["pvp_room_id"].ToString();
                    MapManager.Instance?.SwitchMap(pvpMapId, pvpRoomId);
                    List<Hero> opponents = new List<Hero>();
                    foreach (var opponentData in data["opponents"] as List<object>)
                    {
                        // ʵ�ֶ���ʵ�����߼�
                    }
                    // BattleManager.Instance.StartPVP(localPlayer, opponents, pvpMapId, pvpRoomId, data["team_members"] as List<object>);
                    break;
                case "siege_start":
                    string siegeMapId = data["siege_map_id"].ToString();
                    string siegeRoomId = data["siege_room_id"].ToString();
                    MapManager.Instance?.SwitchMap(siegeMapId, siegeRoomId);
                    List<Hero> defenders = new List<Hero>();
                    foreach (var defenderData in data["defenders"] as List<object>)
                    {
                        // ʵ�ַ�����ʵ�����߼�
                    }
                    // BattleManager.Instance.StartSiege(localPlayer, defenders, siegeMapId, siegeRoomId, data["team_members"] as List<object>);
                    break;
                case "error":
                    if (data.ContainsKey("message"))
                    {
                        string message = data["message"].ToString();
                        Debug.LogWarning($"����������{message}");
                        UIManager.Instance.ShowErrorMessage(message);
                    }
                    break;
            }
        }
    }
}