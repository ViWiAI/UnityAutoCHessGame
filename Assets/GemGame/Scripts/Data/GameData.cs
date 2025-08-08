using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [System.Serializable]
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