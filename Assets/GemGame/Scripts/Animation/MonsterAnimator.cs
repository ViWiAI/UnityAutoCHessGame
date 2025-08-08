using Spine.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Animation
{
    public class MonsterAnimator : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation monsterAnimator;
        private float skeletonScaleX = 1f;

        private void Awake()
        {
            if (monsterAnimator == null)
            {
                monsterAnimator = GetComponent<SkeletonAnimation>();
            }
        }

        public void ChangeAnimation(string animationName)
        {
            if (monsterAnimator == null) return;
            if (!new List<string> { "Idle", "Walk", "Death", "Hurt", "Attack" }.Contains(animationName))
            {
                Debug.LogWarning($"不支持的动画: {animationName}");
                return;
            }

            bool isLoop = animationName != "Death" && animationName != "Hurt";
            monsterAnimator.skeleton.SetSkin("Side");
            monsterAnimator.skeleton.SetSlotsToSetupPose();
            monsterAnimator.skeleton.ScaleX = skeletonScaleX;
            try
            {
                monsterAnimator.AnimationState.SetAnimation(0, "Side_" + animationName, isLoop);
            }
            catch (Exception ex)
            {
                Debug.LogError($"无法播放动画 Side_{animationName}: {ex.Message}");
            }
        }

        public void SetOrientation(Vector3 direction)
        {
            skeletonScaleX = direction.x < 0 ? -1f : 1f;
            if (monsterAnimator != null)
            {
                monsterAnimator.skeleton.ScaleX = skeletonScaleX;
            }
        }
    }
}