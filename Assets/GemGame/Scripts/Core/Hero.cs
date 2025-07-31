using Game.Animation;
using Game.Combat;
using Game.Managers;
using Game.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static Game.Managers.MapManager;

namespace Game.Core
{
    public interface IAnimatable
    {
        void PlayAnimation(string animationName);
    }

    public class Hero : MonoBehaviour, IAnimatable
    {
        [SerializeField] protected Tilemap tilemap;
        [SerializeField] protected Tilemap collisionTilemap;
        [SerializeField] private float spriteHeightOffset = -0.2f;
        [SerializeField] private GameObject heroBarPrefab; // Slider_HealthBar_L1 预制件
        [SerializeField] private Vector3 healthBarOffset = new Vector3(-0.1f, 1.45f, 0f); // 头顶偏移量
        [SerializeField] private Canvas uiCanvas; // 在 Inspector 中分配 Canvas
        [SerializeField] private HeroType heroType; // 英雄类型

        protected Slider healthBarSlider; // 血条 Slider（由 UIManager 设置）
        protected Slider energeBarSlider; // 魔法条 Slider（由 UIManager 设置）
        protected GameObject heroBarInstance; // 血条实例（由 UIManager 设置）

        protected string currentMapId;
        public AttackMode currentAttackMode;
        public List<Equipment> equipment = new List<Equipment>();
        public Stats stats;
        public string heroName;
        public bool isDead;
        public bool isMoving;
        public bool isAttacking;
        protected bool isHurtAnimationPlaying;
        public bool isAutoAttacking;
        protected Vector3 targetPosition;
        protected List<Vector3Int> path = new List<Vector3Int>();
        protected int pathIndex;
        protected GameObject lastTargetEnemy;
        protected Vector3Int lastTargetCell;

        protected List<Equipment> equipmentList = new List<Equipment>();
        private int maxEquipment = 6;

        public enum HeroType
        {
            PlayerHero,    // 玩家
            PlayerPet,  // 玩家宠物
            Monster,      // 怪物
        }

        protected readonly Vector3[] hexDirections = new Vector3[]
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(0.5f, 1.154f, 0f),
            new Vector3(-0.5f, 1.154f, 0f),
            new Vector3(0.5f, -1.154f, 0f),
            new Vector3(-0.5f, -1.154f, 0f)
        };

        protected class Node
        {
            public Vector3Int cellPos;
            public float gCost;
            public float hCost;
            public float fCost => gCost + hCost;
            public Node parent;

            public Node(Vector3Int cellPos, float gCost, float hCost, Node parent)
            {
                this.cellPos = cellPos;
                this.gCost = gCost;
                this.hCost = hCost;
                this.parent = parent;
            }
        }

        [System.Serializable]
        public class Stats
        {
            public float health;
            public float maxHealth;
            public float mana;
            public float maxMana;
            public float attackDamage;
            public float armor;
            public float attackSpeed = 1f;
            public float spellPower = 10f;
            public float magicResist = 5f;
            public float manaRegen = 2f;
            public float critRate = 0.1f;
            public float dodgeRate = 0.05f;
            public float moveSpeed = 2f;

            public Stats(float health = 100f, float maxHealth = 100f, float mana = 0f, float maxMana = 100f, float attackDamage = 10f, float armor = 5f, float attackSpeed = 1f, float spellPower = 10f, float magicResist = 5f, float manaRegen = 2f, float critRate = 0.1f, float dodgeRate = 0.05f, float moveSpeed = 2f)
            {
                this.health = health;
                this.maxHealth = maxHealth;
                this.mana = mana;
                this.maxMana = maxMana;
                this.attackDamage = attackDamage;
                this.armor = armor;
                this.attackSpeed = attackSpeed;
                this.spellPower = spellPower;
                this.magicResist = magicResist;
                this.manaRegen = manaRegen;
                this.critRate = critRate;
                this.dodgeRate = dodgeRate;
                this.moveSpeed = moveSpeed;
            }

            public void ModifyStat(string statName, float value)
            {
                switch (statName.ToLower())
                {
                    case "health": health = Mathf.Clamp(health + value, 0f, maxHealth); break;
                    case "maxhealth": maxHealth += value; break;
                    case "mana": mana = Mathf.Clamp(mana + value, 0f, maxMana); break;
                    case "maxmana": maxMana += value; break;
                    case "attackdamage": attackDamage += value; break;
                    case "armor": armor += value; break;
                    case "attackspeed": attackSpeed += value; break;
                    case "spellpower": spellPower += value; break;
                    case "magicresist": magicResist += value; break;
                    case "manaregen": manaRegen += value; break;
                    case "critrate": critRate += value; break;
                    case "dodgerate": dodgeRate += value; break;
                    case "movespeed": moveSpeed += value; break;
                }
            }
        }

        protected virtual void Awake()
        {
            stats = new Stats();
            currentAttackMode = gameObject.AddComponent<MeleePhysicalAttack>();
            currentAttackMode.SetHero(this);
            currentAttackMode.SetTilemaps(tilemap, collisionTilemap);
        }

        protected virtual void Start()
        {
            Debug.Log($"Hero name:{gameObject.name}");
            if (uiCanvas == null)
            {
                Debug.LogError($"未在 {gameObject.name} 的 Inspector 中分配 Canvas！");
                return;
            }
            if (heroBarPrefab == null)
            {
                Debug.LogError($"未分配 heroBarPrefab！请在 {gameObject.name} 的 Inspector 中设置 Slider_HealthBar_L1 预制件。");
                return;
            }
            // 实例化血条
            heroBarInstance = Instantiate(heroBarPrefab, uiCanvas.transform);
            InitializeHealthBar();
            InitializeEnergeBar();
            StartCoroutine(ManaRegeneration());
        }

        protected virtual void Update()
        {
            if (isDead) return;

            // 更新血条位置（头顶）
            if (heroBarInstance != null)
            {
                Vector3 worldPos = transform.position + healthBarOffset;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                heroBarInstance.transform.position = new Vector3(screenPos.x, screenPos.y, 0f);
                UpdateHealthBar();
                UpdateEnergeBar();
            }

            if (isMoving)
            {
                Vector3 smoothedPosition = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    stats.moveSpeed * Time.deltaTime
                );
                transform.position = smoothedPosition;

                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    transform.position = targetPosition;
                    if (path != null && pathIndex < path.Count)
                    {
                        targetPosition = ApplyPositionOffset(tilemap.GetCellCenterWorld(path[pathIndex]));
                        UpdateOrientation();
                        pathIndex++;
                    }
                    else
                    {
                        isMoving = false;
                        path.Clear();
                        pathIndex = 0;
                        if (!isHurtAnimationPlaying) PlayAnimation("Idle");
                    }
                }
            }
        }

        protected virtual void FixedUpdate()
        {

        }

        protected void InitializeHealthBar()
        {
            if (heroBarInstance == null)
            {
                Debug.LogError("heroBarInstance 未初始化！");
                return;
            }

            Transform hpTransform = heroBarInstance.transform.Find("Slider_HP");
            if (hpTransform == null)
            {
                Debug.LogError($"未找到 Slider_HP！请检查 {heroBarPrefab.name} 预制件的层级结构。");
                return;
            }
            healthBarSlider = hpTransform.GetComponent<Slider>();
            if (healthBarSlider == null)
            {
                Debug.LogError("Slider_HP 上缺少 Slider 组件！");
                return;
            }
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = stats.maxHealth;
            UpdateHealthBar();
        }

        protected void InitializeEnergeBar()
        {
            if (heroBarInstance == null)
            {
                Debug.LogError("heroBarInstance 未初始化！");
                return;
            }

            Transform mpTransform = heroBarInstance.transform.Find("Slider_MP");
            if (mpTransform == null)
            {
                Debug.LogError($"未找到 Slider_MP！请检查 {heroBarPrefab.name} 预制件的层级结构。");
                return;
            }
            energeBarSlider = mpTransform.GetComponent<Slider>();
            if (energeBarSlider == null)
            {
                Debug.LogError("Slider_MP 上缺少 Slider 组件！");
                return;
            }
            energeBarSlider.minValue = 0f;
            energeBarSlider.maxValue = stats.maxMana;
            UpdateEnergeBar();
        }

        protected void UpdateHealthBar()
        {
            if (healthBarSlider != null)
            {
                healthBarSlider.value = stats.health;
            }
            if (heroBarInstance != null)
            {
                heroBarInstance.SetActive(!isDead); // 死亡时隐藏血条
            }
        }

        protected void UpdateEnergeBar()
        {
            if (energeBarSlider != null)
            {
                energeBarSlider.value = stats.mana;
            }
        }

        public void UseMana(float amount)
        {
            stats.mana = Mathf.Max(0, stats.mana - amount);
            UpdateEnergeBar();
        }

        private IEnumerator ManaRegeneration()
        {
            while (!isDead)
            {
                stats.ModifyStat("mana", stats.manaRegen * Time.deltaTime);
                UpdateEnergeBar();
                yield return null;
            }
        }

        public HeroType GetHeroType() => heroType;

        public virtual void TakeDamage(float damage, bool isMagic = false)
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

            UpdateHealthBar();

            if (stats.health <= 0 && !isDead)
            {
                isDead = true;
                StopAttack();
                isHurtAnimationPlaying = false;
                PlayAnimation("Death");
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null) collider.enabled = false;
                UpdateHealthBar(); // 隐藏血条
                if (this is PlayerHero)
                {
                //    UIManager.Instance.ShowDeathDialog((this as PlayerHero).OnRespawn);
                }
            }
            else if (finalDamage > 0)
            {
                StartCoroutine(PlayHurtAnimation());
            }
        }

        public virtual void OnRespawn()
        {
            stats.health = stats.maxHealth;
            stats.mana = stats.maxMana;
            isDead = false;
            PlayAnimation("Idle");
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = true;
            UpdateHealthBar();
            UpdateEnergeBar();
        }

        protected virtual Vector3 ApplyPositionOffset(Vector3 position)
        {
            return position + new Vector3(0, spriteHeightOffset, 0);
        }

        public virtual void SetTilemap(Tilemap tilemap, Tilemap collisionTilemap)
        {
            this.tilemap = tilemap;
            this.collisionTilemap = collisionTilemap;
            currentAttackMode?.SetTilemaps(tilemap, collisionTilemap);
        }

        protected virtual Tilemap GetCurrentTilemap()
        {
            var mapManager = MapManager.GetMapManager(currentMapId);
            Tilemap tilemap = mapManager.GetTilemap();
            return tilemap;
        }

        public string GetCurrentMapId()
        {
            return currentMapId;
        }

        public void SetCurrentMapId(string mapId)
        {
            currentMapId = mapId;
            var mapManager = MapManager.GetMapManager(mapId);
            tilemap = mapManager.GetTilemap();
            collisionTilemap = mapManager.GetCollisionTilemap();
            if (currentAttackMode != null)
            {
                currentAttackMode.SetTilemaps(tilemap, collisionTilemap);
            }
        }

        public virtual void PlayAnimation(string animationName)
        {
            ChangeAnimation(animationName);
        }

        protected virtual void ChangeAnimation(string animationName)
        {
            if (heroType == HeroType.PlayerHero && GetComponent<CharacterAnimator>() != null)
            {
                GetComponent<CharacterAnimator>().ChangeAnimation(animationName);
            }
            else if (heroType == HeroType.Monster && GetComponent<MonsterAnimator>() != null)
            {
                GetComponent<MonsterAnimator>().ChangeAnimation(animationName);
            }
            else if (heroType == HeroType.PlayerPet && GetComponent<PetAnimator>() != null)
            {
                GetComponent<PetAnimator>().ChangeAnimation(animationName);
            }
            else
            {
                Debug.LogWarning($"在 {gameObject.name} 上未找到与 HeroType {heroType} 对应的有效动画组件");
            }
        }

        public void SetAttackMode(AttackMode newMode)
        {
            if (currentAttackMode != null)
            {
                Destroy(currentAttackMode);
            }
            currentAttackMode = newMode;
            currentAttackMode.SetHero(this);
            currentAttackMode.SetTilemaps(tilemap, collisionTilemap);
        }

        public void Attack(GameObject target, Vector3Int targetCell, bool isAuto = false)
        {
            if (isDead || isHurtAnimationPlaying) return;
            isAttacking = true;
            lastTargetEnemy = target;
            lastTargetCell = targetCell;

            Hero targetHero = target.GetComponent<Hero>();
            if (targetHero == null || targetHero.isDead)
            {
                StopAttack();
                return;
            }

            Vector3Int attackerCell = tilemap.WorldToCell(transform.position);
            if (currentAttackMode.IsWithinAttackDistance(attackerCell, targetCell))
            {
                if (stats.mana >= stats.maxMana && !(this is PlayerHero))
                {
                    currentAttackMode.PerformSkill(targetCell);
                }
                else
                {
                    currentAttackMode.PerformAttack(targetCell);
                }
            }
            else if (isAuto)
            {
                MoveToAttackRange(targetCell, true);
            }
        }

        protected virtual IEnumerator PlayHurtAnimation()
        {
            isHurtAnimationPlaying = true;
            PlayAnimation("Hurt");
            yield return new WaitForSeconds(0.5f);
            isHurtAnimationPlaying = false;
            if (!isDead && !isAttacking) PlayAnimation("Idle");
        }

        public void MoveToAttackRange(Vector3Int targetCell, bool isAuto)
        {
            List<Vector3Int> path = Pathfinding.FindPathToAttackRange(
                tilemap.WorldToCell(transform.position),
                targetCell,
                currentAttackMode.GetAttackDistance(),
                tilemap,
                collisionTilemap
            );
            if (path != null && path.Count > 0)
            {
                this.path = path;
                pathIndex = 0;
                isMoving = true;
                isAutoAttacking = isAuto;
                targetPosition = ApplyPositionOffset(tilemap.GetCellCenterWorld(path[pathIndex]));
                UpdateOrientation();
                PlayAnimation("Walk");
            }
        }

        public virtual void MoveTo(Vector3Int cellPos)
        {
            if (tilemap == null || collisionTilemap == null)
            {
                Debug.LogError("Tilemap 或 collisionTilemap 未设置");
                return;
            }

            if (tilemap.HasTile(cellPos) && !GridUtility.HasObstacle(cellPos, tilemap, collisionTilemap))
            {
                List<Vector3Int> path = Pathfinding.FindPathToAttackRange(
                    tilemap.WorldToCell(transform.position),
                    cellPos,
                    0f,
                    tilemap,
                    collisionTilemap
                );
                if (path != null && path.Count > 0)
                {
                    this.path = path;
                    pathIndex = 0;
                    isMoving = true;
                    targetPosition = ApplyPositionOffset(tilemap.GetCellCenterWorld(path[pathIndex]));
                    UpdateOrientation();
                    PlayAnimation("Walk");
                    Debug.Log($"移动到 {cellPos}，路径节点数: {path.Count}");
                }
                else
                {
                    Debug.Log($"未找到到 {cellPos} 的有效路径");
                //    UIManager.Instance.ShowMessage("无法到达目标位置！");
                }
            }
            else
            {
                Debug.Log($"目标格子 {cellPos} 无效或有障碍物");
             //   UIManager.Instance.ShowMessage("无法移动到此位置！");
            }
        }

        public virtual void UpdateOrientation()
        {
            Vector3 direction = targetPosition - transform.position;
            Debug.Log($"UpdateOrientation {direction} ");
            if (heroType == HeroType.PlayerHero && GetComponent<PlayerAnimator>() != null)
            {
                Debug.Log($"UpdateOrientation PlayerHero ");
                PlayerAnimator animator = GetComponent<PlayerAnimator>();
                animator.SetOrientation(direction);
            }
        }

        public void StopAttack()
        {
            isAttacking = false;
            isAutoAttacking = false;
            lastTargetEnemy = null;
            lastTargetCell = Vector3Int.zero;
            StopAllCoroutines();
            if (!isDead && !isHurtAnimationPlaying) PlayAnimation("Idle");
        }

        public void Equip(Equipment equipment)
        {
            if (equipmentList.Count < maxEquipment)
            {
                equipmentList.Add(equipment);
                foreach (var bonus in equipment.statBonuses)
                {
                    stats.ModifyStat(bonus.Key, bonus.Value);
                }
              //  GearsManager.Instance.ApplySkinChanges();
            }
        }

    }
}