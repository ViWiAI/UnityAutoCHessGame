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

    // ��ɫ���ݽṹ
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
        public string monsterId; // ����Ψһ ID���� "Goblin", "Dragon"��
        public GameObject prefab; // ����Ԥ�Ƽ�
        public float maxHP; // ���Ѫ��
        public float attackDamage; // ������
        public float attackRange; // ������Χ
        public float attackSpeed; // �����ٶ�
        public bool autoSearchEnabled; // �Զ��ҹֿ���
        public bool autoCounterAttackEnabled; // �Զ���������
        public List<DropItem> dropTable; // �����
    }

    [System.Serializable]
    public class PetData
    {
        public string petId; // ����Ψһ ID���� "Wolf", "Eagle"��
        public GameObject prefab; // ����Ԥ�Ƽ�
        public float maxHP; // ���Ѫ��
        public float attackDamage; // ������
        public float attackRange; // ������Χ
        public float attackSpeed; // �����ٶ�
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