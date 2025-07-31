using UnityEngine;
using System;

namespace Game.Combat
{
    [CreateAssetMenu(fileName = "Skill", menuName = "Game/Skill")]
    public class Skill : ScriptableObject
    {
        public string skillId; // 技能ID（如 "Fireball"）
        public string skillName; // 技能名称
        public SkillType skillType; // 技能类型
        public DamageType damageType; // 伤害类型（物理/魔法）
        public float damageMultiplier = 1f; // 伤害倍率（基于 attack/spellPower）
        public float healAmount = 0f; // 回复量（固定值）
        public bool isAOE; // 是否群体效果
        public float aoeRadius = 1f; // AOE 半径（格子数）
        public GameObject effectPrefab; // 技能特效 Prefab
        public string animationName = "Special"; // 释放动画
        public float manaCost = 100f; // 魔法消耗

        public enum SkillType
        {
            Damage, // 伤害技能
            Heal // 回复技能
        }

        public enum DamageType
        {
            Physical,
            Magic
        }
    }
}