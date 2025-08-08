using UnityEngine;
using System;

namespace Game.Combat
{
    [CreateAssetMenu(fileName = "Skill", menuName = "Game/Skill")]
    public class Skill : ScriptableObject
    {
        public string skillId; // ����ID���� "Fireball"��
        public string skillName; // ��������
        public SkillType skillType; // ��������
        public DamageType damageType; // �˺����ͣ�����/ħ����
        public float damageMultiplier = 1f; // �˺����ʣ����� attack/spellPower��
        public float healAmount = 0f; // �ظ������̶�ֵ��
        public bool isAOE; // �Ƿ�Ⱥ��Ч��
        public float aoeRadius = 1f; // AOE �뾶����������
        public GameObject effectPrefab; // ������Ч Prefab
        public string animationName = "Special"; // �ͷŶ���
        public float manaCost = 100f; // ħ������

        public enum SkillType
        {
            Damage, // �˺�����
            Heal // �ظ�����
        }

        public enum DamageType
        {
            Physical,
            Magic
        }
    }
}