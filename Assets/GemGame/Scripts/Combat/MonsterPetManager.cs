using Game.Core;
using Game.Data;
using Game.Managers;
using System.Collections.Generic;
using UnityEngine;
using static Game.Core.Hero;

namespace Game.Combat
{
    public class MonsterPetManager : MonoBehaviour
    {
        public static MonsterPetManager Instance { get; private set; }

        [SerializeField] private List<MonsterData> monsterTemplates; // ����ģ�壨Inspector ���ã�
        [SerializeField] private List<PetData> petTemplates; // ����ģ�壨Inspector ���ã�

        private Dictionary<string, MonsterData> monsterTemplateDict; // ����ģ���ֵ�
        private Dictionary<string, PetData> petTemplateDict; // ����ģ���ֵ�

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeTemplates();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeTemplates()
        {
            monsterTemplateDict = new Dictionary<string, MonsterData>();
            petTemplateDict = new Dictionary<string, PetData>();

            foreach (var template in monsterTemplates)
            {
                if (template != null && !string.IsNullOrEmpty(template.monsterId))
                {
                    monsterTemplateDict[template.monsterId] = template;
                }
            }

            foreach (var template in petTemplates)
            {
                if (template != null && !string.IsNullOrEmpty(template.petId))
                {
                    petTemplateDict[template.petId] = template;
                }
            }
        }

        // ��ʼ������
        public List<Hero> InitializeMonsters(List<string> monsterIds, string battleMapId)
        {
            List<Hero> monsters = new List<Hero>();
            foreach (var monsterId in monsterIds)
            {
                if (monsterTemplateDict.TryGetValue(monsterId, out MonsterData template))
                {
                    GameObject monsterObj = Instantiate(template.prefab);
                    Monster monster = monsterObj.GetComponent<Monster>();
                    if (monster != null)
                    {
                        // ���ù�������
                        monster.heroName = monsterId;
                        monster.SetCurrentMapId(battleMapId);
                        monster.stats.maxHP = template.maxHP;
                        monster.stats.curHP = template.maxHP;
                        monster.stats.attackDamage = template.attackDamage;
                        monster.stats.attackRange = template.attackRange;
                        monster.stats.attackSpeed = template.attackSpeed;
                        monster.SetAutoSearchEnabled(template.autoSearchEnabled);
                        monster.SetAutoCounterAttackEnabled(template.autoCounterAttackEnabled);
                        // ���õ�������� Monster ������ dropTable �ķ�����
                  //      monster.SetDropTable(template.dropTable);
                        monsters.Add(monster);
                        Debug.Log($"ʵ��������: {monsterId}, HP: {template.maxHP}, �Զ��ҹ�: {template.autoSearchEnabled}, �Զ�����: {template.autoCounterAttackEnabled}");
                    }
                    else
                    {
                        Debug.LogWarning($"Ԥ�Ƽ� {monsterId} ������ Monster ���");
                        Destroy(monsterObj);
                    }
                }
                else
                {
                    Debug.LogWarning($"δ�ҵ�����ģ��: {monsterId}");
                }
            }
            return monsters;
        }

        // ��ʼ������
        public List<Hero> InitializePets(List<string> petIds, string battleMapId, PlayerHero owner)
        {
            List<Hero> pets = new List<Hero>();
            foreach (var petId in petIds)
            {
                if (petTemplateDict.TryGetValue(petId, out PetData template))
                {
                    GameObject petObj = Instantiate(template.prefab);
                    Monster pet = petObj.GetComponent<Monster>(); // �������ʹ�� Monster ��
                    if (pet != null)
                    {
                        // ���ó�������
                        pet.heroName = petId;
                        pet.heroType = HeroType.Pet; // ���ֳ���͹���
                        pet.SetCurrentMapId(battleMapId);
                        pet.stats.maxHP = template.maxHP;
                        pet.stats.curHP = template.maxHP;
                        pet.stats.attackDamage = template.attackDamage;
                        pet.stats.attackRange = template.attackRange;
                        pet.stats.attackSpeed = template.attackSpeed;
                        pet.SetAutoSearchEnabled(true); // ����Ĭ����������
                        pet.SetAutoCounterAttackEnabled(true); // ����Ĭ�Ϸ���
                  //      pet.SetOwner(owner); // �������ˣ����� Monster ����ӣ�
                        pets.Add(pet);
                        Debug.Log($"ʵ��������: {petId}, HP: {template.maxHP}, Owner: {owner.heroName}");
                    }
                    else
                    {
                        Debug.LogWarning($"Ԥ�Ƽ� {petId} ������ Monster ���");
                        Destroy(petObj);
                    }
                }
                else
                {
                    Debug.LogWarning($"δ�ҵ�����ģ��: {petId}");
                }
            }
            return pets;
        }
    }
}