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

        [SerializeField] private List<MonsterData> monsterTemplates; // 怪物模板（Inspector 配置）
        [SerializeField] private List<PetData> petTemplates; // 宠物模板（Inspector 配置）

        private Dictionary<string, MonsterData> monsterTemplateDict; // 怪物模板字典
        private Dictionary<string, PetData> petTemplateDict; // 宠物模板字典

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

        // 初始化怪物
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
                        // 设置怪物属性
                        monster.heroName = monsterId;
                        monster.SetCurrentMapId(battleMapId);
                        monster.stats.maxHP = template.maxHP;
                        monster.stats.curHP = template.maxHP;
                        monster.stats.attackDamage = template.attackDamage;
                        monster.stats.attackRange = template.attackRange;
                        monster.stats.attackSpeed = template.attackSpeed;
                        monster.SetAutoSearchEnabled(template.autoSearchEnabled);
                        monster.SetAutoCounterAttackEnabled(template.autoCounterAttackEnabled);
                        // 设置掉落表（假设 Monster 有设置 dropTable 的方法）
                  //      monster.SetDropTable(template.dropTable);
                        monsters.Add(monster);
                        Debug.Log($"实例化怪物: {monsterId}, HP: {template.maxHP}, 自动找怪: {template.autoSearchEnabled}, 自动反击: {template.autoCounterAttackEnabled}");
                    }
                    else
                    {
                        Debug.LogWarning($"预制件 {monsterId} 不包含 Monster 组件");
                        Destroy(monsterObj);
                    }
                }
                else
                {
                    Debug.LogWarning($"未找到怪物模板: {monsterId}");
                }
            }
            return monsters;
        }

        // 初始化宠物
        public List<Hero> InitializePets(List<string> petIds, string battleMapId, PlayerHero owner)
        {
            List<Hero> pets = new List<Hero>();
            foreach (var petId in petIds)
            {
                if (petTemplateDict.TryGetValue(petId, out PetData template))
                {
                    GameObject petObj = Instantiate(template.prefab);
                    Monster pet = petObj.GetComponent<Monster>(); // 假设宠物使用 Monster 类
                    if (pet != null)
                    {
                        // 设置宠物属性
                        pet.heroName = petId;
                        pet.heroType = HeroType.Pet; // 区分宠物和怪物
                        pet.SetCurrentMapId(battleMapId);
                        pet.stats.maxHP = template.maxHP;
                        pet.stats.curHP = template.maxHP;
                        pet.stats.attackDamage = template.attackDamage;
                        pet.stats.attackRange = template.attackRange;
                        pet.stats.attackSpeed = template.attackSpeed;
                        pet.SetAutoSearchEnabled(true); // 宠物默认主动攻击
                        pet.SetAutoCounterAttackEnabled(true); // 宠物默认反击
                  //      pet.SetOwner(owner); // 设置主人（需在 Monster 类添加）
                        pets.Add(pet);
                        Debug.Log($"实例化宠物: {petId}, HP: {template.maxHP}, Owner: {owner.heroName}");
                    }
                    else
                    {
                        Debug.LogWarning($"预制件 {petId} 不包含 Monster 组件");
                        Destroy(petObj);
                    }
                }
                else
                {
                    Debug.LogWarning($"未找到宠物模板: {petId}");
                }
            }
            return pets;
        }
    }
}