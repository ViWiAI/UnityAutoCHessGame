using Game.Managers;
using UnityEngine;

namespace Game.Combat
{
    public class MeleeMagicAttack : AttackMode
    {
        private float attackDistance = 1f;
        private float baseCooldown = 1.2f;

        public override float GetAttackDistance() => attackDistance;
        public override float GetBaseCooldown() => baseCooldown;

        public override void PerformAttack(Vector3Int targetCell)
        {
            hero.PlayAnimation("Attack1");
            ApplyDamage(targetCell, hero.stats.attackDamage, true);
            hero.stats.ModifyStat("mana", 10); // 普通攻击恢复mana
        }

        public override void PerformSkill(Vector3Int targetCell)
        {
            if (hero.stats.curMP >= hero.stats.attackDamage)
            {
                hero.PlayAnimation("Special");
                ApplyDamage(targetCell, hero.stats.attackDamage * 1.5f, true);
                hero.stats.ModifyStat("mana", -hero.stats.maxMP); // 技能消耗mana
            }
        }

        public override void StopAttack() { }

        public override bool IsWithinAttackDistance(Vector3Int attackerCell, Vector3Int targetCell)
        {
            return GridUtility.CalculateGridDistance(attackerCell, targetCell, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()) <= attackDistance;
        }
    }
}