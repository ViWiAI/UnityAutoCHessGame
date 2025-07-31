using System;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using Game.Core;
using Game.Managers;

namespace Game.Animation
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] public SkeletonAnimation characterSkeleton;
        [SerializeField] public GameObject arrowPrefab; // 用于弓箭手远程攻击
        private float skeletonScaleX = 1f;
        private PlayerAnimations currentAnimation;
        private PlayerHero playerHero;

        // 职业动画映射
        private Dictionary<HeroJobs, Dictionary<PlayerAnimations, string>> jobsAnimations = new Dictionary<HeroJobs, Dictionary<PlayerAnimations, string>>();

        private void Awake()
        {
            // 获取SkeletonAnimation组件
            if (characterSkeleton == null)
            {
                characterSkeleton = GetComponentInChildren<SkeletonAnimation>();
                if (characterSkeleton == null)
                {
                    Debug.LogError($"{name} 未找到 SkeletonAnimation 组件！");
                }
            }

            // 获取GearEquipper和PlayerHero组件
          //  gearEquipper = GetComponent<GearEquipper>();
            playerHero = GetComponent<PlayerHero>();
            if (playerHero == null)
            {
                Debug.LogError($"{name} 需要 GearEquipper 和 PlayerHero 组件！");
            }

            // 初始化动画字典
            CreateAnimationsDictionary();
        }

        private void Start()
        {
            // 订阅Spine动画事件
            if (characterSkeleton != null)
            {
                characterSkeleton.AnimationState.Event += OnEventAnimation;
            }
        }

        private void CreateAnimationsDictionary()
        {
            // Warrior动画
            var warriorAnimations = new Dictionary<PlayerAnimations, string>
            {
                { PlayerAnimations.Idle, "Idle" },
                { PlayerAnimations.Walk, "Walk" },
                { PlayerAnimations.Attack1, "Attack1" },
                { PlayerAnimations.Attack2, "Attack2" },
                { PlayerAnimations.Hurt, "Hurt" },
                { PlayerAnimations.Death, "Death" },
                { PlayerAnimations.Special, "Defence" },
                { PlayerAnimations.Buff, "Buff" },
                { PlayerAnimations.Run, "Run" },
                { PlayerAnimations.FullJump, "Jump" },
                { PlayerAnimations.Jump1, "Jump1" },
                { PlayerAnimations.Jump2, "Jump2" },
                { PlayerAnimations.Jump3, "Jump3" }
            };

            // Archer动画
            var archerAnimations = new Dictionary<PlayerAnimations, string>
            {
                { PlayerAnimations.Idle, "Idle ARCHER" },
                { PlayerAnimations.Walk, "Walk" },
                { PlayerAnimations.Attack1, "Shoot1" },
                { PlayerAnimations.Attack2, "Shoot2" },
                { PlayerAnimations.Hurt, "Hurt" },
                { PlayerAnimations.Death, "Death" },
                { PlayerAnimations.Special, "Shoot3" },
                { PlayerAnimations.Buff, "Buff" },
                { PlayerAnimations.Run, "Run ARCHER" },
                { PlayerAnimations.FullJump, "Jump" },
                { PlayerAnimations.Jump1, "Jump1 ARCHER" },
                { PlayerAnimations.Jump2, "Jump2" },
                { PlayerAnimations.Jump3, "Jump3 ARCHER" }
            };

            // Elementalist动画
            var elementalistAnimations = new Dictionary<PlayerAnimations, string>
            {
                { PlayerAnimations.Idle, "Idle" },
                { PlayerAnimations.Walk, "Walk" },
                { PlayerAnimations.Attack1, "Cast1" },
                { PlayerAnimations.Attack2, "Cast2" },
                { PlayerAnimations.Hurt, "Hurt" },
                { PlayerAnimations.Death, "Death" },
                { PlayerAnimations.Special, "Cast3" },
                { PlayerAnimations.Buff, "Buff" },
                { PlayerAnimations.Run, "Fly" },
                { PlayerAnimations.FullJump, "Jump" },
                { PlayerAnimations.Jump1, "Jump1" },
                { PlayerAnimations.Jump2, "Jump2" },
                { PlayerAnimations.Jump3, "Jump3" }
            };

            // Duelist动画
            var duelistAnimations = new Dictionary<PlayerAnimations, string>
            {
                { PlayerAnimations.Idle, "Idle" },
                { PlayerAnimations.Walk, "Walk" },
                { PlayerAnimations.Attack1, "Attack 1 DUELIST" },
                { PlayerAnimations.Attack2, "Attack 2 DUELIST" },
                { PlayerAnimations.Hurt, "Hurt" },
                { PlayerAnimations.Death, "Death" },
                { PlayerAnimations.Special, "Attack 3 DUELIST" },
                { PlayerAnimations.Buff, "Buff" },
                { PlayerAnimations.Run, "Run DUELIST" },
                { PlayerAnimations.FullJump, "Jump" },
                { PlayerAnimations.Jump1, "Jump1" },
                { PlayerAnimations.Jump2, "Jump2" },
                { PlayerAnimations.Jump3, "Jump3" }
            };

            jobsAnimations.Add(HeroJobs.Warrior, warriorAnimations);
            jobsAnimations.Add(HeroJobs.Archer, archerAnimations);
            jobsAnimations.Add(HeroJobs.Elementalist, elementalistAnimations);
            jobsAnimations.Add(HeroJobs.Duelist, duelistAnimations);
        }

        private void OnEventAnimation(TrackEntry trackEntry, Spine.Event e)
        {
            if (e.Data.Name == "OnArrowLeftBow")
            {
                // 弓箭手远程攻击，触发箭矢发射
                Vector3 arrowStartingPosition = transform.position; // 默认使用角色位置
                float angle = 0f;
                string animName = trackEntry.Animation.ToString();
                Transform firePoints = transform.Find("ArrowsFirePoints");

                if (firePoints != null)
                {
                    if (animName == "Shoot1")
                    {
                        arrowStartingPosition = firePoints.Find("FirePoint_Shoot1")?.position ?? transform.position;
                    }
                    else if (animName == "Shoot2")
                    {
                        arrowStartingPosition = firePoints.Find("FirePoint_Shoot2")?.position ?? transform.position;
                    }
                    else if (animName == "Shoot3")
                    {
                        arrowStartingPosition = firePoints.Find("FirePoint_Shoot3")?.position ?? transform.position;
                        angle = 50f;
                    }
                }

                // 调整箭矢方向以匹配角色朝向
                float adjustedAngle = skeletonScaleX < 0 ? 180f - angle : angle;
                GameObject newArrow = Instantiate(arrowPrefab, arrowStartingPosition, Quaternion.Euler(0, 0, 90 + adjustedAngle));
                float velocityX = Mathf.Cos(adjustedAngle * Mathf.Deg2Rad) * 3200f * skeletonScaleX;
                float velocityY = Mathf.Sin(adjustedAngle * Mathf.Deg2Rad) * 3200f;
                newArrow.GetComponent<Rigidbody2D>().velocity = new Vector2(velocityX, velocityY);
                Debug.Log($"发射箭矢: 位置={arrowStartingPosition}, 角度={adjustedAngle}, 速度=({velocityX}, {velocityY})");
            }
        }

        public void ChangeAnimation(string animationString, HeroJobs job)
        {
            if (playerHero.isDead && animationString != "Death") return;

            try
            {
                PlayerAnimations newAnimation = (PlayerAnimations)Enum.Parse(typeof(PlayerAnimations), animationString);
                if (newAnimation != currentAnimation)
                {
                    currentAnimation = newAnimation;
                    AnimationManager(job);
                }
            }
            catch
            {
                Debug.LogWarning($"无效的动画名称: {animationString}");
            }
        }

        public void SetOrientation(Vector3 direction)
        {
            skeletonScaleX = direction.x < 0 ? -1f : 1f;
            if (characterSkeleton != null)
            {
                characterSkeleton.skeleton.ScaleX = skeletonScaleX;
                Debug.Log($"设置朝向: direction={direction}, skeletonScaleX={skeletonScaleX}");
            }
            else
            {
                Debug.LogWarning($"SetOrientation: characterSkeleton 未设置");
            }
        }

        public void JobChanged(HeroJobs newJob)
        {
            //if (gearEquipper.Job != newJob)
            //{
            //    AnimationManager(newJob);
            //    Debug.Log($"职业切换为: {newJob}, 重新播放动画: {currentAnimation}");
            //}
        }

        private void AnimationManager(HeroJobs job)
        {
            if (characterSkeleton == null || !jobsAnimations.ContainsKey(job))
            {
                Debug.LogWarning($"AnimationManager: characterSkeleton 或职业 {job} 未正确配置");
                return;
            }

            bool isLoop = currentAnimation != PlayerAnimations.Death;
            string animationName = jobsAnimations[job][currentAnimation];

            try
            {
                characterSkeleton.AnimationState.SetAnimation(0, animationName, isLoop);
                characterSkeleton.skeleton.ScaleX = skeletonScaleX; // 确保朝向正确
                Debug.Log($"播放动画: {animationName} for {job} (isLoop: {isLoop}, skeletonScaleX: {skeletonScaleX})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"无法播放动画 {animationName} for {job}: {ex.Message}");
            }
        }
    }

    public enum PlayerAnimations
    {
        Idle, Walk, Attack1, Attack2, Hurt, Death, Special, Buff, Run, FullJump, Jump1, Jump2, Jump3
    }

    public enum HeroJobs
    {
        Warrior, Archer, Elementalist, Duelist
    }
}