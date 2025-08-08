using UnityEngine;
using UnityEngine.Tilemaps;
using Game.Core;
using Game.Managers;

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
            if (hero == null || hero.isDead) return;

            hero.PlayAnimation("Attack1");
            Vector3 targetPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(targetCell);
            Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, 0.5f);
            foreach (Collider2D hit in hits)
            {
                Hero target = hit.GetComponent<Hero>();
                if (target != null && target != hero && !target.isDead)
                {
                    target.TakeDamage(hero.stats.attackDamage, false);
                    BattleManager.Instance.RecordAttack(target, hero); // 记录攻击者
                    Debug.Log($"{hero.heroName} 对 {target.heroName} 造成 {hero.stats.attackDamage:F2} 物理伤害");
                }
            }
            hero.stats.ModifyStat("mana", 10f); // 普通攻击回复10点魔法
        }

        public override void PerformSkill(Vector3Int targetCell)
        {
            if (hero == null || hero.isDead) return;

            if (hero.stats.curMP >= hero.stats.maxMP)
            {
                hero.PlayAnimation("Special");
                Vector3 targetPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(targetCell);
                Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, 0.5f);
                foreach (Collider2D hit in hits)
                {
                    Hero target = hit.GetComponent<Hero>();
                    if (target != null && target != hero && !target.isDead)
                    {
                        target.TakeDamage(hero.stats.attackDamage * 1.5f, false);
                        BattleManager.Instance.RecordAttack(target, hero); // 记录攻击者
                        Debug.Log($"{hero.heroName} 对 {target.heroName} 使用技能，造成 {hero.stats.attackDamage * 1.5f:F2} 物理伤害");
                    }
                }
                hero.stats.ModifyStat("mana", -hero.stats.maxMP); // 技能消耗全部魔法
            }
        }

        public override void StopAttack()
        {
            // 实现停止攻击逻辑（例如取消动画）
            if (hero != null)
            {
                hero.PlayAnimation("Idle");
            }
        }

        public override bool IsWithinAttackDistance(Vector3Int attackerCell, Vector3Int targetCell)
        {
            return GridUtility.CalculateGridDistance(attackerCell, targetCell, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()) <= attackDistance;
        }
    }
}