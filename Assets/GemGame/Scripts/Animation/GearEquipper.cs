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
            // ȷ���������
            if (characterSkeleton == null)
            {
                characterSkeleton = GetComponentInChildren<SkeletonAnimation>();
                if (characterSkeleton == null)
                {
                    Debug.LogError($"{name} δ�ҵ� SkeletonAnimation �����");
                }
            }

            if (playerHero == null)
            {
                playerHero = GetComponent<PlayerHero>();
                if (playerHero == null)
                {
                    Debug.LogError($"{name} δ�ҵ� PlayerHero �����");
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
                Debug.LogError("SkeletonAnimation δ���ã��޷�Ӧ��Ƥ����");
                return;
            }

            var skeleton = characterSkeleton.Skeleton;
            var skeletonData = skeleton.Data;
            var newCustomSkin = new Skin("CustomCharacter");

            // ����ְҵ��������͸���Ƥ��
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

            // ���ͨ��װ��Ƥ��
            AddSkin(newCustomSkin, skeletonData, "ARMOR", Armor);
            AddSkin(newCustomSkin, skeletonData, "HELMET", Helmet - 1, Helmet == 0);
            AddSkin(newCustomSkin, skeletonData, "SHOULDER", Shoulder);
            AddSkin(newCustomSkin, skeletonData, "ARM", Arm);
            AddSkin(newCustomSkin, skeletonData, "FEET", Feet);
            AddSkin(newCustomSkin, skeletonData, "HAIR", Hair);
            AddSkin(newCustomSkin, skeletonData, "EYES", Face);

            // Ӧ��Ƥ��
            try
            {
                skeleton.SetSkin(newCustomSkin);
                skeleton.SetSlotsToSetupPose();
                Debug.Log($"��Ϊ {name} Ӧ��Ƥ����{newCustomSkin.Name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ӧ��Ƥ��ʧ�ܣ�{ex.Message}");
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
                    Debug.LogWarning($"δ�ҵ� EMPTY Ƥ����");
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
                Debug.LogWarning($"δ�ҵ�Ƥ����{skinName}");
            }
        }
    }
}