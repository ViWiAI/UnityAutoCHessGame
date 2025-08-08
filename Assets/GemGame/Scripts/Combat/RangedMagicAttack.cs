using Game.Managers;
using UnityEngine;

namespace Game.Combat
{
    public class RangedMagicAttack : AttackMode
    {
        private float attackDistance = 3f;
        private float baseCooldown = 1.8f;

        public override float GetAttackDistance() => attackDistance;
        public override float GetBaseCooldown() => baseCooldown;

        public override void PerformAttack(Vector3Int targetCell)
        {
            hero.PlayAnimation("AttackPower");
            ApplyDamage(targetCell, hero.stats.spellPower, true);
            hero.stats.ModifyStat("curMP", hero.stats.spellPower); // 普通攻击恢复魔法值
        }

        public override void PerformSkill(Vector3Int targetCell)
        {
            if (hero.stats.curMP >= hero.stats.spellPower)
            {
                hero.PlayAnimation("AttackPower");
                ApplyDamage(targetCell, hero.stats.spellPower * 2f, true);
                hero.stats.ModifyStat("mana", -hero.stats.spellPower); // 技能消耗魔法值
            }
        }

        public override void StopAttack() { }

        public override bool IsWithinAttackDistance(Vector3Int attackerCell, Vector3Int targetCell)
        {
            return GridUtility.CalculateGridDistance(attackerCell, targetCell, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()) <= attackDistance;
        }
    }
}