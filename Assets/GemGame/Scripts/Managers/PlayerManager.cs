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
                Debug.Log("PlayerManager ��ʼ���ɹ�");
            }
            else
            {
                Debug.LogWarning("�����ظ��� PlayerManager");
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
                Debug.Log($"��� {playerId} �Ѵ���");
                return;
            }

            if (playerPrefab == null)
            {
                Debug.LogError("playerPrefab δ����");
                return;
            }

            Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(cellPos);
            GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"Player_{playerId}";
            PlayerHero player = playerObj.GetComponent<PlayerHero>();
            if (player == null)
            {
                Debug.LogError($"playerPrefab ȱ�� PlayerHero ���");
                Destroy(playerObj);
                return;
            }

            player.Initialize(playerId, false, job); // Զ����ң�isLocalPlayer = false
            player.SetCurrentMapId(mapId);
            player.MoveTo(cellPos);
            otherPlayers.Add(playerId, player);
            Debug.Log($"�ɹ����Զ����� {playerId} ��λ�� {cellPos}����ͼ {mapId}, job: {job}");
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
                    player.SetJob(job); // ����ְҵ
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
                Debug.Log($"�Ƴ�Զ����� {playerId}");
            }
        }
    }
}