using Game.Combat;
using Game.Core;
using Game.Managers;
using Game.Network;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Game.Managers
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [SerializeField] private SpriteRenderer highlightSprite;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 0.8f);
        [SerializeField] private Color obstacleHighlightColor = new Color(1f, 0f, 0f, 0.8f);
        [SerializeField] private GameObject gridInfo; // 右键信息面板（Image + Text）
        [SerializeField] private TMP_Text TipsMouse;
        [SerializeField] private TMP_Text TipsPlayer;

  
        private Text gridInfoText;
        private PlayerHero playerHero;
        private Vector3Int lastHighlightedCell;
        private bool isHighlightActive = false;
        private Color originalColor;

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
            if (highlightSprite != null)
            {
                highlightSprite.enabled = false;
                highlightSprite.sortingLayerName = "Foreground";
                highlightSprite.sortingOrder = 10;
                if (highlightSprite.sprite == null)
                {
                    Debug.LogError($"highlightSprite 缺少 Sprite");
                }
            }
            else
            {
                Debug.LogError("highlightSprite 未在 InputManager Inspector 中分配");
            }

           

            if (gridInfo != null)
            {
                gridInfoText = gridInfo.GetComponentInChildren<Text>();
                if (gridInfoText == null)
                {
                    Debug.LogWarning("GridInfo 未包含 Text 组件");
                }
            }
            else
            {
                Debug.LogWarning("GridInfo 未在 InputManager Inspector 中分配");
            }

            
        }

        public void setTipsPlayerText(string text)
        {
            TipsPlayer.text = text;
        }

        //private void OnEnable()
        //{
        //    MapManager.OnMapChanged += UpdateReferences;
        //}

        //private void OnDisable()
        //{
        //    MapManager.OnMapChanged -= UpdateReferences;
        //}

        // 更新场景相关引用
        //private void UpdateReferences()
        //{
        //    if (tips == null || gridInfo == null)
        //    {
        //        Canvas canvas = FindObjectOfType<Canvas>();
        //        if (canvas != null)
        //        {
        //            GameObject[] uiObjects = canvas.GetComponentsInChildren<GameObject>();
        //            foreach (var obj in uiObjects)
        //            {
        //                if (obj.name == "Tips") tips = obj;
        //                if (obj.name == "GridInfo") gridInfo = obj;
        //            }
        //            if (tips != null)
        //            {
        //                Text[] texts = tips.GetComponentsInChildren<Text>();
        //                if (texts.Length >= 2)
        //                {
        //                    TipsMouse = texts[0];
        //                    TipsPlayer = texts[1];
        //                }
        //            }
        //            gridInfoText = gridInfo != null ? gridInfo.GetComponentInChildren<Text>() : null;
        //        }
        //    }
           
        //    ResetState();
        //    Debug.Log("InputManager 更新引用并重置状态");
        //}

        // 重置状态
        private void ResetState()
        {
            if (isHighlightActive && MapManager.Instance?.GetTilemap() != null)
            {
                MapManager.Instance.GetTilemap().SetColor(lastHighlightedCell, originalColor);
            }
            if (highlightSprite != null) highlightSprite.enabled = false;
            isHighlightActive = false;
            lastHighlightedCell = Vector3Int.zero;

            Debug.Log("InputManager 重置高亮和 UI 状态");
        }

        private void Update()
        {
            if (playerHero != null && !playerHero.IsDead())
            {
                UpdateTileHighlight();
                HandleMouseClick();
                HandleRightClick();
            }
            
        }

        private void UpdateTileHighlight()
        {
            if (playerHero == null || MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
            {
                Debug.LogWarning($"玩家或 MapManager 未初始化，玩家: {playerHero?.GetPlayerId()}");
                return;
            }

            if (Camera.main == null)
            {
                Debug.LogError("未找到主摄像机");
                return;
            }

            if (highlightSprite == null || highlightSprite.sprite == null)
            {
                Debug.LogError($"highlightSprite 未设置或缺少 Sprite");
                return;
            }

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (!Camera.main.orthographic)
            {
                float zDistance = Mathf.Abs(Camera.main.transform.position.z - MapManager.Instance.GetTilemap().transform.position.z);
                mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
            }
            Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(new Vector3(mousePos.x, mousePos.y, 0));

            if (TipsMouse != null)
            {
                TipsMouse.text = $"{currentCell.x} , {currentCell.y}";
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (isHighlightActive)
                {
                    MapManager.Instance.GetTilemap().SetColor(lastHighlightedCell, originalColor);
                    highlightSprite.enabled = false;
                    isHighlightActive = false;
                }
                //Debug.Log($"鼠标悬停 UI，重置高亮和 UI: lastHighlightedCell={lastHighlightedCell}");
                return;
            }

            if (isHighlightActive && currentCell != lastHighlightedCell)
            {
                MapManager.Instance.GetTilemap().SetColor(lastHighlightedCell, originalColor);
                highlightSprite.enabled = false;
                isHighlightActive = false;
            }

            if (MapManager.Instance.GetTilemap().HasTile(currentCell) || MapManager.Instance.GetCollisionTilemap().HasTile(currentCell))
            {
                if (!isHighlightActive)
                {
                    originalColor = MapManager.Instance.GetTilemap().HasTile(currentCell) ? MapManager.Instance.GetTilemap().GetColor(currentCell) : Color.white;
                }

                Color targetColor = GridUtility.HasObstacle(currentCell, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()) ? obstacleHighlightColor : highlightColor;
                if (MapManager.Instance.GetTilemap().HasTile(currentCell))
                {
                    MapManager.Instance.GetTilemap().SetColor(currentCell, targetColor);
                }

                highlightSprite.transform.position = MapManager.Instance.GetTilemap().GetCellCenterWorld(currentCell);
                highlightSprite.color = targetColor;
                highlightSprite.enabled = true;

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
                }

            //    Debug.Log($"鼠标悬停无效格子: {currentCell}");
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
                Debug.LogWarning("WebSocket 未连接");
                return;
            }
            if (playerHero == null || MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || playerHero.IsDead())
            {
                Debug.LogWarning($"点击无效: playerHero={playerHero}, MapManager={MapManager.Instance}, Tilemap={MapManager.Instance?.GetTilemap()?.name}, isDead={playerHero?.IsDead()}");
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {

                Debug.Log($"鼠标点击在 UI 上，忽略");
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
                if (playerHero.GetlastClickedCell() == cellPos)
                {
                    Debug.Log($"鼠标点击同一个坐标位置 {cellPos}，忽略点击");
                    return;
                }

                if (playerHero.IsMoving())
                {
                    playerHero.StopMoving();
                    Debug.Log("当前路径被新点击中断");
                }
                playerHero.MoveTo(cellPos);
                Debug.Log($"玩家 {playerHero.GetPlayerId()} 请求移动到 {cellPos}");
            }
            else
            {
                Debug.Log($"点击位置 {cellPos} 无瓦片或有障碍物");
            }
        }

        private void HandleRightClick()
        {
            if (!Input.GetMouseButtonDown(1))
            {
                return;
            }
            if (playerHero == null || MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || playerHero.IsDead())
            {
             
                Debug.LogWarning($"右键点击无效: playerHero={playerHero}, MapManager={MapManager.Instance}, Tilemap={MapManager.Instance?.GetTilemap()?.name}");
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // 获取当前点击的 UI 对象
                var pointerData = new UnityEngine.EventSystems.PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                var raycastResults = new List<UnityEngine.EventSystems.RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, raycastResults);

                bool isGridInfoClicked = false;
                foreach (var result in raycastResults)
                {
                    if (result.gameObject == gridInfo)
                    {
                        isGridInfoClicked = true;
                        break;
                    }
                }

                if (isGridInfoClicked && gridInfo != null)
                {
                    gridInfo.SetActive(false);
                    Debug.Log("右键点击 GridInfo 的 UI，隐藏信息面板");
                }
                
                return;
            }

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (!Camera.main.orthographic)
            {
                float zDistance = Mathf.Abs(Camera.main.transform.position.z - MapManager.Instance.GetTilemap().transform.position.z);
                mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
            }
            Vector3Int currentCell = MapManager.Instance.GetTilemap().WorldToCell(new Vector3(mousePos.x, mousePos.y, 0));

            if (MapManager.Instance.GetTilemap().HasTile(currentCell) || MapManager.Instance.GetCollisionTilemap().HasTile(currentCell))
            {
                UpdateGridInfoDisplay(currentCell);
            }
            else
            {
                Debug.Log($"右键点击无效格子: {currentCell}");
            }
        }

        private void UpdateGridInfoDisplay(Vector3Int cell)
        {
            if (gridInfo == null || gridInfoText == null)
            {
                Debug.LogWarning("gridInfo 或 gridInfoText 未设置，跳过信息显示");
                return;
            }

            WorldMapDataManager.Instance.QueryGridInfo(cell, (info) =>
            {
                string infoText = $"格子坐标: ({cell.x}, {cell.y}, {cell.z})\n";

                // 怪物信息
                if (info.monsterIds != null && info.monsterIds.Count > 0)
                {
                    Dictionary<string, int> monsterCounts = new Dictionary<string, int>();
                    foreach (var monsterId in info.monsterIds)
                    {
                        monsterCounts.TryGetValue(monsterId, out int count);
                        monsterCounts[monsterId] = count + 1;
                    }
                    List<string> monsterStrings = new List<string>();
                    foreach (var kvp in monsterCounts)
                    {
                        monsterStrings.Add($"{kvp.Key} x{kvp.Value}");
                    }
                    infoText += $"怪物: {string.Join(", ", monsterStrings)}\n";
                }
                else
                {
                    infoText += "怪物: 无\n";
                }

                // 宝物信息
                if (info.treasureItems != null && info.treasureItems.Count > 0)
                {
                    List<string> treasureStrings = new List<string>();
                    foreach (var item in info.treasureItems)
                    {
                        treasureStrings.Add($"{item.itemId} x{item.minQuantity}-{item.maxQuantity}");
                    }
                    infoText += $"宝物: {string.Join(", ", treasureStrings)}";
                }
                else
                {
                    infoText += "宝物: 无";
                }
                gridInfo.SetActive(true);
                gridInfoText.text = infoText;
                Debug.Log($"格子信息: {infoText}");
            });
        }

        public void SetPlayer(PlayerHero player)
        {
            playerHero = player;
            Debug.Log($"InputManager: 设置玩家 {player?.GetPlayerId()}");
        }
    }
}