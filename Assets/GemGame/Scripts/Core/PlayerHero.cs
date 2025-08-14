using Game.Animation;
using Game.Data;
using Game.Managers;
using Game.Network;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    public class PlayerHero : Hero
    {
        [SerializeField] private HeroRole currentJob = HeroRole.Warrior;
        private string playerId;
        private string teamId;
        private float lastSkeletonScaleX = 1f;
        private Vector3Int? lastClickedCell;

        public void Initialize(string playerId, bool isLocalPlayer, HeroRole job = HeroRole.Warrior)
        {
            this.playerId = playerId.ToLower();
            this.isLocalPlayer = isLocalPlayer;
            this.currentJob = job;
            gameObject.name = $"{playerId}";
            Debug.Log($"PlayerHero ��ʼ��: {gameObject.name}, playerId: {playerId}, isLocalPlayer: {isLocalPlayer}, job: {currentJob}");
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
                // ���ٶ��� OnMessageReceived���Ƶ� NetworkMessageHandler
                RequestMapData();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Debug.Log($"PlayerHero {playerId} ���٣�isLocalPlayer: {isLocalPlayer}, job: {currentJob}");
        }

        public void SetJob(HeroRole newJob)
        {
            if (currentJob != newJob)
            {
                currentJob = newJob;
                Debug.Log($"��� {playerId} ����ְҵ: {newJob}");
            }
        }

        public HeroRole GetJob() => currentJob;

        public bool IsDead() => isDead;

        public bool IsMoving() => isMoving;

        public Vector3Int? GetlastClickedCell() => lastClickedCell;

        public void ResetClickedCell()
        {
            lastClickedCell = null;
            Debug.Log($"��� {playerId} ���õ�����Ӽ�¼");
        }

        public void moveToBase(Vector3Int cellPos)
        {
            base.MoveTo(cellPos);
        }

        public override void MoveTo(Vector3Int cellPos)
        {
            if (isLocalPlayer)
            {
                if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
                {
                    Debug.LogError($"MapManager �� Tilemap δ���ã����: {playerId}");
                    return;
                }

                // �ظ����������� InputManager ��������ֱ�ӷ�������
                lastClickedCell = cellPos;

                // ��������� payload
                NetworkMessageHandler.Instance.SendMoveRequest(playerId, currentMapId, cellPos);


                Debug.Log($"������� {playerId} �����ƶ����� {cellPos}");
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
                Debug.LogWarning($"MapManager �� Tilemap δ��ʼ�������: {playerId}");
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
                Debug.LogWarning($"δ�ҵ� PlayerAnimator ��������: {playerId}");
            }
        }

        public override void PlayAnimation(string animationName, HeroRole job = HeroRole.Warrior)
        {
            PlayerAnimator animator = GetComponent<PlayerAnimator>();
            if (animator != null)
            {
                animator.ChangeAnimation(animationName, job == HeroRole.Warrior ? currentJob : job);
            }
            else
            {
                Debug.LogWarning($"δ�ҵ� PlayerAnimator ��������: {playerId}");
            }
        }

        public string GetPlayerId() => playerId;
        public string GetTeamId() => teamId;

        public void SetPlayerId(string playerId)
        {
            this.playerId = playerId.ToLower();
            gameObject.name = $"Player_{playerId}";
            Debug.Log($"������� ID: {playerId}, isLocalPlayer: {isLocalPlayer}, job: {currentJob}");
        }

        public void JoinTeam(string newTeamId)
        {
            teamId = newTeamId;
            //WebSocketManager.Instance.Send(new Dictionary<string, object>
            //{
            //    { "type", "team_join" },
            //    { "player_id", playerId },
            //    { "team_id", teamId }
            //});
        }

        public void RequestPVPMatch(string mode)
        {
            //WebSocketManager.Instance.Send(new Dictionary<string, object>
            //{
            //    { "type", "pvp_match" },
            //    { "player_id", playerId },
            //    { "team_id", teamId },
            //    { "mode", mode }
            //});
        }

        private void RequestMapData()
        {
            //WebSocketManager.Instance.Send(new Dictionary<string, object>
            //{
            //    { "type", "get_player_map" },
            //    { "player_id", playerId }
            //});
        }
    }
}