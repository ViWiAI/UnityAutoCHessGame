using Game.Combat;
using Game.Core;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public class PlayerMovement : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [SerializeField] public Tilemap tilemap; // 野外主Tilemap（land）
    [SerializeField] public Tilemap collisionTilemap; // 野外障碍Tilemap
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private SpriteRenderer highlightSprite;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 0.8f);
    [SerializeField] private Color obstacleHighlightColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] private float spriteHeightOffset = -0.2f;
    [SerializeField] private float health = 1000f;
    [SerializeField] private float autoAttackScanInterval = 0.5f; // 自动战斗扫描间隔[Header("Attack Mode Settings")]
    [SerializeField] private AttackMode currentAttackMode;
    [SerializeField] private AttackModeType attackModeType;
    [SerializeField] public GameMode currentGameMode = GameMode.Wild;

    public enum AttackModeType
    {
        None, MeleePhysical, MeleeMagic, RangedPhysical, RangedMagic
    }

    public enum GameMode
    {
        Wild, AutoChess
    }

    private SkeletonAnimation monsterAnimator;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private Vector3Int lastHighlightedCell;
    private bool isHighlightActive = false;
    private Color originalColor;
    private List<Vector3Int> path;
    private int pathIndex;
    private float scalingFactor = 1.35f;
    private float skeletonScaleX = 1f;
    private Vector3 lastPosition;
    private Vector3Int lastTargetCell;
    private bool isAutoAttacking = false;
    private GameObject lastTargetEnemy;
    private bool isHurtAnimationPlaying = false;
    private bool isDead = false;

    private readonly Vector3[] hexDirections = new Vector3[]
    {
    new Vector3(1f, 0f, 0f),
    new Vector3(-1f, 0f, 0f),
    new Vector3(0.5f, 1.154f, 0f),
    new Vector3(-0.5f, 1.154f, 0f),
    new Vector3(0.5f, -1.154f, 0f),
    new Vector3(-0.5f, -1.154f, 0f)
    };

    private class Node
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

    void Start()
    {
        ApplyPositionOffset(transform.position);
        monsterAnimator = GetComponent<SkeletonAnimation>();
        if (monsterAnimator != null) ChangeAnimation("Idle");
        targetPosition = transform.position;
        lastPosition = transform.position;
        path = new List<Vector3Int>();
        pathIndex = 0;
        if (tilemap != null) originalColor = tilemap.color;
        if (highlightSprite != null) highlightSprite.enabled = false;

    }

    void Update()
    {
        if (isDead) return;

        UpdateTileHighlight();
        if (currentGameMode == GameMode.Wild)
        {
            HandleMouseClick();
            HandleAttackInput();
        }
       
    }


    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Tilemap currentTilemap = tilemap;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = currentTilemap.transform.position.z;
            Vector3Int targetCell = currentTilemap.WorldToCell(mousePos);

            if (currentTilemap.HasTile(targetCell) && !HasObstacle(currentTilemap.GetCellCenterWorld(targetCell)))
            {
                GameObject targetEnemy = FindEnemyAtCell(targetCell);
                if (targetEnemy != null)
                {
                    lastTargetCell = targetCell;
                    lastTargetEnemy = targetEnemy;
                    if (currentAttackMode != null)
                    {
                        Vector3Int attackerCell = currentTilemap.WorldToCell(transform.position);
                        AdjustOrientation(targetCell);
                        if (currentAttackMode.IsWithinAttackDistance(attackerCell, targetCell))
                        {
                            if (!isAutoAttacking)
                            {
                                isAutoAttacking = true;
                                StartCoroutine(ContinuousAttack(targetEnemy, targetCell));
                            }
                        }
                        else
                        {
                            MoveToAttackRange(targetCell);
                            Debug.Log($"Moving to attack range for cell {targetCell}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No attack mode assigned!");
                    }
                }
                else
                {
                    Debug.Log("No enemy (Player or Monster) found at target cell!");
                }
            }
            else
            {
                Debug.Log("Invalid target cell: No tile or contains obstacle/water.");
            }
        }
    }

    private IEnumerator ContinuousAttack(GameObject targetEnemy, Vector3Int targetCell)
    {
        while (isAutoAttacking && targetEnemy != null && targetEnemy.activeInHierarchy && !isDead)
        {
            Vector3Int attackerCell = GetCurrentTilemap().WorldToCell(transform.position);
            Vector3Int currentTargetCell = GetCurrentTilemap().WorldToCell(targetEnemy.transform.position);

            if (currentAttackMode != null && currentAttackMode.IsWithinAttackDistance(attackerCell, currentTargetCell) && !isHurtAnimationPlaying)
            {
                AdjustOrientation(currentTargetCell);
                currentAttackMode.PerformAttack(currentTargetCell);
                Debug.Log($"Attack triggered for enemy {targetEnemy.name} at cell {currentTargetCell}");
                yield return new WaitForSeconds(currentAttackMode.GetCooldown());
                if (!isHurtAnimationPlaying && health > 0)
                {
                    ChangeAnimation("Idle");
                }
            }
            else
            {
                MoveToAttackRange(currentTargetCell);
                break;
            }
        }

        StopAttack();
    }

    private GameObject FindEnemyAtCell(Vector3Int cell)
    {
        Vector3 worldPos = GetCurrentTilemap().GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.5f);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player") || hit.CompareTag("Monster"))
            {
                return hit.gameObject;
            }
        }
        return null;
    }

    private void AdjustOrientation(Vector3Int targetCell)
    {
        if (monsterAnimator == null) return;
        Vector3 targetWorldPos = GetCurrentTilemap().GetCellCenterWorld(targetCell);
        Vector3 direction = (targetWorldPos - transform.position).normalized;
        skeletonScaleX = direction.x < 0 ? -1f : 1f;
        monsterAnimator.skeleton.ScaleX = skeletonScaleX;
        Debug.Log($"Adjusted orientation: ScaleX = {skeletonScaleX}, Direction = {direction}, TargetCell = {targetCell}");
    }

    public void SetAttackMode(AttackMode newMode)
    {
        if (currentAttackMode != null)
        {
            Destroy(currentAttackMode);
        }
        currentAttackMode = newMode;
        Debug.Log($"Set attack mode to: {currentAttackMode?.GetType().Name ?? "None"}");
    }

  

    public void SetGameMode(GameMode mode)
    {
        currentGameMode = mode;
        Debug.Log($"Game mode switched to: {mode}");
    }

    private Tilemap GetCurrentTilemap()
    {
        return tilemap;
    }

    void FixedUpdate()
    {
        if (isMoving && !isDead)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                if (path != null && pathIndex < path.Count)
                {
                    Vector3 targetWorldPos = GetCurrentTilemap().GetCellCenterWorld(path[pathIndex]);
                    targetPosition = ApplyPositionOffset(targetWorldPos);
                    pathIndex++;
                    Debug.Log($"Moving to path point {pathIndex}: {targetPosition}");
                }
                else
                {
                    isMoving = false;
                    if (!isHurtAnimationPlaying)
                    {
                      //  ChangeAnimation("Idle");
                    }
                    path.Clear();
                    pathIndex = 0;

                    if (currentGameMode == GameMode.Wild && lastTargetEnemy != null && lastTargetCell != Vector3Int.zero)
                    {
                        Vector3Int attackerCell = GetCurrentTilemap().WorldToCell(transform.position);
                        Vector3Int currentTargetCell = GetCurrentTilemap().WorldToCell(lastTargetEnemy.transform.position);
                        if (currentAttackMode != null && currentAttackMode.IsWithinAttackDistance(attackerCell, currentTargetCell) && lastTargetEnemy.activeInHierarchy && !isHurtAnimationPlaying)
                        {
                            if (!isAutoAttacking)
                            {
                                isAutoAttacking = true;
                                StartCoroutine(ContinuousAttack(lastTargetEnemy, currentTargetCell));
                            }
                        }
                        else
                        {
                            StopAttack();
                        }
                    }
                }
            }
            UpdateOrientation();
        }
    }

    private void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0) && tilemap != null && !isDead)
        {
            if (Camera.main == null)
            {
                Debug.LogError("未找到主摄像机");
                return;
            }

            // 检查 UI 拦截
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("鼠标点击在 UI 上，忽略");
                return;
            }

            StopAttack();

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (!Camera.main.orthographic)
            {
                float zDistance = Mathf.Abs(Camera.main.transform.position.z - tilemap.transform.position.z);
                mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
            }
            Vector3Int cellPos = tilemap.WorldToCell(new Vector3(mousePos.x, mousePos.y, 0));
            Debug.Log($"点击格子: {cellPos}, 世界坐标: {mousePos}");

            if (tilemap.HasTile(cellPos) && !HasObstacle(tilemap.GetCellCenterWorld(cellPos)))
            {
                if (isMoving)
                {
                    isMoving = false;
                    path.Clear();
                    pathIndex = 0;
                    Debug.Log("当前路径被新点击中断");
                }

                path = FindPath(tilemap.WorldToCell(transform.position), cellPos);
                if (path != null && path.Count > 0)
                {
                    pathIndex = 0;
                    Vector3 targetWorldPos = tilemap.GetCellCenterWorld(path[pathIndex]);
                    targetPosition = ApplyPositionOffset(targetWorldPos);
                    //float currentX = transform.position.x;
                    //float targetX = targetPosition.x;
                    //skeletonScaleX = (targetX < currentX) ? -1f : 1f;
                    //if (monsterAnimator != null)
                    //{
                    //    monsterAnimator.skeleton.ScaleX = skeletonScaleX;
                    //}
                    pathIndex++;
                    isMoving = true;
                  //  ChangeAnimation("Walk");
                    Debug.Log($"找到路径，节点数: {path.Count}, 移动到: {targetPosition}, 路径: {string.Join(", ", path)}");
                }
                else
                {
                    Debug.Log("未找到有效路径");
                }
            }
            else
            {
                Debug.Log($"点击位置 {cellPos} 无瓦片或有障碍物，HasTile: {tilemap.HasTile(cellPos)}");
            }
        }
    }

    private void StopAttack()
    {
        if (currentAttackMode != null)
        {
            currentAttackMode.StopAttack();
        }
        isAutoAttacking = false;
        lastTargetCell = Vector3Int.zero;
        lastTargetEnemy = null;
        if (monsterAnimator != null && !isHurtAnimationPlaying)
        {
            ChangeAnimation("Idle");
        }
        Debug.Log("Attack stopped.");
    }

  

    

    public int CalculateGridDistance(Vector3Int start, Vector3Int goal, bool isAutoChess)
    {
        Tilemap currentTilemap = tilemap;
        if (currentTilemap == null)
        {
            Debug.LogError($"Tilemap is null (isAutoChess: {isAutoChess})");
            return int.MaxValue;
        }
        if (start == goal) return 0;

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, int> distances = new Dictionary<Vector3Int, int>();

        queue.Enqueue(start);
        visited.Add(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            int currentDistance = distances[current];

            foreach (Vector3 dir in hexDirections)
            {
                Vector3Int neighbor = currentTilemap.WorldToCell(currentTilemap.GetCellCenterWorld(current) + dir);
                if (!visited.Contains(neighbor) && currentTilemap.HasTile(neighbor) && !HasObstacle(currentTilemap.GetCellCenterWorld(neighbor), isAutoChess))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    distances[neighbor] = currentDistance + 1;

                    if (neighbor == goal)
                    {
                        Debug.Log($"Grid Distance from {start} to {goal}: {distances[neighbor]}");
                        return distances[neighbor];
                    }
                }
            }
        }

        Debug.Log($"No path found from {start} to {goal}");
        return int.MaxValue;
    }
    private void MoveToAttackRange(Vector3Int targetCell, bool isAutoChess = false)
    {
        Tilemap currentTilemap = tilemap;
        Vector3Int attackerCell = currentTilemap.WorldToCell(transform.position);
        float attackDistance = currentAttackMode != null ? currentAttackMode.GetAttackDistance() : 1f;

        List<Vector3Int> pathToRange = FindPathToAttackRange(attackerCell, targetCell, attackDistance, isAutoChess);
        if (pathToRange != null && pathToRange.Count > 0)
        {
            path = pathToRange;
            pathIndex = 0;
            Vector3 targetWorldPos = currentTilemap.GetCellCenterWorld(path[pathIndex]);
            targetPosition = ApplyPositionOffset(targetWorldPos);
            float currentX = transform.position.x;
            float targetX = targetPosition.x;
            skeletonScaleX = (targetX < currentX) ? -1f : 1f;
            if (monsterAnimator != null)
            {
                monsterAnimator.skeleton.ScaleX = skeletonScaleX;
            }
            pathIndex++;
            isMoving = true;
            ChangeAnimation("Walk");
            Debug.Log($"Moving to attack range: {targetPosition}");
        }
        else
        {
            Debug.Log("No path to attack range!");
            StopAttack();
        }
    }

    private List<Vector3Int> FindPathToAttackRange(Vector3Int start, Vector3Int target, float maxDistance, bool isAutoChess)
    {
        Tilemap currentTilemap = tilemap;
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> parents = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, int> distances = new Dictionary<Vector3Int, int>();

        queue.Enqueue(start);
        visited.Add(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            int currentDistance = distances[current];

            int targetDistance = CalculateGridDistance(current, target, true);
            if (targetDistance <= maxDistance && targetDistance > 0 && currentTilemap.HasTile(current))
            {
                List<Vector3Int> path = new List<Vector3Int>();
                Vector3Int node = current;
                while (parents.ContainsKey(node))
                {
                    path.Add(node);
                    node = parents[node];
                }
                path.Reverse();
                return path;
            }

            foreach (Vector3 dir in hexDirections)
            {
                Vector3Int neighbor = currentTilemap.WorldToCell(currentTilemap.GetCellCenterWorld(current) + dir);
                if (!visited.Contains(neighbor) && currentTilemap.HasTile(neighbor) && !HasObstacle(currentTilemap.GetCellCenterWorld(neighbor), isAutoChess))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    parents[neighbor] = current;
                    distances[neighbor] = currentDistance + 1;
                }
            }
        }

        return null;
    }

    private Vector3 ApplyPositionOffset(Vector3 position)
    {
        return new Vector3(position.x, position.y + spriteHeightOffset, position.z);
    }

    private List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        List<Node> openList = new List<Node>();
        HashSet<Vector3Int> closedList = new HashSet<Vector3Int>();
        Node startNode = new Node(start, 0, Heuristic(start, goal), null);
        openList.Add(startNode);

        int maxIterations = 1000;
        while (openList.Count > 0 && maxIterations > 0)
        {
            maxIterations--;
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
                Vector3Int neighborPos = tilemap.WorldToCell(tilemap.GetCellCenterWorld(current.cellPos) + dir);
                if (closedList.Contains(neighborPos) || !tilemap.HasTile(neighborPos) || HasObstacle(tilemap.GetCellCenterWorld(neighborPos)))
                {
                    continue;
                }

                float newGCost = current.gCost + 1;
                Node neighbor = new Node(neighborPos, newGCost, Heuristic(neighborPos, goal), current);
                bool inOpenList = false;

                foreach (Node openNode in openList)
                {
                    if (openNode.cellPos == neighborPos && openNode.gCost <= newGCost)
                    {
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

        Debug.Log("No path found after max iterations.");
        return null;
    }

    private float Heuristic(Vector3Int a, Vector3Int b)
    {
        Vector3 worldA = tilemap.GetCellCenterWorld(a);
        Vector3 worldB = tilemap.GetCellCenterWorld(b);
        return Vector3.Distance(worldA, worldB) / 0.866f;
    }

    public bool HasObstacle(Vector3 worldPos, bool isAutoChess = false)
    {
        Tilemap obstacleMap =  collisionTilemap;
        if (obstacleMap == null) return false;
        Vector3Int cellPos = obstacleMap.WorldToCell(worldPos);
        return obstacleMap.HasTile(cellPos);
    }

    private void UpdateTileHighlight()
    {
        // 检查必要组件
        if (tilemap == null || isDead)
        {
            if (highlightSprite != null) highlightSprite.enabled = false;
            isHighlightActive = false;
            Debug.LogWarning("Tilemap 未设置或玩家已死亡");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError("未找到主摄像机");
            return;
        }

        // 获取鼠标世界坐标
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // 对于正交摄像机，Z 轴通常不需要手动设置
        if (!Camera.main.orthographic)
        {
            float zDistance = Mathf.Abs(Camera.main.transform.position.z - tilemap.transform.position.z);
            mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
        }
        // 强制 Z 轴为 0 以匹配 2D Tilemap
        Vector3Int currentCell = tilemap.WorldToCell(new Vector3(mousePos.x, mousePos.y, 0));

        // 调试坐标信息
        Debug.Log($"鼠标屏幕坐标: {Input.mousePosition}, 世界坐标: {mousePos}, 格子坐标: {currentCell}, Tilemap Z: {tilemap.transform.position.z}, Cell Bounds: {tilemap.cellBounds}");

        // 检查 UI 拦截
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("鼠标在 UI 上，取消高亮");
            if (isHighlightActive)
            {
                tilemap.SetColor(lastHighlightedCell, originalColor);
                if (highlightSprite != null) highlightSprite.enabled = false;
                isHighlightActive = false;
            }
            return;
        }

        // 重置上一个高亮的格子
        if (isHighlightActive && currentCell != lastHighlightedCell)
        {
            tilemap.SetColor(lastHighlightedCell, originalColor);
            if (highlightSprite != null) highlightSprite.enabled = false;
            isHighlightActive = false;
        }

        // 检查格子是否有效且可移动
        if (tilemap.HasTile(currentCell) && !HasObstacle(tilemap.GetCellCenterWorld(currentCell)))
        {
            // 初始化 originalColor
            if (!isHighlightActive)
            {
                originalColor = tilemap.GetColor(currentCell);
                Debug.Log($"初始化格子颜色: {originalColor}");
            }

            // 设置高亮颜色
            Color targetColor = HasObstacle(tilemap.GetCellCenterWorld(currentCell)) ? obstacleHighlightColor : highlightColor;
            tilemap.SetColor(currentCell, targetColor);

            // 更新高亮精灵
            if (highlightSprite != null)
            {
                highlightSprite.transform.position = tilemap.GetCellCenterWorld(currentCell);
                highlightSprite.color = targetColor;
                highlightSprite.enabled = true;
                Debug.Log($"高亮精灵位置: {highlightSprite.transform.position}, 颜色: {targetColor}");
            }
            else
            {
                Debug.LogWarning("highlightSprite 未设置");
            }

            lastHighlightedCell = currentCell;
            isHighlightActive = true;
        }
        else
        {
            // 如果格子无效，重置高亮
            if (isHighlightActive)
            {
                tilemap.SetColor(lastHighlightedCell, originalColor);
                if (highlightSprite != null) highlightSprite.enabled = false;
                isHighlightActive = false;
            }
            Debug.Log($"格子 {currentCell} 无效或不可移动，HasTile: {tilemap.HasTile(currentCell)}, HasObstacle: {HasObstacle(tilemap.GetCellCenterWorld(currentCell))}");
        }
    }

    private void UpdateOrientation()
    {
        if (monsterAnimator == null || !isMoving) return;

        Vector3 moveDirection = targetPosition - transform.position;
        if (moveDirection.x < 0)
        {
            skeletonScaleX = -1f;
        }
        else if (moveDirection.x > 0)
        {
            skeletonScaleX = 1f;
        }
        monsterAnimator.skeleton.ScaleX = skeletonScaleX;
        Debug.Log($"Updated orientation: ScaleX = {skeletonScaleX}, MoveDirection = {moveDirection}");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            isMoving = false;
            transform.position = targetPosition;
            path.Clear();
            pathIndex = 0;
            if (!isHurtAnimationPlaying)
            {
                ChangeAnimation("Idle");
            }
            Debug.Log($"Collision with obstacle at: {collision.transform.position}");
        }
    }

    void OnDisable()
    {
        if (tilemap != null && isHighlightActive)
        {
            tilemap.SetColor(lastHighlightedCell, originalColor);
            isHighlightActive = false;
        }
        if (highlightSprite != null)
        {
            highlightSprite.enabled = false;
        }
        if (monsterAnimator != null && !isHurtAnimationPlaying)
        {
            ChangeAnimation("Idle");
        }
    }

    public void ChangeAnimation(string animationName)
    {
        if (monsterAnimator == null)
        {
            Debug.LogError($"Cannot change animation {animationName}: SkeletonAnimation component is null on {gameObject.name}");
            return;
        }

        bool isLoop = animationName != "Death" && animationName != "Hurt" && animationName != "Attack";
        monsterAnimator.skeleton.SetSkin("Side");
        monsterAnimator.skeleton.SetSlotsToSetupPose();
        monsterAnimator.skeleton.ScaleX = skeletonScaleX;
        try
        {
            monsterAnimator.AnimationState.SetAnimation(0, "Side_" + animationName, isLoop);
            Debug.Log($"Playing Side_{animationName} animation, ScaleX: {skeletonScaleX}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to play Side_{animationName} animation on {gameObject.name}: {ex.Message}");
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"Player {gameObject.name} health: {health}");

        if (health <= 0)
        {
            isDead = true;
            StopAttack();
            isHurtAnimationPlaying = false;
            ChangeAnimation("Death");
            Destroy(gameObject, 1f);
        }
        else
        {
            StartCoroutine(PlayHurtAnimation());
        }
    }

    private IEnumerator PlayHurtAnimation()
    {
        if (monsterAnimator == null)
        {
            Debug.LogError($"Cannot play hurt animation: SkeletonAnimation component is null on {gameObject.name}");
            yield break;
        }

        isHurtAnimationPlaying = true;
        ChangeAnimation("Hurt");
        yield return new WaitForSeconds(0.5f);
        isHurtAnimationPlaying = false;

        if (health > 0)
        {
            if (lastTargetEnemy != null && lastTargetCell != Vector3Int.zero)
            {
                Vector3Int attackerCell = GetCurrentTilemap().WorldToCell(transform.position);
                Vector3Int currentTargetCell = GetCurrentTilemap().WorldToCell(lastTargetEnemy.transform.position);
                if (currentAttackMode != null && currentAttackMode.IsWithinAttackDistance(attackerCell, currentTargetCell) && lastTargetEnemy.activeInHierarchy)
                {
                    isAutoAttacking = true;
                    StartCoroutine(ContinuousAttack(lastTargetEnemy, currentTargetCell));
                }
                else
                {
                    MoveToAttackRange(currentTargetCell);
                }
            }
            else
            {
                ChangeAnimation("Idle");
            }
        }
    }

    public void MoveTo(Vector3 targetPosition)
    {
        Vector3Int cellPos = tilemap.WorldToCell(targetPosition);
        if (tilemap.HasTile(cellPos) && !HasObstacle(tilemap.GetCellCenterWorld(cellPos)))
        {
            path = FindPath(tilemap.WorldToCell(transform.position), cellPos);
            if (path != null && path.Count > 0)
            {
                pathIndex = 0;
                Vector3 targetWorldPos = tilemap.GetCellCenterWorld(path[pathIndex]);
                targetPosition = ApplyPositionOffset(targetWorldPos);
                float currentX = transform.position.x;
                float targetX = targetPosition.x;
                skeletonScaleX = (targetX < currentX) ? -1f : 1f;
                if (monsterAnimator != null)
                {
                    monsterAnimator.skeleton.ScaleX = skeletonScaleX;
                }
                pathIndex++;
                isMoving = true;
                ChangeAnimation("Walk");
                Debug.Log($"Moving to: {targetPosition}, ScaleX = {skeletonScaleX}");
            }
        }
    }

    public void Attack(GameObject target, float damage)
    {
        PlayerMovement targetMovement = target.GetComponent<PlayerMovement>();
        Monster targetMonster = target.GetComponent<Monster>();
        if (targetMovement != null)
        {
            targetMovement.TakeDamage(damage);
            Debug.Log($"Player attacked player {target.name} for {damage} damage");
        }
        else if (targetMonster != null)
        {
            targetMonster.TakeDamage(damage);
            Debug.Log($"Player attacked monster {target.name} for {damage} damage");
        }
    }
}

