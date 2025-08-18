using Game.Animation;
using Game.Core;
using Game.Data;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Managers
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        private Dictionary<int, PlayerHero> otherPlayers = new Dictionary<int, PlayerHero>();

        private PlayerHero localPlayer; // �������

        // ������ Role (int) �� HeroRole ��ӳ��
        private static readonly Dictionary<int, HeroRole> RoleMapping = new Dictionary<int, HeroRole>
        {
            { 1, HeroRole.Warrior }, // ������ Role 1 ��Ӧ Warrior
            { 2, HeroRole.Mage },    // ������ Role 2 ��Ӧ Mage
            { 3, HeroRole.Hunter },  // ������ Role 3 ��Ӧ Hunter
            { 4, HeroRole.Rogue },   // ������ Role 4 ��Ӧ Rogue
            { 5, HeroRole.Priest }   // ������ Role 5 ��Ӧ Priest
        };

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

            // ע�᳡�������¼�
          //  SceneManager.sceneLoaded += OnSceneLoaded;
        }

        //private void OnDestroy()
        //{
        //    SceneManager.sceneLoaded -= OnSceneLoaded;
        //}

        private void Start()
        {
            
        }

        // ���������� Role (int) ת��Ϊ HeroRole
        private HeroRole ConvertRole(int serverRole)
        {
            if (RoleMapping.TryGetValue(serverRole, out HeroRole role))
            {
                return role;
            }
            Debug.LogWarning($"δ����ķ����� Role ֵ: {serverRole}��Ĭ��ʹ�� Warrior");
            return HeroRole.Warrior; // Ĭ��ֵ
        }

        // �����������ʱ����
        //private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        //{
        //    Debug.Log($"PlayerManager: ���� {scene.name} OnSceneLoaded");
        //    if (localPlayer == null && GameManager.Instance.GetOnline())
        //    {
        //        InitializeLocalPlayer(CharacterManager.Instance.selectPLayerCharacter); // ��ʼ���������
        //    }
        //    Debug.Log($"PlayerManager: ���� {scene.name} �������");
        //}

        // ��ʼ���������
        public void InitializeLocalPlayer(PlayerCharacterInfo playerCharacter)
        {
            if (localPlayer != null)
            {
                Debug.LogWarning($"������� {localPlayer.GetPlayerId()} �Ѵ���");
                return;
            }

            // ȷ��MapManager�ѳ�ʼ��
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogError("MapManager �� Tilemap δ��ʼ��");
                return;
            }

            // ������Ҷ���
            int playerId = playerCharacter.CharacterId;
            Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(new Vector3Int(playerCharacter.X, playerCharacter.Y, 0));
            HeroRole role = ConvertRole(playerCharacter.Role);
            int mapId = playerCharacter.MapId;

            GameObject playerPrefab = CharacterManager.Instance.InitPlayerObj(role, playerCharacter.SkinId);
            if (playerPrefab == null)
            {
                Debug.LogError("�޷���ȡ���Ԥ����");
                return;
            }

            GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"LocalPlayer_{playerId}";

            localPlayer = playerObj.GetComponent<PlayerHero>();
            if (localPlayer == null)
            {
                Debug.LogError("��Ҷ���ȱ��PlayerHero���");
                Destroy(playerObj);
                return;
            }

            localPlayer.Initialize(playerId, true, role);
            localPlayer.SetCurrentMapId(mapId);

            // ȷ��PlayerCameraFollow�ѳ�ʼ��
            if (PlayerCameraFollow.Instance == null)
            {
                Debug.LogError("PlayerCameraFollowδ��ʼ��");
                return;
            }

            // ����������� - ����ӳ�ȷ��transform�Ѹ���
            StartCoroutine(InitializePlayer());
            Debug.Log($"�ɹ���ʼ��������� {playerId}");
        }

        private IEnumerator InitializePlayer()
        {

            yield return null; // �ȴ�һ֡ȷ�������������

            if (PlayerCameraFollow.Instance != null)
            {
                PlayerCameraFollow.Instance.SetPlayerTarget(localPlayer.transform);
            }
            else
            {
                Debug.LogError("Camera controller not found");
            }
        }

        public void AddPlayer(int playerId, Vector3Int cellPos, int mapId, HeroRole job = HeroRole.Warrior)
        {
            if (otherPlayers.ContainsKey(playerId))
            {
                Debug.Log($"��� {playerId} �Ѵ���");
                return;
            }

            GameObject playerObj = CharacterManager.Instance.InitPlayerObj(HeroRole.Hunter, 1);
            Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(cellPos);
            playerObj.name = $"Player_{playerId}";
            PlayerHero player = playerObj.GetComponent<PlayerHero>();
            if (player == null)
            {
                Debug.LogError($"playerPrefab ȱ�� PlayerHero ���");
                Destroy(playerObj);
                return;
            }

            player.Initialize(playerId, false, job); // Զ�����
            player.SetCurrentMapId(mapId);
            player.MoveTo(cellPos);
            otherPlayers.Add(playerId, player);
            Debug.Log($"�ɹ����Զ����� {playerId} ��λ�� {cellPos}, ��ͼ {mapId}, ְҵ: {job}");
        }

        public void UpdatePlayerPosition(int playerId, Vector3Int cellPos, int mapId, HeroRole job = HeroRole.Warrior)
        {
            if (localPlayer != null && localPlayer.GetPlayerId() == playerId)
            {
                if (localPlayer.GetCurrentMapId() != mapId)
                {
                    localPlayer.SetCurrentMapId(mapId);
                }
                localPlayer.MoveTo(cellPos);
                if (localPlayer.GetJob() != job)
                {
                    localPlayer.SetJob(job);
                }
            }
            else if (otherPlayers.ContainsKey(playerId))
            {
                var player = otherPlayers[playerId];
                if (player.GetCurrentMapId() != mapId)
                {
                    player.SetCurrentMapId(mapId);
                }
                player.MoveTo(cellPos);
                if (player.GetJob() != job)
                {
                    player.SetJob(job);
                }
            }
            else
            {
                AddPlayer(playerId, cellPos, mapId, job);
            }
        }

        public PlayerHero GetLocalPlayer()
        {
            return localPlayer;
        }

        public void SetLocalPLayer(PlayerHero hero)
        {
            localPlayer = hero;
        }

        public void RemovePlayer(int playerId)
        {
            if (localPlayer != null && localPlayer.GetPlayerId() == playerId)
            {
                Destroy(localPlayer.gameObject);
                localPlayer = null;
                Debug.Log($"�Ƴ�������� {playerId}");
            }
            else if (otherPlayers.ContainsKey(playerId))
            {
                Destroy(otherPlayers[playerId].gameObject);
                otherPlayers.Remove(playerId);
                Debug.Log($"�Ƴ�Զ����� {playerId}");
            }
        }
    }
}