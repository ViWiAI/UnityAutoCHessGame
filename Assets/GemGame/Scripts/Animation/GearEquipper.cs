using UnityEngine;
using Spine;
using Spine.Unity;
using Game.Core;
using Game.Managers;

namespace Game.Animation
{
    public class GearEquipper : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation characterSkeleton;
        [SerializeField] private PlayerHero playerHero;

        public HeroJobs Job { get; set; }
        public int Melee { get; set; }
        public int Shield { get; set; }
        public int Bow { get; set; }
        public int Quiver { get; set; }
        public int Staff { get; set; }
        public int DuelistOffhand { get; set; }
        public int Armor { get; set; }
        public int Helmet { get; set; }
        public int Shoulder { get; set; }
        public int Arm { get; set; }
        public int Feet { get; set; }
        public int Hair { get; set; }
        public int Face { get; set; }

        private void Awake()
        {
            // 确保组件存在
            if (characterSkeleton == null)
            {
                characterSkeleton = GetComponentInChildren<SkeletonAnimation>();
                if (characterSkeleton == null)
                {
                    Debug.LogError($"{name} 未找到 SkeletonAnimation 组件！");
                }
            }

            if (playerHero == null)
            {
                playerHero = GetComponent<PlayerHero>();
                if (playerHero == null)
                {
                    Debug.LogError($"{name} 未找到 PlayerHero 组件！");
                }
            }
        }

        private void Start()
        {
            ApplySkinChanges();
        }

        public void ApplySkinChanges()
        {
            if (characterSkeleton == null)
            {
                Debug.LogError("SkeletonAnimation 未配置，无法应用皮肤！");
                return;
            }

            var skeleton = characterSkeleton.Skeleton;
            var skeletonData = skeleton.Data;
            var newCustomSkin = new Skin("CustomCharacter");

            // 根据职业添加武器和副手皮肤
            switch (Job)
            {
                case HeroJobs.Warrior:
                    AddSkin(newCustomSkin, skeletonData, "MELEE", Melee);
                    AddSkin(newCustomSkin, skeletonData, "SHIELD", Shield - 1, Shield == 0);
                    break;
                case HeroJobs.Archer:
                    AddSkin(newCustomSkin, skeletonData, "BOW", Bow);
                    AddSkin(newCustomSkin, skeletonData, "QUIVER", Quiver);
                    break;
                case HeroJobs.Elementalist:
                    AddSkin(newCustomSkin, skeletonData, "STAFF", Staff);
                    break;
                case HeroJobs.Duelist:
                    AddSkin(newCustomSkin, skeletonData, "MELEE", Melee);
                    AddSkin(newCustomSkin, skeletonData, "OFFHAND", DuelistOffhand);
                    break;
            }

            // 添加通用装备皮肤
            AddSkin(newCustomSkin, skeletonData, "ARMOR", Armor);
            AddSkin(newCustomSkin, skeletonData, "HELMET", Helmet - 1, Helmet == 0);
            AddSkin(newCustomSkin, skeletonData, "SHOULDER", Shoulder);
            AddSkin(newCustomSkin, skeletonData, "ARM", Arm);
            AddSkin(newCustomSkin, skeletonData, "FEET", Feet);
            AddSkin(newCustomSkin, skeletonData, "HAIR", Hair);
            AddSkin(newCustomSkin, skeletonData, "EYES", Face);

            // 应用皮肤
            try
            {
                skeleton.SetSkin(newCustomSkin);
                skeleton.SetSlotsToSetupPose();
                Debug.Log($"已为 {name} 应用皮肤：{newCustomSkin.Name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"应用皮肤失败：{ex.Message}");
            }
        }

        private void AddSkin(Skin targetSkin, SkeletonData skeletonData, string skinPrefix, int skinId, bool isEmpty = false)
        {
            if (isEmpty)
            {
                Skin emptySkin = skeletonData.FindSkin("EMPTY");
                if (emptySkin != null)
                {
                    targetSkin.AddSkin(emptySkin);
                }
                else
                {
                    Debug.LogWarning($"未找到 EMPTY 皮肤！");
                }
                return;
            }

            string skinName = $"{skinPrefix} {skinId}";
            Skin skin = skeletonData.FindSkin(skinName);
            if (skin != null)
            {
                targetSkin.AddSkin(skin);
            }
            else
            {
                Debug.LogWarning($"未找到皮肤：{skinName}");
            }
        }
    }
}