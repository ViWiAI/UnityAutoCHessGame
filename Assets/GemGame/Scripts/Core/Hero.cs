using Game.Animation;
using Game.Combat;
using Game.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Game.Data;

namespace Game.Core
{
    public interface IAnimatable
    {
        void PlayAnimation(string animationName, HeroRole job = HeroRole.Warrior);
    }

    public abstract class Hero : MonoBehaviour, IAnimatable
    {
        [SerializeField] private float spriteHeightOffset = -0.2f;
        [SerializeField] private GameObject heroBarPrefab; // Slider_HealthBar_L1 预制件
        [SerializeField] private Vector3 healthBarOffset = new Vector3(-0.1f, 1.45f, 0f);
        [SerializeField] private Canvas uiCanvas; // 在 Inspector 中分配 Canvas
        [SerializeField] public HeroType heroType; // 英雄类型

        protected bool isLocalPlayer;
        protected Slider healthBarSlider; // 血条 Slider
        protected Slider energeBarSlider; // 魔法条 Slider
        protected GameObject heroBarInstance; // 血条实例
        protected int currentMapId;
        protected AttackMode currentAttackMode;
        protected List<Equipment> equipment = new List<Equipment>();
        public Stats stats;
        public string heroName;
        public bool isDead;
        protected bool isMoving;
        protected bool isWalking;
        protected bool isAttacking;
        protected bool isHurtAnimationPlaying;
        protected bool isAutoAttacking;
        protected Vector3 targetPosition;
        protected List<Vector3Int> path = new List<Vector3Int>();
        protected int pathIndex;
        protected GameObject lastTargetEnemy;
        protected Vector3Int lastTargetCell;
        private Vector3Int? currentTargetPosition; // 新增：缓存当前目标位置

        protected readonly Vector3[] hexDirections = new Vector3[]
        {
            new Vector3(1f, 0f, 0f),        // 右
            new Vector3(-1f, 0f, 0f),       // 左
            new Vector3(0.5f, 0.866f, 0f),  // 右上
            new Vector3(-0.5f, 0.866f, 0f), // 左上
            new Vector3(0.5f, -0.866f, 0f), // 右下
            new Vector3(-0.5f, -0.866f, 0f) // 左下
        };
        private readonly int maxEquipment = 6;
        private static readonly Dictionary<string, List<Vector3Int>> pathCache = new Dictionary<string, List<Vector3Int>>(); // 路径缓存

        public enum HeroType
        {
            Player,    // 玩家
            Pet,     // 玩家宠物
            Monster        // 怪物
        }

        [System.Serializable]
        public class Stats
        {
            public float curHP;
            public float maxHP;
            public float curMP;
            public float maxMP;
            public float AC;
            public float MC;
            public float attackDamage;
            public float attackSpeed;
            public float spellPower;
            public float MPRegen;
            public float HPRegen;
            public float critRate;
            public float critDmg;
            public float dodgeRate;
            public float moveSpeed;
            public float attackRange;
            public float Str;   // ADD attackDamage critRate
            public float Sta;   // ADD maxHP HPRegen
            public float Dex;   // ADD critRate dodgeRate AC
            public float Int;   // ADD maxMP spellPower critRate
            public float Spi;   // ADD MC critDmg MPRegen

            public Stats(
                float curHP = 100f, float maxHP = 100f, float curMP = 50f, float maxMP = 50f,
                float AC = 0f, float MC = 0f, float attackDamage = 10f, float attackSpeed = 1f,
                float spellPower = 0f, float MPRegen = 1f, float HPRegen = 1f,
                float critRate = 0.05f, float critDmg = 1.5f, float dodgeRate = 0.05f,
                float moveSpeed = 3f, float attackRange = 1f,
                float Str = 10f, float Sta = 10f, float Dex = 10f, float Int = 10f, float Spi = 10f)
            {
                this.curHP = curHP;
                this.maxHP = maxHP;
                this.curMP = curMP;
                this.maxMP = maxMP;
                this.AC = AC;
                this.MC = MC;
                this.attackDamage = attackDamage;
                this.attackSpeed = attackSpeed;
                this.spellPower = spellPower;
                this.MPRegen = MPRegen;
                this.HPRegen = HPRegen;
                this.critRate = critRate;
                this.critDmg = critDmg;
                this.dodgeRate = dodgeRate;
                this.moveSpeed = moveSpeed;
                this.attackRange = attackRange;
                this.Str = Str;
                this.Sta = Sta;
                this.Dex = Dex;
                this.Int = Int;
                this.Spi = Spi;
            }

            public void ModifyStat(string statName, float value)
            {
                switch (statName.ToLower())
                {
                    case "curhp":
                        curHP = Mathf.Clamp(curHP + value, 0f, maxHP);
                        break;
                    case "maxhp":
                        maxHP += value;
                        if (maxHP < 0) maxHP = 0;
                        curHP = Mathf.Clamp(curHP, 0f, maxHP); // 调整curHP以适应新的maxHP
                        break;
                    case "curmp":
                        curMP = Mathf.Clamp(curMP + value, 0f, maxMP);
                        break;
                    case "maxmp":
                        maxMP += value;
                        if (maxMP < 0) maxMP = 0;
                        curMP = Mathf.Clamp(curMP, 0f, maxMP); // 调整curMP以适应新的maxMP
                        break;
                    case "ac":
                        AC += value;
                        break;
                    case "mc":
                        MC += value;
                        break;
                    case "attackdamage":
                        attackDamage += value;
                        break;
                    case "attackspeed":
                        attackSpeed += value;
                        if (attackSpeed < 0) attackSpeed = 0; // 防止负值
                        break;
                    case "spellpower":
                        spellPower += value;
                        break;
                    case "mpregen":
                        MPRegen += value;
                        break;
                    case "hpregen":
                        HPRegen += value;
                        break;
                    case "critrate":
                        critRate += value;
                        critRate = Mathf.Clamp(critRate, 0f, 1f); // 限制在0-1
                        break;
                    case "critdmg":
                        critDmg += value;
                        if (critDmg < 0) critDmg = 0; // 防止负值
                        break;
                    case "dodgerate":
                        dodgeRate += value;
                        dodgeRate = Mathf.Clamp(dodgeRate, 0f, 1f); // 限制在0-1
                        break;
                    case "movespeed":
                        moveSpeed += value;
                        if (moveSpeed < 0) moveSpeed = 0; // 防止负值
                        break;
                    case "attackrange":
                        attackRange += value;
                        if (attackRange < 0) attackRange = 0; // 防止负值
                        break;
                    case "str":
                        Str += value;
                        if (Str < 0) Str = 0;
                        // 力量影响attackDamage和critRate
                        attackDamage += value * 0.5f; // 示例：每点Str增加0.5攻击力
                        critRate += value * 0.01f; // 示例：每点Str增加1%暴击率
                        critRate = Mathf.Clamp(critRate, 0f, 1f);
                        break;
                    case "sta":
                        Sta += value;
                        if (Sta < 0) Sta = 0;
                        // 耐力影响maxHP和HPRegen
                        maxHP += value * 10f; // 示例：每点Sta增加10最大生命
                        curHP = Mathf.Clamp(curHP, 0f, maxHP);
                        HPRegen += value * 0.1f; // 示例：每点Sta增加0.1生命回复
                        break;
                    case "dex":
                        Dex += value;
                        if (Dex < 0) Dex = 0;
                        // 敏捷影响critRate、dodgeRate和AC
                        critRate += value * 0.01f; // 示例：每点Dex增加1%暴击率
                        critRate = Mathf.Clamp(critRate, 0f, 1f);
                        dodgeRate += value * 0.01f; // 示例：每点Dex增加1%闪避率
                        dodgeRate = Mathf.Clamp(dodgeRate, 0f, 1f);
                        AC += value * 0.5f; // 示例：每点Dex增加0.5防御
                        break;
                    case "int":
                        Int += value;
                        if (Int < 0) Int = 0;
                        // 智力影响maxMP、spellPower和critRate
                        maxMP += value * 5f; // 示例：每点Int增加5最大魔法
                        curMP = Mathf.Clamp(curMP, 0f, maxMP);
                        spellPower += value * 1f; // 示例：每点Int增加1魔法强度
                        critRate += value * 0.01f; // 示例：每点Int增加1%暴击率
                        critRate = Mathf.Clamp(critRate, 0f, 1f);
                        break;
                    case "spi":
                        Spi += value;
                        if (Spi < 0) Spi = 0;
                        // 精神影响MC、critDmg和MPRegen
                        MC += value * 0.5f; // 示例：每点Spi增加0.5魔法抗性
                        critDmg += value * 0.05f; // 示例：每点Spi增加5%暴击伤害
                        MPRegen += value * 0.1f; // 示例：每点Spi增加0.1魔法回复
                        break;
                    default:
                        Debug.LogWarning($"未知属性: {statName}");
                        break;
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
            isWalking = false;
            stats = new Stats();
            heroName = gameObject.name;
            uiCanvas = FindObjectOfType<Canvas>();
            if (uiCanvas == null)
            {
                Debug.LogWarning($"未找到 Canvas 组件，将禁用血条和魔法条: {heroName}");
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
            // 强制对齐初始位置
         //   SnapToGrid();
        }

        protected virtual void FixedUpdate()
        {
            if (isMoving && !isDead)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, stats.moveSpeed * Time.unscaledDeltaTime);
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    // 强制对齐到当前格子
                    //    SnapToGrid();
                    pathIndex++;
                    if (pathIndex < path.Count)
                    {
                        targetPosition = ApplyPositionOffset(MapManager.Instance.GetTilemap().GetCellCenterWorld(path[pathIndex]));
                        if (isLocalPlayer)
                        {
                            Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(targetPosition);
                            InputManager.Instance.setTipsPlayerText(currentCell.x.ToString() + " , " + currentCell.y.ToString());
                        }
                        
                        UpdateOrientation();
                    }
                    else
                    {
                        isWalking = false;
                        isMoving = false;
                        path.Clear();
                        pathIndex = 0;
                        // 重置点击记录，仅对 PlayerHero 有效
                        if (this is PlayerHero playerHero)
                        {
                            playerHero.ResetClickedCell();
                        }
                        if (!isHurtAnimationPlaying)
                        {
                            PlayAnimation("Idle");
                        }
                    }
                }
            }
        }

        // 强制对齐到格子中心
        protected void SnapToGrid()
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogWarning($"MapManager 或 Tilemap 未初始化，无法对齐: {heroName}");
                return;
            }
            Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(transform.position);
            transform.position = MapManager.Instance.GetTilemap().GetCellCenterWorld(currentCell);
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
            healthBarSlider.maxValue = stats.maxHP;
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
            energeBarSlider.maxValue = stats.maxHP;
            UpdateEnergeBar();
        }

        protected void UpdateHealthBar()
        {
            if (healthBarSlider != null)
            {
                healthBarSlider.value = stats.maxHP;
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
                energeBarSlider.value = stats.curMP;
            }
        }

        public void UseMana(float amount)
        {
            stats.curMP = Mathf.Max(0, stats.curMP - amount);
            UpdateEnergeBar();
        }

        private IEnumerator ManaRegeneration()
        {
            while (!isDead)
            {
                stats.ModifyStat("curMP", stats.MPRegen * Time.deltaTime);
                UpdateEnergeBar();
                yield return null;
            }
        }
     

        public HeroType GetHeroType() => heroType;

        public void SetCurrentMapId(int mapId)
        {
            currentMapId = mapId;
        }



        public virtual void TakeDamage(float damage, bool isMagic = false)
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
                finalDamage *= stats.critDmg; // 使用 critDmg 替代硬编码的 1.5f
                Debug.Log($"{heroName} 受到暴击");
            }
            stats.ModifyStat("curHP", -finalDamage);
            Debug.Log($"{heroName} 受到 {finalDamage:F2} {(isMagic ? "魔法" : "物理")}伤害，剩余血量: {stats.curHP:F2}");

            UpdateHealthBar();

            if (stats.curHP <= 0 && !isDead)
            {
                isDead = true;
                StopAttack();
                isHurtAnimationPlaying = false;
                PlayAnimation("Death");
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null) collider.enabled = false;
                UpdateHealthBar();
                if (heroType == HeroType.Player)
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
            stats.curHP = stats.maxHP;
            stats.curMP = stats.maxMP;
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
        public int GetCurrentMapId() => currentMapId;


        public virtual void PlayAnimation(string animationName, HeroRole job = HeroRole.Warrior)
        {
            switch (heroType)
            {
                case HeroType.Player:
                    var playerAnimator = GetComponent<PlayerAnimator>();
                    if (playerAnimator != null)
                        playerAnimator.ChangeAnimation(animationName, job);
                    else
                        Debug.LogWarning($"未找到 PlayerAnimator 组件: {heroName}");
                    break;
                case HeroType.Pet:
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
                if (stats.curMP >= stats.maxMP && heroType != HeroType.Player)
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

        protected List<Vector3Int> FindPathToAttackRange(Vector3Int start, Vector3Int target, float attackRange)
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
            {
                Debug.LogError($"MapManager 或 Tilemap 未设置: {heroName}");
                return null;
            }

            // 找到目标格子周围的攻击范围内格子
            List<Vector3Int> attackRangeCells = new List<Vector3Int>();
            foreach (Vector3 dir in hexDirections)
            {
                Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(target) + dir * attackRange;
                Vector3Int cell = MapManager.Instance.GetTilemap().WorldToCell(worldPos);
                if (MapManager.Instance.GetTilemap().HasTile(cell) && !GridUtility.HasObstacle(cell, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()))
                {
                    attackRangeCells.Add(cell);
                }
            }

            // 寻找最近的攻击范围内格子
            List<Vector3Int> shortestPath = null;
            float minDistance = float.MaxValue;

            foreach (Vector3Int goal in attackRangeCells)
            {
                List<Vector3Int> path = FindPath(start, goal);
                if (path != null && path.Count > 0)
                {
                    float distance = GridUtility.CalculateGridDistance(start, goal, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap());
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        shortestPath = path;
                    }
                }
            }

            if (shortestPath == null)
            {
                Debug.LogWarning($"{heroName} 未找到到攻击范围 {target} 的路径");
            }
            else
            {
                Debug.Log($"{heroName} 找到攻击范围路径: {string.Join(" -> ", shortestPath)}");
            }
            return shortestPath;
        }

        public virtual void MoveToAttackRange(Vector3Int targetCell, bool isAuto)
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
            {
                Debug.LogError($"MapManager 或 Tilemap 未设置: {heroName}");
                return;
            }

            List<Vector3Int> path = FindPathToAttackRange(
                MapManager.Instance.GetTilemap().WorldToCell(transform.position),
                targetCell,
                currentAttackMode.GetAttackDistance()
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
                Debug.Log($"{heroName} 开始移动到攻击范围，目标: {targetCell}, 路径: {string.Join(" -> ", path)}");
            }
            else
            {
                Debug.LogWarning($"{heroName} 未找到到攻击范围 {targetCell} 的路径");
                StopMoving();
                if (!isAuto)
                {
                    StopAttack();
                }
            }
        }

        public virtual void MoveTo(Vector3Int cellPos)
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
            {
                Debug.LogError($"MapManager 或 Tilemap 未设置: {heroName}");
                return;
            }

            // 强制对齐当前位置
       //     SnapToGrid();
            Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(transform.position);
         //   Debug.Log($"MoveTo: 当前格子={currentCell}, 目标格子={cellPos}");
            
            // 检查目标格子是否有效
            if (!MapManager.Instance.GetTilemap().HasTile(cellPos) || GridUtility.HasObstacle(cellPos, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()))
            {
                Debug.LogWarning($"目标格子 {cellPos} 无效或有障碍物");
                if (this is PlayerHero playerHero)
                {
                    playerHero.ResetClickedCell();
                }
                return;
            }

            // 直接相邻且无障碍物，直接移动
            if (IsAdjacent(currentCell, cellPos))
            {
                path = new List<Vector3Int> { cellPos };
                pathIndex = 0;
                isMoving = true;
                targetPosition = ApplyPositionOffset(MapManager.Instance.GetTilemap().GetCellCenterWorld(cellPos));
                UpdateOrientation();
                PlayAnimation("Walk");
                if (isLocalPlayer)
                {
                    Vector3Int curCell = MapManager.Instance.GetTilemap().WorldToCell(targetPosition);
                    InputManager.Instance.setTipsPlayerText(curCell.x.ToString() + "," + curCell.y.ToString());
                }
                Debug.Log($"直接移动到相邻格子 {cellPos}");
                return;
            }

            // 检查路径缓存
            string cacheKey = $"{currentCell}_{cellPos}";
            if (pathCache.TryGetValue(cacheKey, out path) && path != null && path.Count > 0)
            {
                Debug.Log($"使用缓存路径: 从 {currentCell} 到 {cellPos}, 路径: {string.Join(" -> ", path)}");
            }
            else
            {
             //   Debug.Log($"缓存无效或不存在，重新计算路径: {cacheKey}");
                path = FindPath(currentCell, cellPos);
                if (path != null && path.Count > 0)
                {
                    pathCache[cacheKey] = path; // 仅缓存有效路径
                 //   Debug.Log($"缓存新路径: {cacheKey}, 路径: {string.Join(" -> ", path)}");
                }
                else
                {
                    Debug.LogWarning($"未找到到 {cellPos} 的有效路径: {heroName}");
                    if (this is PlayerHero playerHero)
                    {
                        playerHero.ResetClickedCell();
                        Debug.LogWarning($"playerHero.StopMoving");
                        playerHero.StopMoving();
                    }
                    ClearPathCacheForCell(cellPos);
                    return;
                }
            }

            pathIndex = 0;
            isMoving = true;
            targetPosition = ApplyPositionOffset(MapManager.Instance.GetTilemap().GetCellCenterWorld(path[pathIndex]));
            UpdateOrientation();
            if(!isWalking)
            {
                PlayAnimation("Walk");
                isWalking = true;
            }
           
         //   Debug.Log($"移动到 {cellPos}，路径: {string.Join(" -> ", path)}");
        }

        public void ChangeMove()
        {
            path.Clear();
            pathIndex = 0;
            currentTargetPosition = null; // 清除目标位置
                                          // 清除所有相关路径缓存
            if (MapManager.Instance != null && MapManager.Instance.GetTilemap() != null)
            {
                Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(transform.position);
                ClearPathCacheForCell(currentCell);
            }

            Debug.Log($"{heroName} 移动路径更换");
        }
        
        public void StopMoving()
        {
            if (isMoving)
            {
                isMoving = false;
                path.Clear();
                pathIndex = 0;
                currentTargetPosition = null; // 清除目标位置

                // 清除所有相关路径缓存
                if (MapManager.Instance != null && MapManager.Instance.GetTilemap() != null)
                {
                    Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(transform.position);
                    ClearPathCacheForCell(currentCell);
                }
                if (this is PlayerHero playerHero)
                {
                    playerHero.ResetClickedCell();
                }

                if (!isHurtAnimationPlaying && !isDead)
                {
                    PlayAnimation("Idle");
                }

                Debug.Log($"{heroName} 移动已中断");
            }
        }

        // 清除以特定格子为起点的所有路径缓存
        protected void ClearPathCacheForCell(Vector3Int startCell)
        {
            List<string> keysToRemove = new List<string>();

            foreach (var key in pathCache.Keys)
            {
                if (key.StartsWith($"{startCell}_"))
                {
                    if (!pathCache.TryGetValue(key, out var cachedPath) || cachedPath == null || cachedPath.Count == 0)
                    {
                        keysToRemove.Add(key); // 移除无效路径
                    }
                    else
                    {
                        // 验证路径中的格子是否仍然有效
                        bool isValid = true;
                        foreach (var pos in cachedPath)
                        {
                            if (!MapManager.Instance.GetTilemap().HasTile(pos) || GridUtility.HasObstacle(pos, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()))
                            {
                                isValid = false;
                                break;
                            }
                        }
                        if (!isValid)
                        {
                            keysToRemove.Add(key); // 移除包含无效格子的路径
                        }
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                pathCache.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                Debug.Log($"已清除 {keysToRemove.Count} 条以 {startCell} 为起点的路径缓存");
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
            if (heroType == HeroType.Player || heroType == HeroType.Pet)
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
                isWalking = false;
                PlayAnimation("Idle");
                return new List<Vector3Int>();
            }

            // 直接相邻且无障碍物，直接返回目标格子
            if (IsAdjacent(start, goal))
            {
                if (!GridUtility.HasObstacle(goal, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()))
                {
                    Debug.Log($"单步移动: 从 {start} 到 {goal}");
                    return new List<Vector3Int> { goal };
                }
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
                    Debug.Log($"找到路径: {string.Join(" -> ", path)}");
                    return path.Count > 1 ? path.GetRange(1, path.Count - 1) : path;
                }

                foreach (Vector3 dir in hexDirections)
                {
                    Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(current.cellPos) + dir;
                    Vector3Int neighborPos = MapManager.Instance.GetTilemap().WorldToCell(worldPos);
                    if (closedList.Contains(neighborPos) ||
                        !MapManager.Instance.GetTilemap().HasTile(neighborPos) ||
                        GridUtility.HasObstacle(neighborPos, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()))
                    {
                        continue;
                    }

                    float newGCost = current.gCost + 1;
                    Node neighbor = new Node(neighborPos, newGCost, Heuristic(neighborPos, goal), current);
                    bool inOpenList = false;

                    for (int i = 0; i < openList.Count; i++)
                    {
                        if (openList[i].cellPos == neighborPos)
                        {
                            if (openList[i].gCost > newGCost)
                            {
                                openList[i] = neighbor;
                            }
                            inOpenList = true;
                            break;
                        }
                    }

                    if (!inOpenList)
                    {
                        openList.Add(neighbor);
                    }
                }
            }

            Debug.LogWarning($"未找到路径，超出最大迭代次数或无有效路径: 从 {start} 到 {goal}");
            string cacheKey = $"{start}_{goal}";
            pathCache.Remove(cacheKey); // 移除无效路径缓存
            return null;
        }

        private bool IsAdjacent(Vector3Int start, Vector3Int goal)
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogWarning($"MapManager 或 Tilemap 未初始化: {heroName}");
                return false;
            }

            // 获取所有可能的六边形方向
            foreach (Vector3 dir in hexDirections)
            {
                Vector3Int neighborPos = MapManager.Instance.GetTilemap().WorldToCell(
                    MapManager.Instance.GetTilemap().GetCellCenterWorld(start) + dir);
                if (neighborPos == goal)
                {
                    return true;
                }
            }
            return false;
        }

        private float Heuristic(Vector3Int a, Vector3Int b)
        {
            if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
            {
                Debug.LogWarning($"MapManager 或 Tilemap 未初始化，取消启发式计算: {heroName}");
                return 0f;
            }

            // 六边形网格的轴向坐标
            int ax = a.x;
            int az = a.y;
            int bx = b.x;
            int bz = b.y;

            // 六边形网格距离计算
            return (Mathf.Abs(ax - bx) +
                    Mathf.Abs(ax + az - bx - bz) +
                    Mathf.Abs(az - bz)) / 2f;
        }

        protected virtual void OnDestroy()
        {
            // 清理逻辑
        }
    }
}