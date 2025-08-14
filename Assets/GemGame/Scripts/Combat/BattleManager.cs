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
        private Dictionary<Hero, Hero> lastAttackers = new Dictionary<Hero, Hero>(); // ��¼��󹥻��ߵ�ӳ��

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
            teammates.Clear(); // ��վɶ���
            teammates.Add(player);
            this.enemies = enemies ?? new List<Hero>();

            foreach (var memberId in teamMembers)
            {
                if (memberId.ToString() != player.GetPlayerId())
                {
                    // ������� InstantiatePlayer ��������ʵ�ֻ��滻��
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
            Debug.Log($"PVE ս����ʼ: ���� {battleRoomId}, ��ͼ {battleMapId}, ������: {teammates.Count}, ������: {enemies.Count}");
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
            lastAttackers.Clear(); // �������߼�¼
            teammates.Clear();
            enemies.Clear();
            Debug.Log("ս���������������͵����б�");
        }

        // ��������¼������
        public void RecordAttack(Hero target, Hero attacker)
        {
            if (target != null && attacker != null)
            {
                lastAttackers[target] = attacker;
                Debug.Log($"��¼����: {attacker.heroName} ������ {target.heroName}");
            }
        }

        // ��������ȡ��󹥻���
        public Hero GetLastAttacker(Hero target)
        {
            lastAttackers.TryGetValue(target, out Hero attacker);
            return attacker;
        }

        // ������ע����ѣ����ڳ�ʼ����̬��ӣ�
        public void RegisterTeammate(Hero teammate)
        {
            if (teammate != null && !teammates.Contains(teammate))
            {
                teammates.Add(teammate);
                Debug.Log($"ע�����: {teammate.heroName}");
            }
        }
    }
}