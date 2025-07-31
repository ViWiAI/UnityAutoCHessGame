using UnityEngine;

namespace Game.Combat
{
    public class MeleePhysicalAttack : AttackMode
    {
        private float attackDistance = 1f;
        private float baseCooldown = 1f;

        public override float GetAttackDistance() => attackDistance;
        public override float GetBaseCooldown() => baseCooldown;

        public override void PerformAttack(Vector3Int targetCell)
        {
            hero.PlayAnimation("Attack1");
            ApplyDamage(targetCell, hero.stats.attackDamage);
            hero.stats.ModifyStat("mana", 10f); // 普通攻击回复10点魔法
        }

        public override void PerformSkill(Vector3Int targetCell)
        {
            if (hero.stats.mana >= hero.stats.maxMana)
            {
                hero.PlayAnimation("Special");
                ApplyDamage(targetCell, hero.stats.attackDamage * 1.5f);
                hero.stats.ModifyStat("mana", -hero.stats.maxMana); // 技能消耗全部魔法
            }
        }

        public override void StopAttack() { }

        public override bool IsWithinAttackDistance(Vector3Int attackerCell, Vector3Int targetCell)
        {
            return GridUtility.CalculateGridDistance(attackerCell, targetCell, tilemap, collisionTilemap) <= attackDistance;
        }
    }
}