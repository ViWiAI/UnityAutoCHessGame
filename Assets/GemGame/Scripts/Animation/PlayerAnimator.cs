using System;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using Game.Core;
using Game.Data;

namespace Game.Animation
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] public SkeletonAnimation characterSkeleton;
        [SerializeField] public GameObject arrowPrefab; // ���ڹ�����Զ�̹���
        private float skeletonScaleX = 1f;
        private PlayerAnimations currentAnimation;
        private PlayerHero playerHero;

        // ְҵ����ӳ��
        private Dictionary<HeroRole, Dictionary<PlayerAnimations, string>> jobsAnimations = new Dictionary<HeroRole, Dictionary<PlayerAnimations, string>>();

        private void Awake()
        {
            // ��ȡSkeletonAnimation���
            if (characterSkeleton == null)
            {
                characterSkeleton = GetComponentInChildren<SkeletonAnimation>();
                if (characterSkeleton == null)
                {
                    Debug.LogError($"{name} δ�ҵ� SkeletonAnimation �����");
                }
            }

            // ��ȡGearEquipper��PlayerHero���
          //  gearEquipper = GetComponent<GearEquipper>();
            playerHero = GetComponent<PlayerHero>();
            if (playerHero == null)
            {
                Debug.LogError($"{name} ��Ҫ PlayerHero �����");
            }

            // ��ʼ�������ֵ�
            CreateAnimationsDictionary();
        }

        private void Start()
        {
            // ����Spine�����¼�
            if (characterSkeleton != null)
            {
                characterSkeleton.AnimationState.Event += OnEventAnimation;
            }
        }

        private void CreateAnimationsDictionary()
        {
            // Warrior����
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

            // Archer����
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

            // Elementalist����
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

            // Duelist����
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

            jobsAnimations.Add(HeroRole.Warrior, warriorAnimations);
            jobsAnimations.Add(HeroRole.Mage, elementalistAnimations);
            jobsAnimations.Add(HeroRole.Hunter, archerAnimations);
            jobsAnimations.Add(HeroRole.Rogue, duelistAnimations);
            jobsAnimations.Add(HeroRole.Priest, elementalistAnimations);
        }

        private void OnEventAnimation(TrackEntry trackEntry, Spine.Event e)
        {
            if (e.Data.Name == "OnArrowLeftBow")
            {
                // ������Զ�̹�����������ʸ����
                Vector3 arrowStartingPosition = transform.position; // Ĭ��ʹ�ý�ɫλ��
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

                // ������ʸ������ƥ���ɫ����
                float adjustedAngle = skeletonScaleX < 0 ? 180f - angle : angle;
                GameObject newArrow = Instantiate(arrowPrefab, arrowStartingPosition, Quaternion.Euler(0, 0, 90 + adjustedAngle));
                float velocityX = Mathf.Cos(adjustedAngle * Mathf.Deg2Rad) * 3200f * skeletonScaleX;
                float velocityY = Mathf.Sin(adjustedAngle * Mathf.Deg2Rad) * 3200f;
                newArrow.GetComponent<Rigidbody2D>().velocity = new Vector2(velocityX, velocityY);
                Debug.Log($"�����ʸ: λ��={arrowStartingPosition}, �Ƕ�={adjustedAngle}, �ٶ�=({velocityX}, {velocityY})");
            }
        }

        public void ChangeAnimation(string animationString, HeroRole job)
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
                Debug.LogWarning($"��Ч�Ķ�������: {animationString}");
            }
        }

        public void SetOrientation(Vector3 direction)
        {
            skeletonScaleX = direction.x < 0 ? -1f : 1f;
            if (characterSkeleton != null)
            {
                characterSkeleton.skeleton.ScaleX = skeletonScaleX;
              //  Debug.Log($"���ó���: direction={direction}, skeletonScaleX={skeletonScaleX}");
            }
            else
            {
                Debug.LogWarning($"SetOrientation: characterSkeleton δ����");
            }
        }

        public void JobChanged(HeroRole newJob)
        {
            //if (gearEquipper.Job != newJob)
            //{
            //    AnimationManager(newJob);
            //    Debug.Log($"ְҵ�л�Ϊ: {newJob}, ���²��Ŷ���: {currentAnimation}");
            //}
        }

        private void AnimationManager(HeroRole job)
        {
            if (characterSkeleton == null || !jobsAnimations.ContainsKey(job))
            {
                Debug.LogWarning($"AnimationManager: characterSkeleton ��ְҵ {job} δ��ȷ����");
                return;
            }

            bool isLoop = currentAnimation != PlayerAnimations.Death;
            string animationName = jobsAnimations[job][currentAnimation];

            try
            {
                characterSkeleton.AnimationState.SetAnimation(0, animationName, isLoop);
                characterSkeleton.skeleton.ScaleX = skeletonScaleX; // ȷ��������ȷ
              //  Debug.Log($"���Ŷ���: {animationName} for {job} (isLoop: {isLoop}, skeletonScaleX: {skeletonScaleX})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"�޷����Ŷ��� {animationName} for {job}: {ex.Message}");
            }
        }
    }
}