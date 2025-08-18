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

        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 0.8f);
        [SerializeField] private Color obstacleHighlightColor = new Color(1f, 0f, 0f, 0.8f);
        [SerializeField] private GameObject gridInfo; // �Ҽ���Ϣ��壨Image + Text��
        [SerializeField] private TMP_Text TipsMouse;
        [SerializeField] private TMP_Text TipsPlayer;

        [SerializeField] private Button loginButton; // ��¼��ť
        [SerializeField] public TMP_InputField usernameInput; // ��ק�˺�����򵽴��ֶ�
        [SerializeField] public TMP_InputField passwordInput; // ��ק��������򵽴��ֶ�

        private SpriteRenderer highlightSprite; // ��̬����

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
            InitializeHighlightSprite(); // ��ʼ�� SpriteRenderer
            GetMouseSprite();

            if (gridInfo != null)
            {
                gridInfoText = gridInfo.GetComponentInChildren<Text>();
                if (gridInfoText == null)
                {
                    Debug.LogWarning("GridInfo δ���� Text ���");
                }
            }
            else
            {
                Debug.LogWarning("GridInfo δ�� InputManager Inspector �з���");
            }


        }

        // ��ʼ�� SpriteRenderer
        private void InitializeHighlightSprite()
        {
            GameObject highlightObj = new GameObject("HighlightSprite");
            highlightSprite = highlightObj.AddComponent<SpriteRenderer>();
            highlightSprite.enabled = false;
            highlightSprite.sortingLayerName = "Foreground";
            highlightSprite.sortingOrder = 10;
            DontDestroyOnLoad(highlightObj); // ���ֿ糡��
            Debug.Log("��̬���� HighlightSprite");
        }

        public void GetMouseSprite()
        {
            if (highlightSprite == null)
            {
                Debug.LogError("highlightSprite δ��ʼ�����������´���");
                InitializeHighlightSprite();
            }

            Sprite sprite = Resources.Load<Sprite>("Sprites/mouse");
            if (sprite != null)
            {
                highlightSprite.sprite = sprite;
                highlightSprite.enabled = false;
                Debug.Log("�ɹ����� Sprite: Sprites/mouse");
            }
            else
            {
                Debug.LogError("�޷����� Sprite: Sprites/mouse");
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

        // ���³����������
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
        //    Debug.Log("InputManager �������ò�����״̬");
        //}

        // ����״̬
        private void ResetState()
        {
            if (isHighlightActive && MapManager.Instance?.GetTilemap() != null)
            {
                MapManager.Instance.GetTilemap().SetColor(lastHighlightedCell, originalColor);
            }
            if (highlightSprite != null) highlightSprite.enabled = false;
            isHighlightActive = false;
            lastHighlightedCell = Vector3Int.zero;

            Debug.Log("InputManager ���ø����� UI ״̬");
        }

        private void Update()
        {
            UpdateTileHighlight();
            HandleMouseClick();
            //          HandleRightClick();
            HandleTabKeydown();

            HandleEnterKeydown();
        }

        private void HandleEnterKeydown()
        {
            // ���س�����Enter �� KeypadEnter��
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Debug.Log("��⵽�س���");
                // ����Ƿ��������ѡ��
                if (EventSystem.current.currentSelectedGameObject == usernameInput.gameObject ||
                    EventSystem.current.currentSelectedGameObject == passwordInput.gameObject)
                {
                    // ������¼��ť���
                    if (loginButton != null && loginButton.interactable)
                    {
                        loginButton.onClick.Invoke();
                        Debug.Log("�س���������¼��ť");
                    }
                    else
                    {
                        Debug.LogWarning("��¼��ťδ���û򲻿ɽ���");
                    }
                }
            }
        }

        private void UpdateTileHighlight()
        {
            if (highlightSprite == null ||  MapManager.Instance == null || MapManager.Instance.GetTilemap() == null || MapManager.Instance.GetCollisionTilemap() == null)
            {
              //  Debug.LogWarning($"��һ� MapManager δ��ʼ�������: {playerHero?.GetPlayerId()}");
                return;
            }

            if (Camera.main == null)
            {
                Debug.LogError("δ�ҵ��������");
                return;
            }

            //if (highlightSprite == null || highlightSprite.sprite == null)
            //{
            //    Debug.LogWarning($"highlightSprite δ���û�ȱ�� Sprite");
            //    return;
            //}

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
                //Debug.Log($"�����ͣ UI�����ø����� UI: lastHighlightedCell={lastHighlightedCell}");
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

            //    Debug.Log($"�����ͣ��Ч����: {currentCell}");
            }
        }

        private Vector3Int GetMouseClickPosition()
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (!Camera.main.orthographic)
            {
                float zDistance = Mathf.Abs(Camera.main.transform.position.z - MapManager.Instance.GetTilemap().transform.position.z);
                mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
            }
            return MapManager.Instance.GetTilemap().WorldToCell(new Vector3(mousePos.x, mousePos.y, 0));
        }

        private void HandleTabKeydown()
        {
            // ��� Tab ������
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Debug.LogWarning("tab key down");
                // ���������Ƿ���Ч
                if (usernameInput == null || passwordInput == null)
                {
                    Debug.LogWarning("�û��������������δ����");
                    return;
                }

                // ��ȡ��ǰѡ�е� UI Ԫ��
                GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

                // ���û��ѡ���κ������Ĭ��ѡ���û��������
                if (currentSelected == null)
                {
                    usernameInput.Select();
                    return;
                }

                // �л��߼�
                if (currentSelected == usernameInput.gameObject)
                {
                    passwordInput.Select();
                }
                else // ���������������������
                {
                    usernameInput.Select();
                }
            }
        }

        private void HandleMouseClick()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }
            if (!WebSocketManager.Instance.IsConnected)
            {
                Debug.LogWarning("WebSocket δ����");
                return;
            }
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {

                Debug.Log($"������� UI �ϣ�����");
                return;
            }

            //����Ѿ����ߣ���������ƶ�
            if (GameManager.Instance.GetOnline())
            {
                Vector3Int cellPos = GetMouseClickPosition();

                if (MapManager.Instance.GetTilemap().HasTile(cellPos) && !GridUtility.HasObstacle(cellPos, MapManager.Instance.GetTilemap(), MapManager.Instance.GetCollisionTilemap()))
                {
                    if (PlayerManager.Instance.GetLocalPlayer().GetlastClickedCell() == cellPos)
                    {
                        Debug.Log($"�����ͬһ������λ�� {cellPos}�����Ե��");
                        return;
                    }

                    if (PlayerManager.Instance.GetLocalPlayer().IsMoving())
                    {
                        PlayerManager.Instance.GetLocalPlayer().ChangeMove();
                        Debug.Log("��ǰ·�����µ���ж�");
                    }
                    PlayerManager.Instance.GetLocalPlayer().MoveTo(cellPos);
                    Debug.Log($"��� {PlayerManager.Instance.GetLocalPlayer().GetPlayerId()} �����ƶ��� {cellPos}");
                }
                else
                {
                    Debug.Log($"���λ�� {cellPos} ����Ƭ�����ϰ���");
                }
            }
            //���δ���ߴ����߼�
            else if(GameManager.Instance.GetLoginStatus() && UIManager.Instance.isCreateCharacter == false)
            {
                Vector3Int cellPos = GetMouseClickPosition();
                Debug.Log($"��ǰ���λ�ã�{cellPos}");
                if (cellPos == new Vector3Int(-4, 2, 0))
                {
                    CharacterManager.Instance.SetSelectCharacter(0);
                }
                else if (cellPos == new Vector3Int(-2, 2, 0))
                {
                    CharacterManager.Instance.SetSelectCharacter(1);
                }
                else if (cellPos == new Vector3Int(0, 2, 0))
                {
                    CharacterManager.Instance.SetSelectCharacter(2);
                }
                else if (cellPos == new Vector3Int(2, 2, 0))
                {
                    CharacterManager.Instance.SetSelectCharacter(3);
                }
                else if (cellPos == new Vector3Int(4, 2, 0))
                {
                    CharacterManager.Instance.SetSelectCharacter(4);
                }
               
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
             
                Debug.LogWarning($"�Ҽ������Ч: playerHero={playerHero}, MapManager={MapManager.Instance}, Tilemap={MapManager.Instance?.GetTilemap()?.name}");
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // ��ȡ��ǰ����� UI ����
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
                    Debug.Log("�Ҽ���� GridInfo �� UI��������Ϣ���");
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
                Debug.Log($"�Ҽ������Ч����: {currentCell}");
            }
        }

        private void UpdateGridInfoDisplay(Vector3Int cell)
        {
            if (gridInfo == null || gridInfoText == null)
            {
                Debug.LogWarning("gridInfo �� gridInfoText δ���ã�������Ϣ��ʾ");
                return;
            }

            WorldMapDataManager.Instance.QueryGridInfo(cell, (info) =>
            {
                string infoText = $"��������: ({cell.x}, {cell.y}, {cell.z})\n";

                // ������Ϣ
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
                    infoText += $"����: {string.Join(", ", monsterStrings)}\n";
                }
                else
                {
                    infoText += "����: ��\n";
                }

                // ������Ϣ
                if (info.treasureItems != null && info.treasureItems.Count > 0)
                {
                    List<string> treasureStrings = new List<string>();
                    foreach (var item in info.treasureItems)
                    {
                        treasureStrings.Add($"{item.itemId} x{item.minQuantity}-{item.maxQuantity}");
                    }
                    infoText += $"����: {string.Join(", ", treasureStrings)}";
                }
                else
                {
                    infoText += "����: ��";
                }
                gridInfo.SetActive(true);
                gridInfoText.text = infoText;
                Debug.Log($"������Ϣ: {infoText}");
            });
        }

        public void SetPlayer(PlayerHero player)
        {
            playerHero = player;
            Debug.Log($"InputManager: ������� {player?.GetPlayerId()}");
        }
    }
}