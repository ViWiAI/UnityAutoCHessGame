using Game.Animation;
using Game.Combat;
using Game.Managers;
using Game.Network;
using Newtonsoft.Json;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Game.Core
{
    public class PlayerHero : Hero
    {
        [SerializeField] private SpriteRenderer highlightSprite;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 0.8f);
        [SerializeField] private Color obstacleHighlightColor = new Color(1f, 0f, 0f, 0.8f);
        [SerializeField] private HeroJobs currentJob; // 新增：当前玩家的职业
        public static PlayerHero Instance { get; private set; }
        private string playerId = "Player_001"; // 动态分配
        private string teamId; // 队伍 ID
        private Vector3Int currentPosition;
        private Vector3Int lastHighlightedCell;
        private bool isHighlightActive = false;
        private Color originalColor; // Tilemap原始颜色
        private float lastSkeletonScaleX = 1f; // 新增：记录上一次的朝向

        protected override void Awake()
        {
            base.Awake();
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            if (string.IsNullOrEmpty(currentMapId))
            {
                currentMapId = SceneManager.GetActiveScene().name; // 默认使用场景名称
            }
        }

        protected override void Start()
        {
            //base.Start();
            if (highlightSprite != null) highlightSprite.enabled = false;

            Debug.Log($"PlayerHero Start called for: {gameObject.name}, Active: {gameObject.activeInHierarchy}");
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Debug.Log($"SpriteRenderer enabled for {gameObject.name}: {renderer.enabled}");
            }
            //  SetJob(job);
            //UIManager.Instance.ShowSkillButton(this);
            // WebSocketManager.Instance.OnMessageReceived += HandleServerMessage;
            //RequestMapData();
        }

        protected override void Update()
        {
            if (isDead) return;

            UpdateTileHighlight();
            HandleMouseClick();
        }

        protected override void FixedUpdate()
        {
            if (isMoving && !isDead)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, this.stats.moveSpeed * Time.fixedDeltaTime);
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
                            ChangeAnimation("Idle");
                        }
                        path.Clear();
                        pathIndex = 0;

                   
                    }
                }
                UpdateOrientation();
            }
        }

        private void OnDestroy()
        {
            var mapManager = MapManager.GetMapManager(currentMapId);
            if (mapManager != null)
            {
                mapManager.OnMapLoaded -= OnMapInitialized;
            }
        }

        private void OnMapInitialized(MapManager mapManager)
        {
            currentMapId = mapManager.GetMapId();
            SetTilemap(mapManager.GetTilemap(), mapManager.GetCollisionTilemap());
            Debug.Log($"PlayerHero {gameObject.name} 初始化到地图: {currentMapId}, Type: {mapManager.GetMapType()}, Style: {mapManager.GetMapStyle()}");
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

                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (!Camera.main.orthographic)
                {
                    float zDistance = Mathf.Abs(Camera.main.transform.position.z - tilemap.transform.position.z);
                    mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
                }
                Vector3Int cellPos = tilemap.WorldToCell(new Vector3(mousePos.x, mousePos.y, 0));
            //    Debug.Log($"点击格子: {cellPos}, 世界坐标: {mousePos}");

                if (tilemap.HasTile(cellPos) && !GridUtility.HasObstacle(cellPos, tilemap, collisionTilemap))
                {
                    if (isMoving)
                    {
                        isMoving = false;
                        path.Clear();
                        pathIndex = 0;
                        Debug.Log("当前路径被新点击中断");
                    }

                    // 调用 MoveTo 方法，统一处理移动逻辑
                    MoveTo(cellPos);
                }
                else
                {
                    Debug.Log($"点击位置 {cellPos} 无瓦片或有障碍物，HasTile: {tilemap.HasTile(cellPos)}");
                  //  UIManager.Instance.ShowMessage("无法移动到此位置！");
                }
            }
        }

        public override void MoveTo(Vector3Int cellPos)
        {
            if (tilemap == null || collisionTilemap == null)
            {
                Debug.LogError("Tilemap 或 collisionTilemap 未设置");
                return;
            }

            if (tilemap.HasTile(cellPos) && !GridUtility.HasObstacle(cellPos, tilemap, collisionTilemap))
            {
                Vector3Int currentCell = tilemap.WorldToCell(transform.position);
                List<Vector3Int> path = FindPath(currentCell, cellPos);
                if (path != null && path.Count > 0)
                {
                    this.path = path;
                    pathIndex = 0;
                    isMoving = true;
                    // 直接设置第一个目标位置
                    targetPosition = ApplyPositionOffset(tilemap.GetCellCenterWorld(path[pathIndex]));
                    UpdateOrientation();
                    ChangeAnimation("Walk");
                    Debug.Log($"找到路径，节点数: {path.Count}, 移动到: {targetPosition}, 路径: {string.Join(", ", path)}");
                }
                else
                {
                    Debug.Log($"未找到到 {cellPos} 的有效路径");
                   // UIManager.Instance.ShowMessage("无法到达目标位置！");
                }
            }
            else
            {
                Debug.Log($"目标格子 {cellPos} 无效或有障碍物");
               // UIManager.Instance.ShowMessage("无法移动到此位置！");
            }
        }

        public override void UpdateOrientation()
        {
            if (!isMoving || path == null || pathIndex >= path.Count)
            {
                return; // 如果没有移动或路径无效，直接返回
            }

            Vector3 targetWorldPos = tilemap.GetCellCenterWorld(path[pathIndex]);
            Vector3 direction = targetWorldPos - transform.position;

            PlayerAnimator animator = GetComponent<PlayerAnimator>();
            if (animator != null)
            {
                animator.SetOrientation(direction);
                lastSkeletonScaleX = animator.characterSkeleton != null ? animator.characterSkeleton.skeleton.ScaleX : lastSkeletonScaleX; // 记录朝向
            }
            else
            {
                Debug.LogWarning($"未找到 PlayerAnimator 组件，无法调整朝向，对象: {gameObject.name}");
            }

            Debug.Log($"调整朝向: 目标位置 {targetWorldPos}, 方向 {direction}, lastSkeletonScaleX: {lastSkeletonScaleX}");
        }

        // 新增：设置职业并同步到 GearEquipper 和 PlayerAnimator
        public void SetJob(HeroJobs newJob)
        {
            if (currentJob != newJob)
            {
                currentJob = newJob;
                //GearEquipper gearEquipper = GetComponent<GearEquipper>();
                //if (gearEquipper != null)
                //{
                //    gearEquipper.Job = newJob;
                //}
                PlayerAnimator animator = GetComponent<PlayerAnimator>();
                if (animator != null)
                {
                    animator.JobChanged(newJob);
                }
                Debug.Log($"PlayerHero 职业设置为: {newJob}");
            }
        }

        // 新增：获取当前职业
        public HeroJobs GetJob()
        {
            return currentJob;
        }

        protected override void ChangeAnimation(string animationName)
        {
            PlayerAnimator animator = GetComponent<PlayerAnimator>();
            if (animator != null)
            {
                animator.ChangeAnimation(animationName, currentJob);
            }
            else
            {
                Debug.LogWarning($"未找到 PlayerAnimator 组件，无法播放动画: {animationName}");
            }
        }


        private List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
        {
            // 如果目标格子与起始格子相同，直接返回空路径
            if (start == goal)
            {
                Debug.Log("目标格子与当前位置相同，无需移动");
                return new List<Vector3Int>();
            }

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
                    // 返回路径，排除起始格子
                    return path.Count > 1 ? path.GetRange(1, path.Count - 1) : path;
                }

                foreach (Vector3 dir in hexDirections)
                {
                    Vector3Int neighborPos = tilemap.WorldToCell(tilemap.GetCellCenterWorld(current.cellPos) + dir);
                    if (closedList.Contains(neighborPos) || !tilemap.HasTile(neighborPos) || GridUtility.HasObstacle(neighborPos, tilemap, collisionTilemap))
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

            Debug.Log("未找到路径，超出最大迭代次数或无有效路径");
            return null;
        }

        private float Heuristic(Vector3Int a, Vector3Int b)
        {
            Vector3 worldA = tilemap.GetCellCenterWorld(a);
            Vector3 worldB = tilemap.GetCellCenterWorld(b);
            return Vector3.Distance(worldA, worldB) / 0.866f;
        }

        private void UpdateTileHighlight()
        {
            // 检查必要组件
            if (tilemap == null || collisionTilemap == null || isDead)
            {
                if (highlightSprite != null) highlightSprite.enabled = false;
                isHighlightActive = false;
                Debug.LogWarning("Tilemap、collisionTilemap 未设置或玩家已死亡");
                return;
            }

            if (Camera.main == null)
            {
                Debug.LogError("未找到主摄像机");
                return;
            }

            // 获取鼠标世界坐标
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (!Camera.main.orthographic)
            {
                float zDistance = Mathf.Abs(Camera.main.transform.position.z - tilemap.transform.position.z);
                mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
            }
            Vector3Int currentCell = tilemap.WorldToCell(new Vector3(mousePos.x, mousePos.y, 0));

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

            // 检查格子是否有效（tilemap 上有瓦片）
            if (tilemap.HasTile(currentCell) || collisionTilemap.HasTile(currentCell))
            {
                // 初始化 originalColor
                if (!isHighlightActive)
                {
                    originalColor = tilemap.HasTile(currentCell) ? tilemap.GetColor(currentCell) : Color.white;
                //    Debug.Log($"初始化格子颜色: {originalColor} for cell {currentCell}");
                }

                // 根据 collisionTilemap 判断是否为障碍物
                Color targetColor = GridUtility.HasObstacle(currentCell, tilemap, collisionTilemap) ? obstacleHighlightColor : highlightColor;
                if (tilemap.HasTile(currentCell))
                {
                    tilemap.SetColor(currentCell, targetColor);
                }

                // 更新高亮精灵
                if (highlightSprite != null)
                {
                    highlightSprite.transform.position = tilemap.GetCellCenterWorld(currentCell);
                    highlightSprite.color = targetColor;
                    highlightSprite.enabled = true;
                 //   Debug.Log($"高亮精灵位置: {highlightSprite.transform.position}, 颜色: {targetColor}, 障碍物: {GridUtility.HasObstacle(currentCell, tilemap, collisionTilemap)}");
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
                // 如果格子无效（tilemap 和 collisionTilemap 均无瓦片），重置高亮
                if (isHighlightActive)
                {
                    tilemap.SetColor(lastHighlightedCell, originalColor);
                    if (highlightSprite != null) highlightSprite.enabled = false;
                    isHighlightActive = false;
                }
              //  Debug.Log($"格子 {currentCell} 无效，HasTile (tilemap): {tilemap.HasTile(currentCell)}, HasTile (collisionTilemap): {collisionTilemap.HasTile(currentCell)}");
            }
        }

        private bool IsValidMoveTarget(Vector3Int cellPos)
        {
            // 检查格子是否存在且无障碍物
            bool hasTile = tilemap.HasTile(cellPos);
            bool hasObstacle = GridUtility.HasObstacle(cellPos, tilemap, collisionTilemap);
            if (!hasTile || hasObstacle)
            {
                Debug.Log($"格子 {cellPos} 无效: HasTile={hasTile}, HasObstacle={hasObstacle}");
                return false;
            }

            // 检查移动距离
            int maxMoveDistance = 5; // 示例：最大移动距离
            Vector3Int playerPos = tilemap.WorldToCell(transform.position);
            int distance = Mathf.Abs(cellPos.x - playerPos.x) + Mathf.Abs(cellPos.y - playerPos.y);
            if (distance > maxMoveDistance)
            {
                Debug.Log($"格子 {cellPos} 超出移动范围，距离: {distance}");
                return false;
            }

            return true;
        }

        public string GetPlayerId() => playerId;
        public string GetTeamId() => teamId;

        public void JoinTeam(string newTeamId)
        {
            teamId = newTeamId;
            WebSocketManager.Instance.Send(new Dictionary<string, object>
            {
                { "type", "team_join" },
                { "player_id", playerId },
                { "team_id", teamId }
            });
        }


        public void RequestPVPMatch(string mode)
        {
            WebSocketManager.Instance.Send(new Dictionary<string, object>
            {
                { "type", "pvp_match" },
                { "player_id", playerId },
                { "team_id", teamId },
                { "mode", mode }
            });
        }

        public void UpdatePosition(string mapId)
        {
            currentMapId = mapId;
            var mapManager = MapManager.GetMapManager(mapId);
            if (mapManager != null)
            {
                SetTilemap(mapManager.GetTilemap(), mapManager.GetCollisionTilemap());
                // 根据地图类型调整行为
                if (mapManager.GetMapType() == MapManager.MapType.PVPMap)
                {
                    Debug.Log($"PlayerHero {gameObject.name}: 进入 PVP 模式，禁用某些交互");
                }
            }
            else
            {
                Debug.LogWarning($"PlayerHero {gameObject.name}: 未找到 mapId {mapId} 的 MapManager");
            }
        }


        private void HandleServerMessage(Dictionary<string, object> data)
        {
            if (data.ContainsKey("player_id") && data["player_id"].ToString() != playerId)
            {
                return;
            }
            string type = data["type"].ToString();
            switch (type)
            {
                case "player_map":
                    currentMapId = data["map_id"].ToString();
                    var position = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonConvert.SerializeObject(data["position"]));
                    currentPosition = new Vector3Int(position["x"], position["y"], position["z"]);
                    var mapManager = MapManager.GetMapManager(currentMapId);
                    if (mapManager != null)
                    {
                        transform.position = mapManager.GetTilemap().GetCellCenterWorld(currentPosition);
                        SetTilemap(mapManager.GetTilemap(), mapManager.GetCollisionTilemap());
                    }
                    else
                    {
                        Debug.LogError($"PlayerHero {gameObject.name}: 未找到 mapId {currentMapId} 的 MapManager");
                    }
                    break;
                case "team_created":
                case "team_joined":
                    teamId = data["team_id"].ToString();
                    UIManager.Instance.UpdateTeamUI(data["team_members"] as List<object>);
                    break;
                case "battle_start":
                    string battleMapId = data["battle_map_id"].ToString();
                    string battleRoomId = data["battle_room_id"].ToString();
                    MapManager.GetMapManager(battleMapId)?.SwitchMap(battleMapId, battleRoomId);
                    List<Hero> enemies = new List<Hero>();
                    foreach (var enemyData in data["monsters"] as List<object>)
                    {
                        //string monsterId = enemyData.ToString();
                        //enemies.Add(InstantiateMonster(monsterId));
                    }
                    BattleManager.Instance.StartBattle(this, enemies, battleMapId, battleRoomId, data["team_members"] as List<object>);
                    break;
                case "battle_countdown":
                    int countdown = int.Parse(data["countdown"].ToString());
                    UIManager.Instance.ShowCountdown(countdown);
                    break;
                case "pvp_match":
                    string pvpMapId = data["pvp_map_id"].ToString();
                    string pvpRoomId = data["pvp_room_id"].ToString();
                    MapManager.GetMapManager(pvpMapId)?.SwitchMap(pvpMapId, pvpRoomId);
                    List<Hero> opponents = new List<Hero>();
                    foreach (var opponentData in data["opponents"] as List<object>)
                    {
                        string opponentId = opponentData.ToString();
                        //opponents.Add(InstantiatePlayer(opponentId));
                    }
                    //BattleManager.Instance.StartPVP(this, opponents, pvpMapId, pvpRoomId, data["team_members"] as List<object>);
                    break;
                case "siege_start":
                    string siegeMapId = data["siege_map_id"].ToString();
                    string siegeRoomId = data["siege_room_id"].ToString();
                    MapManager.GetMapManager(siegeMapId)?.SwitchMap(siegeMapId, siegeRoomId);
                    List<Hero> defenders = new List<Hero>();
                    foreach (var defenderData in data["defenders"] as List<object>)
                    {
                        string defenderId = defenderData.ToString();
                        //defenders.Add(InstantiatePlayer(defenderId));
                    }
                    //BattleManager.Instance.StartSiege(this, defenders, siegeMapId, siegeRoomId, data["team_members"] as List<object>);
                    break;
            }
        }

        private void RequestMapData()
        {
            WebSocketManager.Instance.Send(new Dictionary<string, object>
            {
                { "type", "get_player_map" },
                { "player_id", playerId }
            });
        }

        private void HandleMouseInput()
        {
            if (string.IsNullOrEmpty(currentMapId)) return;
            var mapManager = MapManager.GetMapManager(currentMapId);
            if (mapManager == null)
            {
                Debug.LogWarning($"PlayerHero {gameObject.name}: 未找到 mapId {currentMapId} 的 MapManager");
                return;
            }
            if (mapManager.GetMapType() == MapManager.MapType.PVPMap)
            {
                return;
            }
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = mapManager.GetTilemap().WorldToCell(mousePos);

            if (Input.GetMouseButtonDown(1))
            {
                string result = mapManager.GetTreasureInfo(cellPos);
                UIManager.Instance.ShowTreasurePrompt(result);
            }
            else if (Input.GetMouseButtonDown(0))
            {
                MoveTo(cellPos);
            }
        }
    }
}