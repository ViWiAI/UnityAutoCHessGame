using Game.Combat;
using Game.Core;
using Game.Managers;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Managers
{
    public class SkillManager : MonoBehaviour
    {
        public static SkillManager Instance { get; private set; }
        [SerializeField] private List<Skill> skills = new List<Skill>(); // 技能列表
        private Dictionary<string, Skill> skillDictionary = new Dictionary<string, Skill>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            WebSocketManager.Instance.OnMessageReceived += HandleServerMessage;
        }

        private void HandleServerMessage(Dictionary<string, object> data)
        {
            if (data["type"].ToString() == "skill_result")
            {
                bool success = bool.Parse(data["success"].ToString());
                if (!success)
                {
                    Debug.LogWarning("技能释放被服务器拒绝！");
                }
            }
        }

        private void NotifyServer(Hero caster, Skill skill, Vector3Int targetCell)
        {
            WebSocketManager.Instance.Send(new Dictionary<string, object>
            {
                { "type", "skill" },
                { "player_id", caster is PlayerHero ? "Player_001" : null },
                { "skill_id", skill.skillId },
                { "map_id", caster.GetCurrentMapId() },
                { "target_cell", new { x = targetCell.x, y = targetCell.y, z = targetCell.z } }
            });
        }

        public void AddSkill(Skill skill)
        {
            if (!skillDictionary.ContainsKey(skill.skillId))
            {
                skills.Add(skill);
                skillDictionary.Add(skill.skillId, skill);
            }
        }

        
        public void CastSkill(Hero caster, string skillId, Vector3Int targetCell, string mapId)
        {
            if (!skillDictionary.TryGetValue(skillId, out Skill skill))
            {
                Debug.LogError($"技能 {skillId} 未找到！");
                return;
            }

            if (caster.stats.curMP < skill.manaCost)
            {
                Debug.LogWarning($"{caster.heroName} 魔法不足，无法释放 {skill.skillName}");
                return;
            }

            // 播放释放动画
            caster.PlayAnimation(skill.animationName);

            // 播放特效
            if (skill.effectPrefab != null)
            {
                Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(targetCell);
                GameObject effectObj = Instantiate(skill.effectPrefab, worldPos, Quaternion.identity);
                effectObj.GetComponent<SkillEffect>().Initialize(worldPos, () =>
                {
                    ApplySkillEffect(caster, skill, targetCell);
                });
            }
            else
            {
                ApplySkillEffect(caster, skill, targetCell);
            }

            caster.stats.ModifyStat("mana", -skill.manaCost);
        }

        private void ApplySkillEffect(Hero caster, Skill skill, Vector3Int targetCell)
        {
            Tilemap tilemap = MapManager.Instance.GetTilemap();
            Tilemap collisionTilemap = MapManager.Instance.GetCollisionTilemap();
            List<Vector3Int> targetCells = skill.isAOE
                ? GridUtility.GetCellsInRange(targetCell, skill.aoeRadius, tilemap, collisionTilemap, false)
                : new List<Vector3Int> { targetCell };

            foreach (var cell in targetCells)
            {
                Hero target = FindHeroAtCell(cell);
                if (target != null && !target.isDead)
                {
                    switch (skill.skillType)
                    {
                        case Skill.SkillType.Damage:
                            float damage = skill.damageType == Skill.DamageType.Physical
                                ? caster.stats.attackDamage * skill.damageMultiplier
                                : caster.stats.spellPower * skill.damageMultiplier;
                            target.TakeDamage(damage, skill.damageType == Skill.DamageType.Magic);
                            break;
                        case Skill.SkillType.Heal:
                            float healAmount = skill.healAmount;
                            target.stats.ModifyStat("health", healAmount);
                            Debug.Log($"{caster.heroName} 治疗 {target.heroName} {healAmount} 生命");
                            break;
                    }
                }
            }

            NotifyServer(caster, skill, targetCell);
        }

        private Hero FindHeroAtCell(Vector3Int cell)
        {
            foreach (var unit in BattleManager.Instance.teammates.Concat(BattleManager.Instance.enemies))
            {
                if (unit != null && !unit.isDead)
                {
                    Vector3Int unitCell = MapManager.Instance.GetTilemap().WorldToCell(unit.transform.position);
                    if (unitCell == cell)
                    {
                        return unit;
                    }
                }
            }
            return null;
        }
    }
}