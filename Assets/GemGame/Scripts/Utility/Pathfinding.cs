using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace Game.Utility
{
    public static class Pathfinding
    {
        public static List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, Tilemap tilemap, Tilemap collisionTilemap, bool isAutoChess)
        {
            List<GameData.Node> openList = new List<GameData.Node>();
            HashSet<Vector3Int> closedList = new HashSet<Vector3Int>();
            GameData.Node startNode = new GameData.Node(start, 0, Heuristic(start, goal, tilemap), null);
            openList.Add(startNode);

            Vector3[] hexDirections = new Vector3[]
            {
                new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f),
                new Vector3(0.5f, 1.154f, 0f), new Vector3(-0.5f, 1.154f, 0f),
                new Vector3(0.5f, -1.154f, 0f), new Vector3(-0.5f, -1.154f, 0f)
            };

            int maxIterations = 500;
            while (openList.Count > 0 && maxIterations > 0)
            {
                maxIterations--;
                GameData.Node current = openList[0];
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
                    GameData.Node node = current;
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
                    if (closedList.Contains(neighborPos) || !tilemap.HasTile(neighborPos) || GridUtility.HasObstacle(neighborPos, tilemap, collisionTilemap))
                    {
                        continue;
                    }

                    float newGCost = current.gCost + 1;
                    GameData.Node neighbor = new GameData.Node(neighborPos, newGCost, Heuristic(neighborPos, goal, tilemap), current);
                    bool inOpenList = false;

                    foreach (GameData.Node openNode in openList)
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

            return null;
        }

        public static List<Vector3Int> FindPathToAttackRange(Vector3Int start, Vector3Int target, float maxDistance, Tilemap tilemap, Tilemap collisionTilemap)
        {
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
            Dictionary<Vector3Int, Vector3Int> parents = new Dictionary<Vector3Int, Vector3Int>();
            Dictionary<Vector3Int, int> distances = new Dictionary<Vector3Int, int>();

            queue.Enqueue(start);
            visited.Add(start);
            distances[start] = 0;

            Vector3[] hexDirections = new Vector3[]
            {
                new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f),
                new Vector3(0.5f, 1.154f, 0f), new Vector3(-0.5f, 1.154f, 0f),
                new Vector3(0.5f, -1.154f, 0f), new Vector3(-0.5f, -1.154f, 0f)
            };

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                int currentDistance = distances[current];

                int targetDistance = GridUtility.CalculateGridDistance(current, target, tilemap, collisionTilemap);
                if (targetDistance <= maxDistance && targetDistance > 0 && tilemap.HasTile(current))
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
                    Vector3Int neighbor = tilemap.WorldToCell(tilemap.GetCellCenterWorld(current) + dir);
                    if (!visited.Contains(neighbor) && tilemap.HasTile(neighbor) && !GridUtility.HasObstacle(neighbor, tilemap, collisionTilemap))
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

        private static float Heuristic(Vector3Int a, Vector3Int b, Tilemap tilemap)
        {
            Vector3 worldA = tilemap.GetCellCenterWorld(a);
            Vector3 worldB = tilemap.GetCellCenterWorld(b);
            float distance = Vector3.Distance(worldA, worldB);
            return distance / (tilemap.cellSize.x * 0.866f);
        }
    }

    public class GameData
    {
        public class Node
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
    }
}