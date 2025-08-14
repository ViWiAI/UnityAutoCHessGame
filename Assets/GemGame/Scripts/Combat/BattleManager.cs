using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Managers;
using Game.Network;

namespace Game.Combat
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }
        private string currentRoomId;
        public List<Hero> teammates;
        public List<Hero> enemies;
        private Dictionary<Hero, Hero> lastAttackers = new Dictionary<Hero, Hero>(); // 记录最后攻击者的映射

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                teammates = new List<Hero>();
                enemies = new List<Hero>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartBattle(PlayerHero player, List<Hero> enemies, string battleMapId, string battleRoomId, List<object> teamMembers)
        {
            currentRoomId = battleRoomId;
            teammates.Clear(); // 清空旧队伍
            teammates.Add(player);
            this.enemies = enemies ?? new List<Hero>();

            foreach (var memberId in teamMembers)
            {
                if (memberId.ToString() != player.GetPlayerId())
                {
                    // 假设存在 InstantiatePlayer 方法（需实现或替换）
                    // Hero teammate = InstantiatePlayer(memberId.ToString());
                    // teammate.SetCurrentMapId(battleMapId);
                    // teammates.Add(teammate);
                }
            }

            foreach (var enemy in this.enemies)
            {
                if (enemy != null)
                {
                    enemy.SetCurrentMapId(battleMapId);
                }
            }

            InitializePositions(battleMapId);
            //UIManager.Instance.ShowBattleUI(true);
            //WebSocketManager.Instance.Send(new Dictionary<string, object>
            //{
            //    { "type", "battle_ready" },
            //    { "battle_room_id", battleRoomId },
            //    { "player_id", player.GetPlayerId() }
            //});
            Debug.Log($"PVE 战斗开始: 房间 {battleRoomId}, 地图 {battleMapId}, 队友数: {teammates.Count}, 敌人数: {enemies.Count}");
        }

        private void InitializePositions(string mapId)
        {
            int index = 0;
            foreach (var teammate in teammates)
            {
                if (teammate != null)
                {
                    teammate.transform.position = MapManager.Instance.GetTilemap().GetCellCenterWorld(new Vector3Int(index, 0, 0));
                    index++;
                }
            }
            index = 0;
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.transform.position = MapManager.Instance.GetTilemap().GetCellCenterWorld(new Vector3Int(index + 5, 0, 0));
                    index++;
                }
            }
        }

        public void EndBattle()
        {
            //WebSocketManager.Instance.Send(new Dictionary<string, object>
            //{
            //    { "type", "battle_end" },
            //    { "battle_room_id", currentRoomId },
            //    { "player_id", teammates.Count > 0 ? (teammates[0] as PlayerHero)?.GetPlayerId() : "" }
            //});
            //UIManager.Instance.ShowBattleUI(false);
            currentRoomId = null;
            lastAttackers.Clear(); // 清理攻击者记录
            teammates.Clear();
            enemies.Clear();
            Debug.Log("战斗结束，清理队伍和敌人列表");
        }

        // 新增：记录攻击者
        public void RecordAttack(Hero target, Hero attacker)
        {
            if (target != null && attacker != null)
            {
                lastAttackers[target] = attacker;
                Debug.Log($"记录攻击: {attacker.heroName} 攻击了 {target.heroName}");
            }
        }

        // 新增：获取最后攻击者
        public Hero GetLastAttacker(Hero target)
        {
            lastAttackers.TryGetValue(target, out Hero attacker);
            return attacker;
        }

        // 新增：注册队友（用于初始化或动态添加）
        public void RegisterTeammate(Hero teammate)
        {
            if (teammate != null && !teammates.Contains(teammate))
            {
                teammates.Add(teammate);
                Debug.Log($"注册队友: {teammate.heroName}");
            }
        }
    }
}