using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text;

public static class GridUtility
{
    private static readonly Vector3[] hexDirections = new Vector3[]
    {
        new Vector3(1f, 0f, 0f),          // 右
        new Vector3(-1f, 0f, 0f),         // 左
        new Vector3(0.5f, 1.154f, 0f),    // 右上
        new Vector3(-0.5f, 1.154f, 0f),   // 左上
        new Vector3(0.5f, -1.154f, 0f),   // 右下
        new Vector3(-0.5f, -1.154f, 0f)   // 左下
    };

    // 新增：生成随机 playerId
    public static string GenerateRandomPlayerId(int length = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder result = new StringBuilder("Player_", length + 7); // 预分配容量，包含前缀

        for (int i = 0; i < length; i++)
        {
            result.Append(chars[Random.Range(0, chars.Length)]);
        }

        return result.ToString();
    }

    public static List<Vector3Int> GetCellsInRange(Vector3Int targetCell, float aoeRadius, Tilemap tilemap, Tilemap collisionTilemap, bool isAutoChess)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap is null in GetCellsInRange!");
            return new List<Vector3Int>();
        }

        int radius = Mathf.FloorToInt(aoeRadius); // 取整处理
        if (radius < 0) return new List<Vector3Int>();

        List<Vector3Int> result = new List<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<(Vector3Int cell, int distance)> queue = new Queue<(Vector3Int, int)>();

        // 初始化中心格子
        if (tilemap.HasTile(targetCell) && !HasObstacle(targetCell, tilemap, collisionTilemap))
        {
            result.Add(targetCell);
            visited.Add(targetCell);
            queue.Enqueue((targetCell, 0));
        }

        // BFS 扩展范围
        while (queue.Count > 0)
        {
            var (currentCell, currentDistance) = queue.Dequeue();
            if (currentDistance >= radius) continue;

            foreach (Vector3 dir in hexDirections)
            {
                Vector3Int neighbor = tilemap.WorldToCell(tilemap.GetCellCenterWorld(currentCell) + dir);
                if (!visited.Contains(neighbor) && tilemap.HasTile(neighbor) && !HasObstacle(neighbor, tilemap, collisionTilemap))
                {
                    visited.Add(neighbor);
                    result.Add(neighbor);
                    queue.Enqueue((neighbor, currentDistance + 1));
                }
            }
        }

        //Debug.Log($"GetCellsInRange: Found {result.Count} cells within radius {aoeRadius} from {targetCell}");
        return result;
    }

    public static int CalculateGridDistance(Vector3Int start, Vector3Int goal, Tilemap tilemap, Tilemap collisionTilemap)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap is null in CalculateGridDistance!");
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
                Vector3Int neighbor = tilemap.WorldToCell(tilemap.GetCellCenterWorld(current) + dir);
                if (!visited.Contains(neighbor) && tilemap.HasTile(neighbor) && !HasObstacle(neighbor, tilemap, collisionTilemap))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    distances[neighbor] = currentDistance + 1;

                    if (neighbor == goal)
                    {
                        //Debug.Log($"Grid Distance from {start} to {goal}: {distances[neighbor]}");
                        return distances[neighbor];
                    }
                }
            }
        }

        Debug.Log($"No path found from {start} to {goal}");
        return int.MaxValue;
    }

    public static bool HasObstacle(Vector3Int cell, Tilemap tilemap, Tilemap collisionTilemap)
    {
        if (collisionTilemap == null) return false;
        Vector3 worldPos = tilemap.GetCellCenterWorld(cell);
        Vector3Int cellPos = collisionTilemap.WorldToCell(worldPos);
        return collisionTilemap.HasTile(cellPos);
    }


}