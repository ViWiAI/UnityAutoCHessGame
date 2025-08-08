using Game.Core;
using Game.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Managers
{
    public class WorldMapDataManager : MonoBehaviour
    {
        public static WorldMapDataManager Instance { get; private set; }

        // 模拟数据库的格子数据
        [System.Serializable]
        public class GridInfo
        {
            public Vector3Int cell;
            public List<string> monsterIds; // 怪物 ID 列表
            public List<DropItem> treasureItems; // 宝物列表
        }

        [SerializeField] private List<GridInfo> gridData; // Inspector 配置模拟数据

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

        // 查询格子信息
        public GridInfo GetGridInfo(Vector3Int cell)
        {
            if (gridInfoDict.TryGetValue(cell, out GridInfo info))
            {
                return info;
            }
            return new GridInfo { cell = cell, monsterIds = new List<string>(), treasureItems = new List<DropItem>() };
        }

        // 模拟数据库查询（可替换为 WebSocketManager 调用）
        public void QueryGridInfo(Vector3Int cell, System.Action<GridInfo> callback)
        {
            // 模拟异步查询
            GridInfo info = GetGridInfo(cell);
            callback?.Invoke(info);
        }
    }
}