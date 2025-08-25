using UnityEngine;
using System.Collections.Generic;

public class BlockLineClearer : MonoBehaviour
{
    public BlockSnapper snapper;
    public static BlockLineClearer Instance;
    private static int nextGroupID = 1; // statischer Zähler für IDs
    public List<int> fullRows = new List<int>(); // Muss von außen gesetzt werden!

    private void Awake()
    {
        Instance = this;
        if (snapper == null) snapper = BlockSnapper.Instance;
    }

    void Update()
    {
        
    }

    public void CheckAndClearFullRows()
    {
        fullRows.Clear(); // <-- Das Feld leeren, nicht eine neue Liste anlegen!

        // 1. Finde alle vollen Reihen
        for (int y = 0; y < Grid.Instance.height; y++)
        {
            bool full = true;
            for (int x = 0; x < Grid.Instance.width; x++)
            {
                if (!snapper.IsCellOccupied(new Vector2Int(x, y)))
                {
                    full = false;
                    break;
                }
            }
            if (full) fullRows.Add(y);
        }

        if (fullRows.Count == 0) return;

        // 2. Lösche nur Block-Kinder, deren Grid-Position exakt in der vollen Reihe liegt
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        HashSet<Transform> parentsToCheck = new HashSet<Transform>();

        foreach (int y in fullRows)
        {
            for (int x = 0; x < Grid.Instance.width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                foreach (var t in allBlocks)
                {
                    if (t.parent == null) continue;
                    if (t.parent.GetComponent<BlockDragHandler>() == null) continue;

                    Vector2Int tCell = snapper.WorldToGrid(t.position);
                    if (tCell == cell)
                    {
                        // Statt UnregisterChild und Destroy:
                        RemoveAndSplitIfIsolated(t);
                        parentsToCheck.Add(t.parent);
                    }
                }
                snapper.SetCellOccupied(cell, false);
            }
        }

        // 3. Lösche Eltern ohne Kinder
        /*
        foreach (var parent in parentsToCheck)
        {
            if (parent.childCount == 0)
                Destroy(parent.gameObject);
            //  SplitDisconnectedChildren(parent);
        }*/
        RebuildGridFromScene();
    }

    public void RemoveAndSplitIfIsolated(Transform child)
    {
        var parent = child.parent;
        var groupGrid = parent.GetComponent<BlockGroupGrid>();
        if (groupGrid == null)
        {
            Destroy(child.gameObject);
            return;
        }

        // 1. Kind im Grid austragen und löschen
        groupGrid.UnregisterChild(child);
        Destroy(child.gameObject);

        // 2. Prüfe für jedes verbleibende Kind, ob es Nachbarn im Grid hat
        var allChildren = new List<Transform>();
        foreach (Transform c in parent)
            allChildren.Add(c);

        if (allChildren.Count == 0)
        {
            // Parent ggf. löschen und Grid freigeben
            CleanupParentAndGrid(parent, groupGrid);
            return;
        }

        int width = groupGrid.grid.GetLength(0);
        int height = groupGrid.grid.GetLength(1);

        // Sammle isolierte Kinder
        var isolatedChildren = new List<(Transform c, int x, int y)>();
        foreach (var c in allChildren)
        {
            Vector3 localPos = parent.InverseTransformPoint(c.position);
            int x = Mathf.RoundToInt(localPos.x);
            int y = Mathf.RoundToInt(localPos.y);

            bool hasNeighbor = false;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int dir = 0; dir < 4; dir++)
            {
                int nx = x + dx[dir];
                int ny = y + dy[dir];
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (groupGrid.grid[nx, ny])
                    {
                        hasNeighbor = true;
                        break;
                    }
                }
            }
            if (!hasNeighbor)
                isolatedChildren.Add((c, x, y));
        }

        // ... in RemoveAndSplitIfIsolated ...
        foreach (var (c, x, y) in isolatedChildren)
        {
            // Prüfe, ob das Kind noch im alten Parent ist (wichtig!)
            if (c.parent != parent) continue;

            // Prüfe, ob das Kind in einer gelöschten Zeile liegt
            if (fullRows.Contains(y))
            {
                // Kind wird sowieso gelöscht, kein neues Parent erzeugen!
                groupGrid.SetBlock(x, y, false);
                snapper.SetCellOccupied(new Vector2Int(x, y), false);
                Destroy(c.gameObject);
                continue;
            }

            // Nur für "überlebende" isolierte Kinder ein neues Parent erzeugen:
            var newParent = new GameObject(parent.name + "_Single");
            newParent.transform.position = c.position;
            newParent.transform.rotation = parent.transform.rotation;
            newParent.AddComponent<BlockDragHandler>();
            //newParent.AddComponent<SelfDestroy>();
            var idComp = newParent.AddComponent<BlockGroupID>();
            idComp.groupID = ++nextGroupID;


            var newGrid = newParent.AddComponent<BlockGroupGrid>();
            newGrid.shapeType = groupGrid.shapeType;
            newGrid.grid = new bool[width, height];
            newGrid.SetBlock(x, y, true);

            c.SetParent(newParent.transform, true);

            // Grid im alten Parent austragen
            groupGrid.SetBlock(x, y, false);
            //newGrid.SetBlock(x, y, false);
            //newParent.GetComponent<BlockGroupGrid>().SetBlock(x, y, false);
            snapper.SetCellOccupied(new Vector2Int(x, y), false);
        }
        
    }

    // Hilfsfunktion zum Grid-Freigeben und Parent-Löschen
    private void CleanupParentAndGrid(Transform parent, BlockGroupGrid groupGrid)
    {
        if (groupGrid != null && groupGrid.grid != null)
        {
            int width = groupGrid.grid.GetLength(0);
            int height = groupGrid.grid.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (groupGrid.grid[x, y])
                    {
                        snapper.SetCellOccupied(new Vector2Int(x, y), false);
                        groupGrid.grid[x, y] = false;
                    }
                }
            }
        }
        Destroy(parent.gameObject);
    }





    // Hilfsfunktion: Sind zwei Blöcke Nachbarn in Weltkoordinaten?
    private bool AreWorldNeighbors(Transform a, Transform b)
    {
        Vector3 delta = a.position - b.position;
        delta.x = Mathf.Round(delta.x * 100f) / 100f;
        delta.y = Mathf.Round(delta.y * 100f) / 100f;
        delta.z = Mathf.Round(delta.z * 100f) / 100f;
        return (Mathf.Abs(delta.x) == 1f && Mathf.Abs(delta.y) < 0.01f) ||
               (Mathf.Abs(delta.y) == 1f && Mathf.Abs(delta.x) < 0.01f);
    }



    private List<Transform> SeparateAllChildren(Transform parent)
    {
        var children = new List<Transform>();
        foreach (Transform child in parent)
            children.Add(child);

        var newParents = new List<Transform>();
        int groupID = parent.GetComponent<BlockGroupID>()?.groupID ?? 0;

        foreach (var child in children)
        {
            var newParentGO = new GameObject(parent.name + "_Single");
            newParentGO.transform.position = child.position;
            newParentGO.transform.rotation = Quaternion.identity;
            newParentGO.AddComponent<BlockDragHandler>();

            // Nur das Parent bekommt die BlockGroupID!
            var idComp = newParentGO.AddComponent<BlockGroupID>();
            idComp.groupID = groupID;

            child.SetParent(newParentGO.transform, true); // Weltposition bleibt erhalten!
            newParents.Add(newParentGO.transform);
        }

        Destroy(parent.gameObject);
        return newParents;
    }



    public void CleanupOrphanedGridCells()
    {
        var snapper = BlockSnapper.Instance;
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;

        // Alle Block-Objekte sammeln
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!snapper.IsCellOccupied(cell))
                    continue;

                bool found = false;
                foreach (var t in allBlocks)
                {
                    // Prüfe: Ist das ein Block-Kind oder Parent, das diese Zelle wirklich belegt?
                    Vector2Int tCell = snapper.WorldToGrid(t.position);
                    if (tCell == cell)
                    {
                        found = true;
                        break;
                    }
                }

                // Falls kein echtes Objekt gefunden: Zelle freigeben!
                if (!found)
                {
                    snapper.SetCellOccupied(cell, false);
                }
            }
        }

        // Zusätzlich: Leere Eltern ohne Kinder entfernen
        foreach (var t in allBlocks)
        {
            if (t.childCount == 0 && t.GetComponent<BlockGroupGrid>() != null)
            {
                Destroy(t.gameObject);
            }
        }
    }




    // Prüft, ob zwei Kinder benachbart sind (direkt horizontal/vertikal)
    private bool IsNeighbor(Transform a, Transform b)
    {
        // Nur prüfen, wenn beide Kinder BlockDragHandler haben (oder ein anderes Marker-Component)
        // Entfernen:
        if (!a.CompareTag("BlockChild") || !b.CompareTag("BlockChild"))
            return false;

        Vector2Int cellA = snapper.WorldToGrid(a.position);
        Vector2Int cellB = snapper.WorldToGrid(b.position);
        int dx = Mathf.Abs(cellA.x - cellB.x);
        int dy = Mathf.Abs(cellA.y - cellB.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    public void RebuildGridFromScene()
    {
        // 1. Globales Grid komplett freigeben
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                snapper.SetCellOccupied(new Vector2Int(x, y), false);

        // 2. Alle Block-Eltern durchgehen
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in allBlocks)
        {
            var groupGrid = t.GetComponent<BlockGroupGrid>();
            if (groupGrid != null)
            {
                if (t.childCount == 0)
                {
                    // Leere Eltern entfernen
                    Destroy(t.gameObject);
                    continue;
                }

                // Grid im Parent zurücksetzen
                int gw = groupGrid.grid.GetLength(0);
                int gh = groupGrid.grid.GetLength(1);
                for (int gx = 0; gx < gw; gx++)
                    for (int gy = 0; gy < gh; gy++)
                        groupGrid.grid[gx, gy] = false;

                // Kinder neu im Parent-Grid und globalen Grid registrieren
                foreach (Transform child in t)
                {
                    Vector3 localPos = Quaternion.Inverse(t.rotation) * (child.position - t.position);
                    int cx = Mathf.RoundToInt(localPos.x);
                    int cy = Mathf.RoundToInt(localPos.y);

                    if (cx >= 0 && cx < gw && cy >= 0 && cy < gh)
                    {
                        groupGrid.grid[cx, cy] = true;
                        Vector2Int gridPos = snapper.WorldToGrid(child.position);
                        snapper.SetCellOccupied(gridPos, true);
                    }
                }
            }
        }
    }
    // Duplicate method 'RemoveChildAndSplitSingles' removed to resolve compile error.
}