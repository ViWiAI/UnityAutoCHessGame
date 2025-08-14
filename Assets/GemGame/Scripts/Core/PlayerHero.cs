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
                // 不再订阅 OnMessageReceived，移到 NetworkMessageHandler
                RequestMapData();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Debug.Log($"PlayerHero {playerId} 销毁，isLocalPlayer: {isLocalPlayer}, job: {currentJob}");
        }

        public void SetJob(HeroRole newJob)
        {
            if (currentJob != newJob)
            {
                currentJob = newJob;
                Debug.Log($"玩家 {playerId} 设置职业: {newJob}");
            }
        }

        public HeroRole GetJob() => currentJob;

        public bool IsDead() => isDead;

        public bool IsMoving() => isMoving;

        public Vector3Int? GetlastClickedCell() => lastClickedCell;

        public void ResetClickedCell()
        {
            lastClickedCell = null;
            Debug.Log($"玩家 {playerId} 重置点击格子记录");
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
                    Debug.LogError($"MapManager 或 Tilemap 未设置，玩家: {playerId}");
                    return;
                }

                // 重复点击检查已由 InputManager 处理，这里直接发送请求
                lastClickedCell = cellPos;

                // 构造二进制 payload
                NetworkMessageHandler.Instance.SendMoveRequest(playerId, currentMapId, cellPos);


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

        public override void PlayAnimation(string animationName, HeroRole job = HeroRole.Warrior)
        {
            PlayerAnimator animator = GetComponent<PlayerAnimator>();
            if (animator != null)
            {
                animator.ChangeAnimation(animationName, job == HeroRole.Warrior ? currentJob : job);
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