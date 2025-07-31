using Game.Animation;
using Game.Combat;
using Game.Managers;
using UnityEngine;

namespace Game.Core
{
    public class Pet : Hero
    {
        protected override void Awake()
        {
            base.Awake();
            currentAttackMode = gameObject.AddComponent<RangedPhysicalAttack>();
            currentAttackMode.SetHero(this);
            currentAttackMode.SetTilemaps(tilemap, collisionTilemap);
        }

        protected override void Update()
        {
            base.Update();
            if (isDead || !isAutoAttacking) return;
            UpdateAutoCombat();
        }

        private void UpdateAutoCombat()
        {
            if (lastTargetEnemy == null || !lastTargetEnemy.activeInHierarchy || lastTargetEnemy.GetComponent<Hero>().isDead)
            {
                Hero target = FindNearestEnemy();
                if (target != null)
                {
                    lastTargetEnemy = target.gameObject;
                    lastTargetCell = tilemap.WorldToCell(target.transform.position);
                    Attack(lastTargetEnemy, lastTargetCell, true);
                }
                else
                {
                    StopAttack();
                }
            }
        }

        private Hero FindNearestEnemy()
        {
            Hero nearest = null;
            float minDistance = float.MaxValue;
            Vector3Int currentCell = tilemap.WorldToCell(transform.position);
            foreach (var enemy in BattleManager.Instance.enemies)
            {
                if (!enemy.isDead)
                {
                    Vector3Int enemyCell = tilemap.WorldToCell(enemy.transform.position);
                    float distance = GridUtility.CalculateGridDistance(currentCell, enemyCell, tilemap, collisionTilemap);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = enemy;
                    }
                }
            }
            return nearest;
        }
    }
}