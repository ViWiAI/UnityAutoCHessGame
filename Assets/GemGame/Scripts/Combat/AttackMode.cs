using Game.Core;
using Game.Managers;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Combat
{
    public abstract class AttackMode : MonoBehaviour
    {
        protected Hero hero;


        public void SetHero(Hero hero) => this.hero = hero;


        public abstract float GetAttackDistance();
        public abstract float GetBaseCooldown();
        public abstract void PerformAttack(Vector3Int targetCell);
        public abstract void PerformSkill(Vector3Int targetCell);
        public abstract void StopAttack();
        public abstract bool IsWithinAttackDistance(Vector3Int attackerCell, Vector3Int targetCell);

        public virtual float GetCooldown()
        {
            return GetBaseCooldown() / hero.stats.attackSpeed; // 攻击速度影响冷却时间
        }

        protected void ApplyDamage(Vector3Int targetCell, float damage, bool isMagic = false)
        {
            Vector3 targetPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(targetCell);
            float radius = isMagic && this is RangedMagicAttack ? 1f : 0.5f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, radius);
            foreach (Collider2D hit in hits)
            {
                Hero target = hit.GetComponent<Hero>();
                if (target != null && target != hero && !target.isDead)
                {
                    target.TakeDamage(damage, isMagic);
                }
            }
        }
    }
}