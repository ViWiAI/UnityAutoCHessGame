using Game.Animation;
using Game.Core;
using Game.Managers;
using Game.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private SpriteRenderer highlightSprite; // 迁移自 PlayerHero
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 0.8f); // 迁移自 PlayerHero
    [SerializeField] private Color obstacleHighlightColor = new Color(1f, 0f, 0f, 0.8f); // 迁移自 PlayerHero
    private PlayerHero playerHero;
    private string currentMapId;
    private Vector3Int lastHighlightedCell; // 迁移自 PlayerHero
    private bool isHighlightActive = false; // 迁移自 PlayerHero
    private Color originalColor; // 迁移自 PlayerHero

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(currentMapId))
        {
            currentMapId = SceneManager.GetActiveScene().name;
        }
        InitializeLocalPlayer(GridUtility.GenerateRandomPlayerId(), HeroJobs.Warrior); // 示例初始化
    }

    private void Update()
    {
        if (playerHero != null && !playerHero.IsDead())
        {
            UpdateTileHighlight();
            HandleMouseClick();
        }
    }

    private void InitializeLocalPlayer(string playerId, HeroJobs job)
    {
        if (playerHero != null)
        {
            Debug.LogWarning($"本地玩家已存在: {playerId}");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("playerPrefab 未设置");
            return;
        }

        if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null)
        {
            Debug.LogError($"MapManager 或 Tilemap 未初始化，场景: {currentMapId}");
            return;
        }

        Vector3 worldPos = MapManager.Instance.GetTilemap().GetCellCenterWorld(Vector3Int.zero);
        GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        playerObj.name = $"Player_{playerId}";
        playerHero = playerObj.GetComponent<PlayerHero>();
        if (playerHero == null)
        {
            Debug.LogError("playerPrefab 缺少 PlayerHero 组件");
            Destroy(playerObj);
            return;
        }

        playerHero.Initialize(playerId, true, job);
        playerHero.SetCurrentMapId(currentMapId);

        // 检查 highlightSprite
        if (highlightSprite != null)
        {
            highlightSprite.enabled = false;
            highlightSprite.sortingLayerName = "Foreground";
            highlightSprite.sortingOrder = 10;
            if (highlightSprite.sprite == null)
            {
                Debug.LogError($"highlightSprite (MouseImg) 缺少 Sprite，玩家: {playerId}");
            }
            else
            {
                Debug.Log($"highlightSprite 初始化: 名称={highlightSprite.name}, Sprite={highlightSprite.sprite.name}, SortingLayer=Foreground, OrderInLayer=10");
            }
        }
        else
        {
            Debug.LogError($"highlightSprite 未在 GameManager Inspector 中分配，玩家: {playerId}");
        }

        Debug.Log($"本地玩家初始化: {playerId}, job: {job}, position: {worldPos}, tilemap={MapManager.Instance.GetTilemap()?.name}");
    }

    private void UpdateTileHighlight()
    {
        if (playerHero == null)
        {
            Debug.LogWarning("playerHero 未初始化");
            return;
        }

        if (MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
        {
            if (highlightSprite != null) highlightSprite.enabled = false;
            isHighlightActive = false;
            Debug.LogWarning($"MapManager 或 Tilemap 未初始化，玩家: {playerHero.GetPlayerId()}");
            return;
        }

        if (playerHero.IsDead())
        {
            if (highlightSprite != null) highlightSprite.enabled = false;
            isHighlightActive = false;
            Debug.LogWarning($"玩家已死亡: isDead={playerHero.IsDead()}");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError("未找到主摄像机");
            return;
        }

        if (highlightSprite == null || highlightSprite.sprite == null)
        {
            Debug.LogError($"highlightSprite 未设置或缺少 Sprite: highlightSprite={highlightSprite?.name}, Sprite={highlightSprite?.sprite?.name}");
            isHighlightActive = false;
            return;
        }

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (!Camera.main.orthographic)
        {
            float zDistance = Mathf.Abs(Camera.main.transform.position.z - MapManager.Instance.GetTilemap().transform.position.z);
            mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
        }
        Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(new Vector3(mousePos.x, mousePos.y, 0));

        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("鼠标在 UI 上，取消高亮");
            if (isHighlightActive)
            {
                MapManager.Instance.GetTilemap().SetColor(lastHighlightedCell, originalColor);
                highlightSprite.enabled = false;
                isHighlightActive = false;
                Debug.Log($"重置高亮: lastHighlightedCell={lastHighlightedCell}, originalColor={originalColor}");
            }
            return;
        }

        if (isHighlightActive && currentCell != lastHighlightedCell)
        {
            MapManager.Instance.GetTilemap().SetColor(lastHighlightedCell, originalColor);
            highlightSprite.enabled = false;
            isHighlightActive = false;
          //  Debug.Log($"重置高亮: lastHighlightedCell={lastHighlightedCell}, originalColor={originalColor}");
        }

        if (MapManager.Instance.GetTilemap().HasTile(currentCell) || MapManager.Instance.GetCollisionTilemap().HasTile(currentCell))
        {
            if (!isHighlightActive)
            {
                originalColor = MapManager.Instance.GetTilemap().HasTile(currentCell) ? MapManager.Instance.GetTilemap().GetColor(currentCell) : Color.white;
              //  Debug.Log($"初始化格子颜色: {originalColor} for cell {currentCell}");
            }

            Color targetColor = GridUtility.HasObstacle(currentCell, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()) ? obstacleHighlightColor : highlightColor;
            if (MapManager.Instance.GetTilemap().HasTile(currentCell))
            {
                MapManager.Instance.GetTilemap().SetColor(currentCell, targetColor);
              //  Debug.Log($"Tilemap 高亮: cell={currentCell}, color={targetColor}, tilemap={MapManager.Instance.GetTilemap().name}");
            }

            highlightSprite.transform.position = MapManager.Instance.GetTilemap().GetCellCenterWorld(currentCell);
            highlightSprite.color = targetColor;
            highlightSprite.enabled = true;
          //  Debug.Log($"高亮精灵: 位置={highlightSprite.transform.position}, 名称={highlightSprite.name}, 颜色={targetColor}, 障碍物={GridUtility.HasObstacle(currentCell, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap())}, SortingLayer={highlightSprite.sortingLayerName}, OrderInLayer={highlightSprite.sortingOrder}, Sprite={highlightSprite.sprite.name}, Enabled={highlightSprite.enabled}");

            lastHighlightedCell = currentCell;
            isHighlightActive = true;
        }
        else
        {
            if (isHighlightActive)
            {
                MapManager.Instance.GetTilemap().SetColor(lastHighlightedCell, originalColor);
                highlightSprite.enabled = false;
                isHighlightActive = false;
                Debug.Log($"重置高亮: 格子 {currentCell} 无效");
            }
           // Debug.Log($"格子 {currentCell} 无效，HasTile (tilemap): {MapManager.Instance.GetTilemap().HasTile(currentCell)}, HasTile (collisionTilemap): {MapManager.Instance.GetCollisionTilemap().HasTile(currentCell)}");
        }
    }

    private void HandleMouseClick()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }
        if (!WebSocketManager.Instance.IsConnected())
        {
            Debug.LogWarning("websocket is not connect");
            return;
        }
        if (playerHero == null || MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || playerHero.IsDead())
        {
            Debug.LogWarning($"点击无效: playerHero={playerHero}, MapManager={MapManager.Instance}, Tilemap={MapManager.Instance?.GetTilemap()?.name}, isDead={playerHero?.IsDead()}");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError("未找到主摄像机");
            return;
        }

        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("鼠标点击在 UI 上，忽略");
            return;
        }

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (!Camera.main.orthographic)
        {
            float zDistance = Mathf.Abs(Camera.main.transform.position.z - MapManager.Instance.GetTilemap().transform.position.z);
            mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
        }
        Vector3Int cellPos = MapManager.Instance.GetTilemap().WorldToCell(new Vector3(mousePos.x, mousePos.y, 0));

        if (MapManager.Instance.GetTilemap().HasTile(cellPos) && !GridUtility.HasObstacle(cellPos, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()))
        {
            if (playerHero.IsMoving())
            {
                playerHero.StopMoving();
                Debug.Log("当前路径被新点击中断");
            }
            Debug.Log("playerHero.MoveTo 调用");
            playerHero.MoveTo(cellPos);
        }
        else
        {
            Debug.Log($"点击位置 {cellPos} 无瓦片或有障碍物");
        }
    }

    public void EnterBattle(string battleMapId, string battleRoomId)
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError($"MapManager 单例未初始化，无法切换到战斗地图: {battleMapId}");
            return;
        }
        MapManager.Instance.SwitchMap(battleMapId, battleRoomId);
        currentMapId = battleMapId;
        if (playerHero != null)
        {
            playerHero.SetCurrentMapId(battleMapId);
        }
        Debug.Log($"GameManager: 进入战斗地图 {battleMapId}, 房间: {battleRoomId}");
    }
}