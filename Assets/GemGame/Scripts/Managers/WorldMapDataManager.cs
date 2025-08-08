using Game.Core;
using Game.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Managers
{
    public class WorldMapDataManager : MonoBehaviour
    {
        public static WorldMapDataManager Instance { get; private set; }

        // ģ�����ݿ�ĸ�������
        [System.Serializable]
        public class GridInfo
        {
            public Vector3Int cell;
            public List<string> monsterIds; // ���� ID �б�
            public List<DropItem> treasureItems; // �����б�
        }

        [SerializeField] private List<GridInfo> gridData; // Inspector ����ģ������

        private Dictionary<Vector3Int, GridInfo> gridInfoDict;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGridData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeGridData()
        {
            gridInfoDict = new Dictionary<Vector3Int, GridInfo>();
            foreach (var info in gridData)
            {
                if (!gridInfoDict.ContainsKey(info.cell))
                {
                    gridInfoDict[info.cell] = info;
                }
            }
        }

        // ��ѯ������Ϣ
        public GridInfo GetGridInfo(Vector3Int cell)
        {
            if (gridInfoDict.TryGetValue(cell, out GridInfo info))
            {
                return info;
            }
            return new GridInfo { cell = cell, monsterIds = new List<string>(), treasureItems = new List<DropItem>() };
        }

        // ģ�����ݿ��ѯ�����滻Ϊ WebSocketManager ���ã�
        public void QueryGridInfo(Vector3Int cell, System.Action<GridInfo> callback)
        {
            // ģ���첽��ѯ
            GridInfo info = GetGridInfo(cell);
            callback?.Invoke(info);
        }
    }
}