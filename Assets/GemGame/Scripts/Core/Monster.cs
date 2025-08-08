using Game.Animation;
using Game.Combat;
using Game.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Core
{
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
        [SerializeField] private float maxSearchRange = 10f;
        [SerializeField] private float combatCheckInterval = 0.5f;
        [SerializeField] private bool autoSearchEnabled = true; // 新增：自动找怪开关
        [SerializeField] private bool autoCounterAttackEnabled = true; // 新增：自动反击开关

        protected override void Awake()
        {
            base.Awake();
            heroType = HeroType.Monster;
            if (monsterAnimator == null)
            {
                monsterAnimator = GetComponent<MonsterAnimator>();
                if (monsterAnimator == null)
                {
                    Debug.LogError($"未找到 MonsterAnimator 组件: {heroName}");
                }
            }
            currentAttackMode = gameObject.AddComponent<MeleePhysicalAttack>();
            currentAttackMode.SetHero(this);   
        }

        protected override void Start()
        {
            base.Start();
            isAutoAttacking = autoSearchEnabled; // 初始化时根据自动找怪开关设置
            if (autoSearchEnabled)
            {
                StartCoroutine(AutoCombatCoroutine());
            }
        }

        protected override void Update()
        {
            base.Update();
        }

        private IEnumerator AutoCombatCoroutine()
        {
            while (!isDead)
            {
                if (isAutoAttacking && autoSearchEnabled)
                {
                    UpdateAutoCombat();
                }
                yield return new WaitForSeconds(combatCheckInterval);
            }
        }

        private void UpdateAutoCombat()
        {
            if (isDead || !isAutoAttacking || isHurtAnimationPlaying)
            {
                return;
            }
            Debug.Log("UpdateAutoCombat");
            if (lastTargetEnemy == null || !lastTargetEnemy.activeInHierarchy || lastTargetEnemy.GetComponent<Hero>().isDead)
            {
                Hero target = FindNearestEnemy();
                if (target != null)
                {
                    lastTargetEnemy = target.gameObject;
                    lastTargetCell = MapManager.Instance.GetTilemap().WorldToCell(target.transform.position);
                    Debug.Log($"{heroName} 找到新目标: {lastTargetEnemy.name} at {lastTargetCell}");
                }
                else
                {
                    StopAttack();
                    StopMoving();
                    Debug.Log($"{heroName} 无有效目标，停止战斗");
                    return;
                }
            }
            Debug.Log("UpdateAutoCombat 2");
            Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(transform.position);
            if (currentAttackMode.IsWithinAttackDistance(currentCell, lastTargetCell))
            {
                StopMoving();
                Attack(lastTargetEnemy, lastTargetCell, true);
                Debug.Log($"{heroName} 在攻击范围内，攻击目标: {lastTargetEnemy.name}");
            }
            else
            {
                MoveToAttackRange(lastTargetCell, true);
                Debug.Log($"{heroName} 目标 {lastTargetEnemy.name} 不在攻击范围内，移动到 {lastTargetCell}");
            }
        }

        private Hero FindNearestEnemy()
        {
            Hero nearest = null;
            float minDistance = maxSearchRange;
            Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(transform.position);
            Debug.Log($"FindNearestEnemy currentCell:{currentCell}");
            foreach (var enemy in BattleManager.Instance.teammates)
            {
                if (enemy != null && !enemy.isDead && enemy.gameObject.activeInHierarchy)
                {
                    Vector3Int enemyCell = MapManager.Instance.GetTilemap().WorldToCell(enemy.transform.position);
                    float distance = GridUtility.CalculateGridDistance(currentCell, enemyCell, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap());
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = enemy;
                    }
                }
            }

            return nearest;
        }

        // 在 Monster.TakeDamage 中
        public override void TakeDamage(float damage, bool isMagic = false)
        {
            if (isDead) return;
            if (Random.value < stats.dodgeRate)
            {
                Debug.Log($"{heroName} 闪避了攻击");
                return;
            }

            float finalDamage = isMagic
                ? damage * (1f - stats.MC / (stats.MC + 100f))
                : damage * (1f - stats.AC / (stats.AC + 100f));
            if (Random.value < stats.critRate)
            {
                finalDamage *= stats.critDmg;
                Debug.Log($"{heroName} 受到暴击");
            }
            stats.ModifyStat("curHP", -finalDamage);
            Debug.Log($"{heroName} 受到 {finalDamage:F2} {(isMagic ? "魔法" : "物理")}伤害，剩余血量: {stats.curHP:F2}");

            UpdateHealthBar();

            // 自动反击逻辑
            if (autoCounterAttackEnabled && !isDead && finalDamage > 0)
            {
                Hero attacker = BattleManager.Instance.GetLastAttacker(this);
                if (attacker != null && !attacker.isDead && attacker.gameObject.activeInHierarchy)
                {
                    lastTargetEnemy = attacker.gameObject;
                    lastTargetCell = MapManager.Instance.GetTilemap().WorldToCell(attacker.transform.position);
                    isAutoAttacking = true;
                    if (!autoSearchEnabled)
                    {
                        StartCoroutine(AutoCombatCoroutine());
                    }
                    Debug.Log($"{heroName} 受到攻击，自动反击目标: {lastTargetEnemy.name}");
                }
            }

            if (stats.curHP <= 0 && !isDead)
            {
                isDead = true;
                StopAttack();
                StopMoving();
                isHurtAnimationPlaying = false;
                PlayAnimation("Death");
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null) collider.enabled = false;
                UpdateHealthBar();
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
                    // PlayerHero.Instance.AddToInventory(drop.itemId, quantity);
                }
            }
            if (droppedItems.Count > 0)
            {
                //UIManager.Instance.ShowTreasurePrompt($"掉落: {string.Join(", ", droppedItems)}");
            }
        }

        private IEnumerator DestroyAfterAnimation()
        {
            yield return new WaitForSeconds(1f);
            Destroy(gameObject);
        }

        public override void MoveTo(Vector3Int cellPos)
        {
            base.MoveTo(cellPos);
        }

        public override void PlayAnimation(string animationName, HeroJobs job = HeroJobs.Warrior)
        {
            if (monsterAnimator != null)
            {
                monsterAnimator.ChangeAnimation(animationName);
            }
            else
            {
                Debug.LogWarning($"未找到 MonsterAnimator 组件: {heroName}");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (MapManager.Instance.GetTilemap() != null)
            {
                Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(transform.position);
                ClearPathCacheForCell(currentCell);
            }
        }

        // 新增：获取开关状态（用于测试）
        public bool IsAutoSearchEnabled() => autoSearchEnabled;
        public bool IsAutoCounterAttackEnabled() => autoCounterAttackEnabled;

        // 新增：设置开关状态（用于测试）
        public void SetAutoSearchEnabled(bool enabled)
        {
            autoSearchEnabled = enabled;
            isAutoAttacking = enabled;
            if (enabled && !isDead)
            {
                StartCoroutine(AutoCombatCoroutine());
            }
            else
            {
                StopAttack();
                StopMoving();
            }
            Debug.Log($"{heroName} 自动找怪开关设置为: {enabled}");
        }

        public void SetAutoCounterAttackEnabled(bool enabled)
        {
            autoCounterAttackEnabled = enabled;
            Debug.Log($"{heroName} 自动反击开关设置为: {enabled}");
        }
    }
}