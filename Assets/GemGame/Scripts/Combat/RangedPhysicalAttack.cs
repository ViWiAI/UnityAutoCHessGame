using Game.Managers;
using UnityEngine;

namespace Game.Combat
{
    public class RangedPhysicalAttack : AttackMode
    {
        private float attackDistance = 3f;
        private float baseCooldown = 1.5f;

        public override float GetAttackDistance() => attackDistance;
        public override float GetBaseCooldown() => baseCooldown;

        public override void PerformAttack(Vector3Int targetCell)
        {
            hero.PlayAnimation("Attack1");
            ApplyDamage(targetCell, hero.stats.attackDamage);
            hero.stats.ModifyStat("mana", 10f);
        }

        public override void PerformSkill(Vector3Int targetCell)
        {
            if (hero.stats.curMP >= hero.stats.maxMP)
            {
                hero.PlayAnimation("Special");
                ApplyDamage(targetCell, hero.stats.attackDamage * 2f);
                hero.stats.ModifyStat("mana", -hero.stats.maxMP);
            }
        }

        public override void StopAttack() { }

        public override bool IsWithinAttackDistance(Vector3Int attackerCell, Vector3Int targetCell)
        {
            return GridUtility.CalculateGridDistance(attackerCell, targetCell, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()) <= attackDistance;
        }
    }
}