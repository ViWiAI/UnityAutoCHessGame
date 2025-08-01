using Game.Animation;
using Game.Combat;
using Game.Managers;
using Game.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Game.Core
{
    public interface IAnimatable
    {
        void PlayAnimation(string animationName, HeroJobs job = HeroJobs.Warrior);
    }

    public abstract class Hero : MonoBehaviour, IAnimatable
    {
        [SerializeField] private float spriteHeightOffset = -0.2f;
        [SerializeField] private GameObject heroBarPrefab; // Slider_HealthBar_L1 预制件
        [SerializeField] private Vector3 healthBarOffset = new Vector3(-0.1f, 1.45f, 0f);
        [SerializeField] private Canvas uiCanvas; // 在 Inspector 中分配 Canvas
        [SerializeField] private HeroType heroType; // 英雄类型
        protected Tilemap tilemap; // 主地图
        protected Tilemap collisionTilemap; // 碰撞地图

        protected Slider healthBarSlider; // 血条 Slider
        protected Slider energeBarSlider; // 魔法条 Slider
        protected GameObject heroBarInstance; // 血条实例
        protected string currentMapId;
        protected AttackMode currentAttackMode;
        protected List<Equipment> equipment = new List<Equipment>();
        public Stats stats;
        public string heroName;
        public bool isDead;
        protected bool isMoving;
        protected bool isAttacking;
        protected bool isHurtAnimationPlaying;
        protected bool isAutoAttacking;
        protected Vector3 targetPosition;
        protected List<Vector3Int> path = new List<Vector3Int>();
        protected int pathIndex;
        protected GameObject lastTargetEnemy;
        protected Vector3Int lastTargetCell;
        protected readonly Vector3[] hexDirections = new Vector3[]
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(0.5f, 1.154f, 0f),
            new Vector3(-0.5f, 1.154f, 0f),
            new Vector3(0.5f, -1.154f, 0f),
            new Vector3(-0.5f, -1.154f, 0f)
        };

        private readonly int maxEquipment = 6;
        private static readonly Dictionary<string, List<Vector3Int>> pathCache = new Dictionary<string, List<Vector3Int>>(); // 路径缓存

        public enum HeroType
        {
            PlayerHero,    // 玩家
            PlayerPet,     // 玩家宠物
            Monster        // 怪物
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

            public Stats(float health = 100f, float maxHealth = 100f, float mana = 0f, float maxMana = 100f,
                         float attackDamage = 10f, float armor = 5f, float attackSpeed = 1f,
                         float spellPower = 10f, float magicResist = 5f, float manaRegen = 2f,
                         float critRate = 0.1f, float dodgeRate = 0.05f, float moveSpeed = 2f)
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
                    default: Debug.LogWarning($"未知属性: {statName}"); break;
                }
            }
        }

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

        protected virtual void Awake()
        {
            stats = new Stats();
            heroName = gameObject.name;
            currentAttackMode = gameObject.AddComponent<MeleePhysicalAttack>();
            currentAttackMode.SetHero(this);

            // 动态获取 Canvas
            uiCanvas = FindObjectOfType<Canvas>();
            if (uiCanvas == null)
            {
                Debug.LogWarning($"未找到 Canvas 组件，将禁用血条和魔法条: {heroName}");
            }

            // 初始化 Tilemap
            if (MapManager.Instance != null)
            {
                tilemap = MapManager.Instance.GetTilemap();
                collisionTilemap = MapManager.Instance.GetCollisionTilemap();
                currentAttackMode?.SetTilemaps(tilemap, collisionTilemap);
                if (string.IsNullOrEmpty(currentMapId))
                {
                    currentMapId = MapManager.Instance.GetMapId();
                }
            }
            else
            {
                Debug.LogWarning($"MapManager 单例未初始化，英雄: {heroName}");
            }
        }

        protected virtual void Start()
        {
            if (heroBarPrefab == null)
            {
                Debug.LogWarning($"未分配 heroBarPrefab，请在 {heroName} 的 Inspector 中设置 Slider_HealthBar_L1 预制件");
            }
            else
            {
                InitializeUI();
            }
            StartCoroutine(ManaRegeneration());
        }

        protected virtual void FixedUpdate()
        {
            if (isMoving && !isDead)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, stats.moveSpeed * Time.fixedDeltaTime);
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    transform.position = targetPosition;
                    if (path != null && pathIndex < path.Count)
                    {
                        if (MapManager.Instance != null && MapManager.Instance.GetTilemap() != null)
                        {
                            targetPosition = ApplyPositionOffset(MapManager.Instance.GetTilemap().GetCellCenterWorld(path[pathIndex]));
                            pathIndex++;
                        }
                        else
                        {
                            Debug.LogWarning($"MapManager 或 Tilemap 未初始化，停止移动: {heroName}");
                            isMoving = false;
                            path.Clear();
                            pathIndex = 0;
                        }
                    }
                    else
                    {
                        isMoving = false;
                        path.Clear();
                        pathIndex = 0;
                        if (!isHurtAnimationPlaying)
                        {
                            PlayAnimation("Idle");
                        }
                    }
                }
                UpdateOrientation();
            }
        }

        protected virtual void Update()
        {
            if (isDead) return;
            if (heroBarInstance != null)
            {
                Vector3 worldPos = transform.position + healthBarOffset;
                Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPos) : worldPos;
                heroBarInstance.transform.position = new Vector3(screenPos.x, screenPos.y, 0f);
                UpdateHealthBar();
                UpdateEnergeBar();
            }
        }

        protected void InitializeUI()
        {
            heroBarInstance = Instantiate(heroBarPrefab, uiCanvas.transform);
            heroBarInstance.name = $"HeroBar_{heroName}";
            InitializeHealthBar();
            InitializeEnergeBar();
        }

        protected void InitializeHealthBar()
        {
            if (heroBarInstance == null) return;
            Transform hpTransform = heroBarInstance.transform.Find("Slider_HP");
            if (hpTransform == null)
            {
                Debug.LogError($"未找到 Slider_HP 在 {heroBarInstance.name}");
                return;
            }
            healthBarSlider = hpTransform.GetComponent<Slider>();
            if (healthBarSlider == null)
            {
                Debug.LogError($"Slider_HP 缺少 Slider 组件");
                return;
            }
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = stats.maxHealth;
            UpdateHealthBar();
        }

        protected void InitializeEnergeBar()
        {
            if (heroBarInstance == null) return;
            Transform mpTransform = heroBarInstance.transform.Find("Slider_MP");
            if (mpTransform == null)
            {
                Debug.LogError($"未找到 Slider_MP 在 {heroBarInstance.name}");
                return;
            }
            energeBarSlider = mpTransform.GetComponent<Slider>();
            if (energeBarSlider == null)
            {
                Debug.LogError($"Slider_MP 缺少 Slider 组件");
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
                heroBarInstance.SetActive(!isDead);
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
            if (isDead) return;
            if (Random.value < stats.dodgeRate)
            {
                Debug.Log($"{heroName} 闪避了攻击");
                return;
            }

            float finalDamage = isMagic
                ? damage * (1f - stats.magicResist / (stats.magicResist + 100f))
                : damage * (1f - stats.armor / (stats.armor + 100f));
            if (Random.value < stats.critRate)
            {
                finalDamage *= 1.5f;
                Debug.Log($"{heroName} 受到暴击");
            }
            stats.ModifyStat("health", -finalDamage);
            Debug.Log($"{heroName} 受到 {finalDamage:F2} {(isMagic ? "魔法" : "物理")}伤害，剩余血量: {stats.health:F2}");

            UpdateHealthBar();

            if (stats.health <= 0 && !isDead)
            {
                isDead = true;
                StopAttack();
                isHurtAnimationPlaying = false;
                PlayAnimation("Death");
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null) collider.enabled = false;
                UpdateHealthBar();
                if (heroType == HeroType.PlayerHero)
                {
                    // UIManager.Instance.ShowDeathDialog(OnRespawn);
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

        public string GetCurrentMapId() => currentMapId;

        public void SetCurrentMapId(string mapId)
        {
            currentMapId = mapId;
            if (MapManager.Instance != null && MapManager.Instance.GetMapId() == mapId)
            {
                tilemap = MapManager.Instance.GetTilemap();
                collisionTilemap = MapManager.Instance.GetCollisionTilemap();
                currentAttackMode?.SetTilemaps(tilemap, collisionTilemap);
            }
            else
            {
                Debug.LogWarning($"MapManager 单例未初始化或 mapId 不匹配: {mapId}, 当前 MapManager mapId: {MapManager.Instance?.GetMapId()}");
            }
        }

        public virtual void PlayAnimation(string animationName, HeroJobs job = HeroJobs.Warrior)
        {
            switch (heroType)
            {
                case HeroType.PlayerHero:
                    var playerAnimator = GetComponent<PlayerAnimator>();
                    if (playerAnimator != null)
                        playerAnimator.ChangeAnimation(animationName, job);
                    else
                        Debug.LogWarning($"未找到 PlayerAnimator 组件: {heroName}");
                    break;
                case HeroType.PlayerPet:
                    var petAnimator = GetComponent<PetAnimator>();
                    if (petAnimator != null)
                        petAnimator.ChangeAnimation(animationName);
                    else
                        Debug.LogWarning($"未找到 PetAnimator 组件: {heroName}");
                    break;
                case HeroType.Monster:
                    var monsterAnimator = GetComponent<MonsterAnimator>();
                    if (monsterAnimator != null)
                        monsterAnimator.ChangeAnimation(animationName);
                    else
                        Debug.LogWarning($"未找到 MonsterAnimator 组件: {heroName}");
                    break;
                default:
                    Debug.LogWarning($"未知 HeroType: {heroType}");
                    break;
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

            Hero targetHero = target?.GetComponent<Hero>();
            if (targetHero == null || targetHero.isDead)
            {
                StopAttack();
                return;
            }

            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogWarning($"MapManager 或 Tilemap 未初始化，取消攻击: {heroName}");
                return;
            }

            Vector3Int attackerCell = MapManager.Instance.GetTilemap().WorldToCell(transform.position);
            if (currentAttackMode.IsWithinAttackDistance(attackerCell, targetCell))
            {
                if (stats.mana >= stats.maxMana && heroType != HeroType.PlayerHero)
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

        public void MoveToAttackRange(Vector3Int targetCell, bool isAuto)
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
            {
                Debug.LogWarning($"MapManager 或 Tilemap 未初始化，取消移动: {heroName}");
                return;
            }

            List<Vector3Int> path = Pathfinding.FindPathToAttackRange(
                MapManager.Instance.GetTilemap().WorldToCell(transform.position),
                targetCell,
                currentAttackMode.GetAttackDistance(),
                MapManager.Instance.GetTilemap(),
                MapManager.Instance.GetCollisionTilemap()
            );
            if (path != null && path.Count > 0)
            {
                this.path = path;
                pathIndex = 0;
                isMoving = true;
                isAutoAttacking = isAuto;
                targetPosition = ApplyPositionOffset(MapManager.Instance.GetTilemap().GetCellCenterWorld(path[pathIndex]));
                UpdateOrientation();
                PlayAnimation("Walk");
            }
        }

        public virtual void MoveTo(Vector3Int cellPos)
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
            {
                Debug.LogError($"MapManager 或 Tilemap 未设置: {heroName}");
                return;
            }

            if (MapManager.Instance.GetTilemap().HasTile(cellPos) && !GridUtility.HasObstacle(cellPos, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()))
            {
                string cacheKey = $"{MapManager.Instance.GetTilemap().WorldToCell(transform.position)}_{cellPos}";
                if (!pathCache.TryGetValue(cacheKey, out path))
                {
                    path = FindPath(MapManager.Instance.GetTilemap().WorldToCell(transform.position), cellPos);
                    if (path != null && path.Count > 0)
                    {
                        pathCache[cacheKey] = path; // 缓存路径
                    }
                }

                if (path != null && path.Count > 0)
                {
                    pathIndex = 0;
                    isMoving = true;
                    targetPosition = ApplyPositionOffset(MapManager.Instance.GetTilemap().GetCellCenterWorld(path[pathIndex]));
                    UpdateOrientation();
                    PlayAnimation("Walk");
                  //  Debug.Log($"移动到 {cellPos}，路径节点数: {path.Count}, 英雄: {heroName}");
                }
                else
                {
                    Debug.LogWarning($"未找到到 {cellPos} 的有效路径: {heroName}");
                }
            }
            else
            {
                Debug.LogWarning($"目标格子 {cellPos} 无效或有障碍物: {heroName}");
            }
        }

        public virtual void UpdateOrientation()
        {
            if (!isMoving || path == null || pathIndex >= path.Count)
            {
                return;
            }

            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogWarning($"MapManager 或 Tilemap 未初始化: {heroName}");
                return;
            }

            Vector3 targetWorldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(path[pathIndex]);
            Vector3 direction = targetWorldPos - transform.position;
            if (heroType == HeroType.PlayerHero || heroType == HeroType.PlayerPet)
            {
                var animator = GetComponent<PlayerAnimator>();
                if (animator != null)
                {
                    animator.SetOrientation(direction);
                }
            }
            else if (heroType == HeroType.Monster)
            {
                var animator = GetComponent<MonsterAnimator>();
                if (animator != null)
                {
                    animator.SetOrientation(direction);
                }
            }
        }

        public void StopAttack()
        {
            isAttacking = false;
            isAutoAttacking = false;
            lastTargetEnemy = null;
            lastTargetCell = Vector3Int.zero;
            StopAllCoroutines();
            if (!isDead && !isHurtAnimationPlaying)
            {
                PlayAnimation("Idle");
            }
        }

        public void Equip(Equipment equipment)
        {
            //if (equipment.Count < maxEquipment)
            //{
            //    this.equipment.Add(equipment);
            //    foreach (var bonus in equipment.statBonuses)
            //    {
            //        stats.ModifyStat(bonus.Key, bonus.Value);
            //    }
            //    // GearsManager.Instance.ApplySkinChanges();
            //}
        }

        protected virtual IEnumerator PlayHurtAnimation()
        {
            isHurtAnimationPlaying = true;
            PlayAnimation("Hurt");
            yield return new WaitForSeconds(0.5f);
            isHurtAnimationPlaying = false;
            if (!isDead && !isAttacking)
            {
                PlayAnimation("Idle");
            }
        }

        protected List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
            {
                Debug.LogWarning($"MapManager 或 Tilemap 未初始化，取消寻路: {heroName}");
                return null;
            }

            if (start == goal)
            {
                Debug.Log($"目标格子与当前位置相同，无需移动: {heroName}");
                return new List<Vector3Int>();
            }

            List<Node> openList = new List<Node>();
            HashSet<Vector3Int> closedList = new HashSet<Vector3Int>();
            Node startNode = new Node(start, 0, Heuristic(start, goal), null);
            openList.Add(startNode);
            int maxIterations = 1000;

            while (openList.Count > 0 && maxIterations-- > 0)
            {
                Node current = openList[0];
                int currentIndex = 0;
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].fCost < current.fCost || (openList[i].fCost == current.fCost && openList[i].hCost < current.hCost))
                    {
                        current = openList[i];
                        currentIndex = i;
                    }
                }

                openList.RemoveAt(currentIndex);
                closedList.Add(current.cellPos);

                if (current.cellPos == goal)
                {
                    List<Vector3Int> path = new List<Vector3Int>();
                    Node node = current;
                    while (node != null)
                    {
                        path.Add(node.cellPos);
                        node = node.parent;
                    }
                    path.Reverse();
                    return path.Count > 1 ? path.GetRange(1, path.Count - 1) : path;
                }

                foreach (Vector3 dir in hexDirections)
                {
                    Vector3Int neighborPos = MapManager.Instance.GetTilemap().WorldToCell(MapManager.Instance.GetTilemap().GetCellCenterWorld(current.cellPos) + dir);
                    if (closedList.Contains(neighborPos) || !MapManager.Instance.GetTilemap().HasTile(neighborPos) || GridUtility.HasObstacle(neighborPos, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()))
                    {
                        continue;
                    }

                    float newGCost = current.gCost + 1;
                    Node neighbor = new Node(neighborPos, newGCost, Heuristic(neighborPos, goal), current);
                    bool inOpenList = false;

                    for (int i = 0; i < openList.Count; i++)
                    {
                        if (openList[i].cellPos == neighborPos && openList[i].gCost <= newGCost)
                        {
                            inOpenList = true;
                            break;
                        }
                        else if (openList[i].cellPos == neighborPos)
                        {
                            openList[i] = neighbor; // 更新更优路径
                            inOpenList = true;
                        }
                    }

                    if (!inOpenList)
                    {
                        openList.Add(neighbor);
                    }
                }
            }

            Debug.LogWarning($"未找到路径，超出最大迭代次数或无有效路径: {heroName}");
            return null;
        }

        private float Heuristic(Vector3Int a, Vector3Int b)
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogWarning($"MapManager 或 Tilemap 未初始化，取消启发式计算: {heroName}");
                return 0f;
            }
            Vector3 worldA = MapManager.Instance.GetTilemap().GetCellCenterWorld(a);
            Vector3 worldB = MapManager.Instance.GetTilemap().GetCellCenterWorld(b);
            return Vector3.Distance(worldA, worldB) / 0.866f;
        }

        protected virtual void OnDestroy()
        {
            // 清理逻辑
        }
    }
}