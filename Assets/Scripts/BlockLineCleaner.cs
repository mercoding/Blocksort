using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class BlockLineClearer : MonoBehaviour
{
    public BlockSnapper snapper;
    public static BlockLineClearer Instance;
    private static int nextGroupID = 1; // statischer Zähler für IDs
    public List<int> fullRows = new List<int>(); // Muss von außen gesetzt werden!
    public List<Vector2Int> deletedCells = new List<Vector2Int>();
    public bool updated = false;

    public Sprite ghostSprite;

    private void Awake()
    {
        Instance = this;
        if (snapper == null) snapper = BlockSnapper.Instance;
    }

    void Update()
    {

    }

    public void RebuildGridFromScene()
    {
        //SplitDisconnectedBlocksByGroupID();
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;
        // 1. Grid komplett freigeben
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                snapper.SetCellOccupied(new Vector2Int(x, y), false);

        // 2. Alle echten Block-Parents und -Kinder durchgehen
        var allBlocks = GameObject.FindGameObjectsWithTag("Block");
        foreach (var parent in allBlocks)
        {
            foreach (Transform child in parent.transform)
            {
                if (!child.CompareTag("BlockChild")) continue;
                Vector2Int cell = snapper.WorldToGrid(child.position);
                if (snapper.IsInsideGrid(cell))
                    snapper.SetCellOccupied(cell, true);
            }
        }
    }

    public void RemoveFullRowBlocks()
    {
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;
        var snapper = BlockSnapper.Instance;

        // 1. Finde alle vollen Reihen
        List<int> fullRows = new List<int>();
        for (int y = 0; y < height; y++)
        {
            bool full = true;
            for (int x = 0; x < width; x++)
            {
                if (!snapper.IsCellOccupied(new Vector2Int(x, y)))
                {
                    full = false;
                    break;
                }
            }
            if (full)
                fullRows.Add(y);
        }

        if (fullRows.Count == 0) return;

        // 2. Sammle alle Kinder, die in einer vollen Reihe liegen, und deren Parents
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        HashSet<Transform> parentsToCheck = new HashSet<Transform>();

        foreach (var t in allBlocks)
        {
            if (t.parent == null) continue;
            if (t.parent.GetComponent<BlockDragHandler>() == null) continue;

            Vector2Int tCell = snapper.WorldToGrid(t.position);
            if (fullRows.Contains(tCell.y))
            {
                // Austragen aus Parent-Grid, falls vorhanden
                var groupGrid = t.parent.GetComponent<BlockGroupGrid>();
                if (groupGrid != null)
                {
                    Vector3 localPos = t.parent.InverseTransformPoint(t.position);
                    int x = Mathf.RoundToInt(localPos.x);
                    int y = Mathf.RoundToInt(localPos.y);
                    if (x >= 0 && x < groupGrid.grid.GetLength(0) && y >= 0 && y < groupGrid.grid.GetLength(1))
                        groupGrid.grid[x, y] = false;
                }

                // Austragen aus globalem Grid
                snapper.SetCellOccupied(tCell, false);

                // Parent merken
                parentsToCheck.Add(t.parent);
                // Kind sofort löschen
                DestroyImmediate(t.gameObject);

            }
        }
    }

    public int GetNextGroupID()
    {
        return ++TetrisPuzzleSpawner.nextGroupID;
    }

    public void SplitDisconnectedBlocksByGroupID()
    {
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        // Nur echte Block-Eltern!
        var parents = allBlocks
            .Where(t => t.CompareTag("Block") && t.GetComponent<BlockGroupID>() != null && t.childCount > 1)
            .ToList();

        foreach (var parent in parents)
        {
            var groupGrid = parent.GetComponent<BlockGroupGrid>();
            var groupID = parent.GetComponent<BlockGroupID>().groupID;

            // Nur echte Block-Kinder!
            var childToCell = new Dictionary<Transform, Vector2Int>();
            foreach (Transform child in parent)
            {
                if (!child.CompareTag("BlockChild")) continue;
                childToCell[child] = snapper.WorldToGrid(child.position);
            }

            if (childToCell.Count <= 1) continue;

            var unvisited = new HashSet<Transform>(childToCell.Keys);
            var groups = new List<List<Transform>>();

            // Flood-Fill wie gehabt ...
            while (unvisited.Count > 0)
            {
                var queue = new Queue<Transform>();
                var group = new List<Transform>();
                var first = unvisited.First();
                queue.Enqueue(first);
                group.Add(first);
                unvisited.Remove(first);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    Vector2Int cell = childToCell[current];
                    int[] dx = { 1, -1, 0, 0 };
                    int[] dy = { 0, 0, 1, -1 };
                    foreach (var other in unvisited.ToList())
                    {
                        Vector2Int otherCell = childToCell[other];
                        for (int dir = 0; dir < 4; dir++)
                        {
                            if (otherCell == cell + new Vector2Int(dx[dir], dy[dir]))
                            {
                                queue.Enqueue(other);
                                group.Add(other);
                                unvisited.Remove(other);
                                break;
                            }
                        }
                    }
                }
                groups.Add(group);
            }

            if (groups.Count > 1)
            {
                foreach (var group in groups)
                {
                    var newParent = new GameObject(parent.name + "_Split");
                    newParent.tag = "Block";
                    newParent.transform.position = group[0].position;
                    newParent.transform.rotation = parent.transform.rotation;
                    newParent.AddComponent<BlockDragHandler>();
                    newParent.GetComponent<BlockDragHandler>().ghostSprite = ghostSprite;
                    
                    var newGrid = newParent.AddComponent<BlockGroupGrid>();
                    newGrid.shapeType = groupGrid != null ? groupGrid.shapeType : 0;
                    newGrid.grid = new bool[4, 4];

                    var idComp = newParent.AddComponent<BlockGroupID>();
                    idComp.groupID = BlockLineClearer.Instance.GetNextGroupID(); // <-- neue ID!

                    foreach (var child in group)
                    {
                        child.SetParent(newParent.transform, true);
                        child.tag = "BlockChild";
                    }
                }

                Destroy(parent.gameObject);
            }
        }
    }
}