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
        [SerializeField] private bool autoSearchEnabled = true; // �������Զ��ҹֿ���
        [SerializeField] private bool autoCounterAttackEnabled = true; // �������Զ���������

        protected override void Awake()
        {
            base.Awake();
            heroType = HeroType.Monster;
            if (monsterAnimator == null)
            {
                monsterAnimator = GetComponent<MonsterAnimator>();
                if (monsterAnimator == null)
                {
                    Debug.LogError($"δ�ҵ� MonsterAnimator ���: {heroName}");
                }
            }
            currentAttackMode = gameObject.AddComponent<MeleePhysicalAttack>();
            currentAttackMode.SetHero(this);   
        }

        protected override void Start()
        {
            base.Start();
            isAutoAttacking = autoSearchEnabled; // ��ʼ��ʱ�����Զ��ҹֿ�������
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
                    Debug.Log($"{heroName} �ҵ���Ŀ��: {lastTargetEnemy.name} at {lastTargetCell}");
                }
                else
                {
                    StopAttack();
                    StopMoving();
                    Debug.Log($"{heroName} ����ЧĿ�ֹ꣬ͣս��");
                    return;
                }
            }
            Debug.Log("UpdateAutoCombat 2");
            Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(transform.position);
            if (currentAttackMode.IsWithinAttackDistance(currentCell, lastTargetCell))
            {
                StopMoving();
                Attack(lastTargetEnemy, lastTargetCell, true);
                Debug.Log($"{heroName} �ڹ�����Χ�ڣ�����Ŀ��: {lastTargetEnemy.name}");
            }
            else
            {
                MoveToAttackRange(lastTargetCell, true);
                Debug.Log($"{heroName} Ŀ�� {lastTargetEnemy.name} ���ڹ�����Χ�ڣ��ƶ��� {lastTargetCell}");
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

        // �� Monster.TakeDamage ��
        public override void TakeDamage(float damage, bool isMagic = false)
        {
            if (isDead) return;
            if (Random.value < stats.dodgeRate)
            {
                Debug.Log($"{heroName} �����˹���");
                return;
            }

            float finalDamage = isMagic
                ? damage * (1f - stats.MC / (stats.MC + 100f))
                : damage * (1f - stats.AC / (stats.AC + 100f));
            if (Random.value < stats.critRate)
            {
                finalDamage *= stats.critDmg;
                Debug.Log($"{heroName} �ܵ�����");
            }
            stats.ModifyStat("curHP", -finalDamage);
            Debug.Log($"{heroName} �ܵ� {finalDamage:F2} {(isMagic ? "ħ��" : "����")}�˺���ʣ��Ѫ��: {stats.curHP:F2}");

            UpdateHealthBar();

            // �Զ������߼�
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
                    Debug.Log($"{heroName} �ܵ��������Զ�����Ŀ��: {lastTargetEnemy.name}");
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
                    // ģ���������ұ���
                    // PlayerHero.Instance.AddToInventory(drop.itemId, quantity);
                }
            }
            if (droppedItems.Count > 0)
            {
                //UIManager.Instance.ShowTreasurePrompt($"����: {string.Join(", ", droppedItems)}");
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
                Debug.LogWarning($"δ�ҵ� MonsterAnimator ���: {heroName}");
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

        // ��������ȡ����״̬�����ڲ��ԣ�
        public bool IsAutoSearchEnabled() => autoSearchEnabled;
        public bool IsAutoCounterAttackEnabled() => autoCounterAttackEnabled;

        // ���������ÿ���״̬�����ڲ��ԣ�
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
            Debug.Log($"{heroName} �Զ��ҹֿ�������Ϊ: {enabled}");
        }

        public void SetAutoCounterAttackEnabled(bool enabled)
        {
            autoCounterAttackEnabled = enabled;
            Debug.Log($"{heroName} �Զ�������������Ϊ: {enabled}");
        }
    }
}