using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [System.Serializable]
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