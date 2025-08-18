using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [System.Serializable]


    public enum PlayerAnimations
    {
        Idle, Walk, Attack1, Attack2, Hurt, Death, Special, Buff, Run, FullJump, Jump1, Jump2, Jump3
    }
    public enum HeroRole
    {
        Warrior, Mage, Hunter, Rogue, Priest
    }

    // 角色数据结构
    [System.Serializable]
    public class PlayerCharacterInfo
    {
        public int CharacterId { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Role { get; set; }
        public int SkinId { get; set; }
        public int MapId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
    public class MonsterData
    {
        public string monsterId; // 怪物唯一 ID（如 "Goblin", "Dragon"）
        public GameObject prefab; // 怪物预制件
        public float maxHP; // 最大血量
        public float attackDamage; // 攻击力
        public float attackRange; // 攻击范围
        public float attackSpeed; // 攻击速度
        public bool autoSearchEnabled; // 自动找怪开关
        public bool autoCounterAttackEnabled; // 自动反击开关
        public List<DropItem> dropTable; // 掉落表
    }

    [System.Serializable]
    public class PetData
    {
        public string petId; // 宠物唯一 ID（如 "Wolf", "Eagle"）
        public GameObject prefab; // 宠物预制件
        public float maxHP; // 最大血量
        public float attackDamage; // 攻击力
        public float attackRange; // 攻击范围
        public float attackSpeed; // 攻击速度
    }

    [System.Serializable]
    public class DropItem
    {
        public string itemId;
        public float dropChance;
        public int minQuantity;
        public int maxQuantity;
    }

}