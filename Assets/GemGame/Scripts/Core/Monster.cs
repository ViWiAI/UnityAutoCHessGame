using Game.Animation;
using Game.Combat;
using Game.Managers;
using Game.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    [System.Serializable]
    public class DropItem
    {
        public string itemId; // 道具ID（如"Gold"，"Sword"，"Gem"）
        public float dropChance; // 掉落概率（0-1）
        public int minQuantity; // 最小数量
        public int maxQuantity; // 最大数量
    }

    public enum ItemType
    {
        Gold,
        Armor,
        Weapon,
        Treasure,
        Gem
    }

    public class Monster : Hero
    {
        [SerializeField] private List<DropItem> dropTable = new List<DropItem>();
        [SerializeField] private MonsterAnimator monsterAnimator;

        protected override void Awake()
        {
            base.Awake();
           // monsterAnimator = monsterAnimator;
            currentAttackMode = gameObject.AddComponent<MeleePhysicalAttack>();
            currentAttackMode.SetHero(this);
            currentAttackMode.SetTilemaps(tilemap, collisionTilemap);
        }

        protected override void Update()
        {
            base.Update();
            if (isDead || !isAutoAttacking) return;
          //  UpdateAutoCombat();
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
            foreach (var enemy in BattleManager.Instance.teammates)
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

        public override void TakeDamage(float damage, bool isMagic = false)
        {
            if (Random.value < stats.dodgeRate)
            {
                Debug.Log($"{heroName} 闪避了攻击！");
                return;
            }

            float finalDamage = isMagic
                ? damage * (1f - stats.magicResist / (stats.magicResist + 100f))
                : damage * (1f - stats.armor / (stats.armor + 100f));
            if (Random.value < stats.critRate)
            {
                finalDamage *= 1.5f;
                Debug.Log($"{heroName} 受到暴击！");
            }
            stats.ModifyStat("health", -finalDamage);
            Debug.Log($"{heroName} 受到 {finalDamage} {(isMagic ? "魔法" : "物理")}伤害，剩余血量: {stats.health}");

            if (stats.health <= 0 && !isDead)
            {
                isDead = true;
                StopAttack();
                isHurtAnimationPlaying = false;
                PlayAnimation("Death");
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null) collider.enabled = false;
                DropItems();
                StartCoroutine(DestroyAfterAnimation());
            }
            else if (finalDamage > 0)
            {
                StartCoroutine(PlayHurtAnimation());
            }
        }

        private void DropItems()
        {
            List<string> droppedItems = new List<string>();
            foreach (var drop in dropTable)
            {
                if (Random.value < drop.dropChance)
                {
                    int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                    droppedItems.Add($"{drop.itemId} x{quantity}");
                    // 模拟添加至玩家背包
                  //  PlayerHero.Instance.AddToInventory(drop.itemId, quantity);
                }
            }
            if (droppedItems.Count > 0)
            {
                UIManager.Instance.ShowTreasurePrompt($"掉落: {string.Join(", ", droppedItems)}");
            }
        }

        private IEnumerator DestroyAfterAnimation()
        {
            yield return new WaitForSeconds(1f); // 等待死亡动画
            Destroy(gameObject);
        }
    }
}