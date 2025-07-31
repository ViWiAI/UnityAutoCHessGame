using Game.Core;
using Game.Managers;
using Game.Network;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }
        private string currentRoomId;
        public List<Hero> teammates;
        public List<Hero> enemies;

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

        public void StartBattle(PlayerHero player, List<Hero> enemies, string battleMapId, string battleRoomId, List<object> teamMembers)
        {
            currentRoomId = battleRoomId;
            teammates = new List<Hero> { player };
            this.enemies = enemies;

            foreach (var memberId in teamMembers)
            {
                if (memberId.ToString() != player.GetPlayerId())
                {
                   // Hero teammate = InstantiatePlayer(memberId.ToString());
                    //teammate.SetCurrentMapId(battleMapId);
                    //teammates.Add(teammate);
                }
            }

            foreach (var enemy in enemies)
            {
                enemy.SetCurrentMapId(battleMapId);
            }

            InitializePositions(battleMapId);
            UIManager.Instance.ShowBattleUI(true);
            WebSocketManager.Instance.Send(new Dictionary<string, object>
            {
                { "type", "battle_ready" },
                { "battle_room_id", battleRoomId },
                { "player_id", player.GetPlayerId() }
            });
            Debug.Log($"PVE 战斗开始: 房间 {battleRoomId}, 地图 {battleMapId}");
        }

        private void InitializePositions(string mapId)
        {
            var mapManager = MapManager.GetMapManager(mapId);
            var tilemap = mapManager.GetTilemap();
            int index = 0;
            foreach (var teammate in teammates)
            {
                teammate.transform.position = tilemap.GetCellCenterWorld(new Vector3Int(index, 0, 0));
                index++;
            }
            index = 0;
            foreach (var enemy in enemies)
            {
                enemy.transform.position = tilemap.GetCellCenterWorld(new Vector3Int(index + 5, 0, 0));
                index++;
            }
        }

        public void EndBattle()
        {
            WebSocketManager.Instance.Send(new Dictionary<string, object>
            {
                { "type", "battle_end" },
                { "battle_room_id", currentRoomId },
                { "player_id", PlayerHero.Instance.GetPlayerId() }
            });
            UIManager.Instance.ShowBattleUI(false);
            currentRoomId = null;
            teammates.Clear();
            enemies.Clear();
        }
    }
}