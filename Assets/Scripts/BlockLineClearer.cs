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
    public int visibleHeight = 16; // z.B. 16 sichtbare Reihen, 4 als Spawn


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
                if (child.parent != null) child.parent.GetComponent<BlockDragHandler>().AdjustCollider();
                if (!child.CompareTag("BlockChild")) continue;
                Vector2Int cell = snapper.WorldToGrid(child.position);
                if (snapper.IsInsideGrid(cell))
                    snapper.SetCellOccupied(cell, true);
                parent.GetComponent<BlockDragHandler>().AdjustChildColliders();
                parent.GetComponent<BlockDragHandler>().isDragging = false;
            }
        }
    }



    public void DropAllBlocksAsFarAsPossible()
    {
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;

        var allParents = GameObject.FindGameObjectsWithTag("Block");
        foreach (var parent in allParents)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform child in parent.transform)
            {
                if (!child.CompareTag("BlockChild")) continue;
                children.Add(child);
            }
            if (children.Count == 0) continue;

            // Für jedes Kind: Wie weit kann es maximal nach unten rutschen?
            int maxDrop = int.MaxValue;
            foreach (var child in children)
            {
                Vector2Int cell = snapper.WorldToGrid(child.position);
                int drop = 0;
                for (int y = cell.y - 1; y >= 0; y--)
                {
                    if (snapper.IsCellOccupied(new Vector2Int(cell.x, y)))
                        break;
                    drop++;
                }
                maxDrop = Mathf.Min(maxDrop, drop);
            }

            if (maxDrop > 0)
            {
                foreach (var child in children)
                {
                    child.position += new Vector3(0, -maxDrop * Grid.Instance.cellSize, 0);
                }
                // Collider ggf. anpassen
                var handler = parent.GetComponent<BlockDragHandler>();
                if (handler != null)
                    handler.AdjustChildColliders();
            }
        }
    }



    public void DropOnlyFittingInvisibleBlocksByFreeRowsOnTop()
    {
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;
        int freeRowsOnTop = 0;
        for (int y = visibleHeight - 1; y >= 0; y--)
        {
            bool rowFree = true;
            for (int x = 0; x < width; x++)
            {
                if (snapper.IsCellOccupied(new Vector2Int(x, y)))
                {
                    rowFree = false;
                    break;
                }
            }
            if (rowFree)
                freeRowsOnTop++;
            else
                break;
        }

        if (freeRowsOnTop == 0)
            return;

        var allParents = GameObject.FindGameObjectsWithTag("Block");
        foreach (var parent in allParents)
        {
            bool canDrop = true;
            foreach (Transform child in parent.transform)
            {
                if (!child.CompareTag("BlockChild")) continue;
                Vector2Int cell = snapper.WorldToGrid(child.position);
                int targetY = cell.y - freeRowsOnTop;
                if (targetY < 0 || targetY >= visibleHeight)
                {
                    canDrop = false;
                    break;
                }
            }

            if (canDrop)
            {
                foreach (Transform child in parent.transform)
                {
                    if (!child.CompareTag("BlockChild")) continue;
                    child.position += new Vector3(0, -freeRowsOnTop * Grid.Instance.cellSize, 0);
                }
                // Collider ggf. anpassen
                var handler = parent.GetComponent<BlockDragHandler>();
                if (handler != null)
                    handler.AdjustChildColliders();
            }
        }
    }



    public void DropInvisibleBlocksByFreeRowsOnTop()
    {
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;

        // Zähle, wie viele Reihen im sichtbaren Grid (y = visibleHeight-1 bis y = 0) komplett frei sind (von oben nach unten)
        int freeRowsOnTop = 0;
        for (int y = visibleHeight - 1; y >= 0; y--)
        {
            bool rowFree = true;
            for (int x = 0; x < width; x++)
            {
                if (snapper.IsCellOccupied(new Vector2Int(x, y)))
                {
                    rowFree = false;
                    break;
                }
            }
            if (rowFree)
                freeRowsOnTop++;
            else
                break; // Nur zusammenhängende freie Reihen ganz oben zählen!
        }

        if (freeRowsOnTop == 0)
            return;

        // Verschiebe alle Block-Kinder im unsichtbaren Bereich (y >= visibleHeight) um freeRowsOnTop nach unten
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in allBlocks)
        {
            if (t.parent == null) continue;
            if (!t.CompareTag("BlockChild")) continue;

            Vector2Int cell = snapper.WorldToGrid(t.position);
            if (cell.y >= visibleHeight)
            {
                t.position += new Vector3(0, -freeRowsOnTop * Grid.Instance.cellSize, 0);
            }

            if (t.parent.GetComponent<BlockDragHandler>() != null)
            {
                t.parent.GetComponent<BlockDragHandler>().AdjustCollider();
            }
        }
    }


    public void RemoveFullRowBlocks()
    {
        var fullRows = FindFullRows();
        if (fullRows.Count == 0) return;

        var parentsToCheck = RemoveChildrenInRows(fullRows);
        // Optional: Rückgabe verwenden, falls du noch weitere Schritte brauchst
    }

    public List<int> FindFullRows()
    {
        int width = Grid.Instance.width;
        int height = visibleHeight; // Nur sichtbares Grid prüfen
        var snapper = BlockSnapper.Instance;
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
        return fullRows;
    }

    private HashSet<Transform> RemoveChildrenInRows(List<int> fullRows)
    {
        var snapper = BlockSnapper.Instance;
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        HashSet<Transform> parentsToCheck = new HashSet<Transform>();

        foreach (var t in allBlocks)
        {
            if (t == null) continue; // <--- Schutz gegen zerstörte Objekte
            if (t.parent == null) continue;
            if (t.parent.GetComponent<BlockDragHandler>() == null) continue;

            Vector2Int tCell = snapper.WorldToGrid(t.position);
            if (fullRows.Contains(tCell.y))
            {
                RemoveChildFromParentGrid(t);
                snapper.SetCellOccupied(tCell, false);
                parentsToCheck.Add(t.parent);

                StartCoroutine(AnimateAndDestroyBlockChild(t.gameObject, 0.5f));
                // Nach dem Start der Coroutine KEINE weiteren Zugriffe auf t!
                // t.GetComponent<BlockDragHandler>()?.AdjustCollider(); // <--- Entfernen!
            }
        }
        return parentsToCheck;
    }

    private void RemoveChildFromParentGrid(Transform child)
    {
        var groupGrid = child.parent.GetComponent<BlockGroupGrid>();
        if (groupGrid != null)
        {
            Vector3 localPos = child.parent.InverseTransformPoint(child.position);
            int x = Mathf.RoundToInt(localPos.x);
            int y = Mathf.RoundToInt(localPos.y);
            if (x >= 0 && x < groupGrid.grid.GetLength(0) && y >= 0 && y < groupGrid.grid.GetLength(1))
                groupGrid.grid[x, y] = false;
        }
    }


    public IEnumerator AnimateAndDestroyBlockChild(GameObject blockChild, float delay = 0.5f)
    {
        // 1. Klone das BlockChild für die Animation
        GameObject clone = Instantiate(blockChild, blockChild.transform.position, blockChild.transform.rotation);
        clone.transform.localScale = blockChild.transform.localScale;
        clone.GetComponent<SpriteRenderer>().sortingOrder = 100; // Optional: Animation immer oben

        // 2. Original sofort zerstören (ist nicht mehr im Grid)
        DestroyImmediate(blockChild);

        // 3. Animation am Klon (z.B. Fade-Out)
        var sr = clone.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float t = 0;
            Color startColor = sr.color;
            while (t < delay)
            {
                sr.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0), t / delay);
                t += Time.deltaTime;
                yield return null;
            }
            sr.color = new Color(startColor.r, startColor.g, startColor.b, 0);
        }

        // 4. Klon zerstören
        Destroy(clone);
        BlockLineClearer.Instance.UpdateSingleBlockVisuals();

        //BlockDropper.Instance.ApplyGravityToAllBlocks();
    }

    public IEnumerator RemoveFullRowBlocksAnimatedAndThenGravity(float animDelay = 0.5f)
    {
        var fullRows = FindFullRows();
        if (fullRows.Count == 0) yield break;

        var blocksToDelete = new List<GameObject>();
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);

        foreach (var t in allBlocks)
        {
            if (t.parent == null) continue;
            if (t.parent.GetComponent<BlockDragHandler>() == null) continue;

            Vector2Int tCell = snapper.WorldToGrid(t.position);
            if (fullRows.Contains(tCell.y))
            {
                RemoveChildFromParentGrid(t);
                snapper.SetCellOccupied(tCell, false);
                blocksToDelete.Add(t.gameObject);
            }
        }

        // Starte alle Animationen
        foreach (var block in blocksToDelete)
        {
            StartCoroutine(AnimateAndDestroyBlockChild(block, animDelay));
        }

        // Warte, bis alle Animationen durch sind
        yield return new WaitForSeconds(animDelay);

        // Jetzt Gravity auslösen
        yield return StartCoroutine(BlockDropper.Instance.ApplyGravityToAllBlocksCoroutine(0.2f));
    }


    public void UpdateSingleBlockVisuals()
    {
        var allParents = GameObject.FindGameObjectsWithTag("Block");
        foreach (var parent in allParents)
        {
            int childCount = 0;
            Transform singleChild = null;
            foreach (Transform child in parent.transform)
            {
                if (!child.CompareTag("BlockChild")) continue;
                childCount++;
                singleChild = child;
            }

            if (childCount == 1 && singleChild != null)
            {
                var sr = singleChild.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = new Color(0.6f, 0.6f, 0.6f, 1.25f); // z.B. grau und halbtransparent
            }
            else
            {
                foreach (Transform child in parent.transform)
                {
                    if (!child.CompareTag("BlockChild")) continue;
                    var sr = child.GetComponent<SpriteRenderer>();
                    if (sr != null && childCount <= 3)
                        sr.color = new Color(0.7f, 0.7f, 0.7f, 1.5f);
                    else if (sr != null)
                        sr.color = Color.white; // Standardfarbe
                }
            }
        }
    }


    public IEnumerator RemoveFullRowBlocksAnimated(float animDelay = 0.5f)
    {
        var fullRows = FindFullRows();
        if (fullRows.Count == 0) yield break;

        var blocksToDelete = new List<GameObject>();
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);

        foreach (var t in allBlocks)
        {
            if (t.parent == null) continue;
            if (t.parent.GetComponent<BlockDragHandler>() == null) continue;

            Vector2Int tCell = snapper.WorldToGrid(t.position);
            if (fullRows.Contains(tCell.y))
            {
                RemoveChildFromParentGrid(t);
                snapper.SetCellOccupied(tCell, false);
                blocksToDelete.Add(t.gameObject);
            }
        }

        foreach (var block in blocksToDelete)
        {
            StartCoroutine(AnimateAndDestroyBlockChild(block, animDelay));
        }

        yield return new WaitForSeconds(animDelay);
    }

    public void RemoveFullColumnBlocks()
    {
        var fullCols = FindFullColumns();
        if (fullCols.Count == 0) return;

        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);

        foreach (var t in allBlocks)
        {
            if (t.parent == null) continue;
            if (t.parent.GetComponent<BlockDragHandler>() == null) continue;

            Vector2Int tCell = BlockSnapper.Instance.WorldToGrid(t.position);
            if (fullCols.Contains(tCell.x) && tCell.y < visibleHeight)
            {
                RemoveChildFromParentGrid(t);
                BlockSnapper.Instance.SetCellOccupied(tCell, false);
                StartCoroutine(AnimateAndDestroyBlockChild(t.gameObject, 0.5f));
                t.GetComponent<BlockDragHandler>()?.AdjustCollider();
            }
        }
    }

    public List<int> FindFullColumns()
    {
        int width = Grid.Instance.width;
        int height = visibleHeight; // Nur sichtbares Feld!
        var snapper = BlockSnapper.Instance;
        List<int> fullCols = new List<int>();

        for (int x = 0; x < width; x++)
        {
            bool full = true;
            for (int y = 0; y < height; y++)
            {
                if (!snapper.IsCellOccupied(new Vector2Int(x, y)))
                {
                    full = false;
                    break;
                }
            }
            if (full)
                fullCols.Add(x);
        }
        return fullCols;
    }

    /*
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
        }*/

    public int GetNextGroupID()
    {
        return ++TetrisPuzzleSpawner.nextGroupID;
    }

    private List<Transform> FindSplitCandidates()
    {
        var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        return allBlocks
            .Where(t => t.CompareTag("Block") && t.GetComponent<BlockGroupID>() != null && t.childCount > 1)
            .ToList();
    }



    public void SplitDisconnectedBlocksByGroupID()
    {
        var parents = FindSplitCandidates();

        foreach (var parent in parents)
        {
            var groupGrid = parent.GetComponent<BlockGroupGrid>();
            var groupID = parent.GetComponent<BlockGroupID>().groupID;

            var childToCell = GetChildToCellMap(parent);
            if (childToCell.Count <= 1) continue;

            var groups = FindConnectedGroupsFloodFill(childToCell);

            if (groups.Count > 1)
            {
                CreateNewParentsForGroups(parent, groups, groupGrid);
                Destroy(parent.gameObject);
            }
        }
    }

    private Dictionary<Transform, Vector2Int> GetChildToCellMap(Transform parent)
    {
        var map = new Dictionary<Transform, Vector2Int>();
        foreach (Transform child in parent)
        {
            if (!child.CompareTag("BlockChild")) continue;
            map[child] = snapper.WorldToGrid(child.position);
        }
        return map;
    }

    private List<List<Transform>> FindConnectedGroupsFloodFill(Dictionary<Transform, Vector2Int> childToCell)
    {
        var unvisited = new HashSet<Transform>(childToCell.Keys);
        var groups = new List<List<Transform>>();

        while (unvisited.Count > 0)
        {
            var group = FloodFillGroup(unvisited.First(), unvisited, childToCell);
            groups.Add(group);
        }
        return groups;
    }

    private void AddNeighborsToFloodFill(Transform current, Dictionary<Transform, Vector2Int> childToCell, HashSet<Transform> unvisited, Queue<Transform> queue, List<Transform> group)
    {
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


    private List<Transform> FloodFillGroup(Transform start, HashSet<Transform> unvisited, Dictionary<Transform, Vector2Int> childToCell)
    {
        var group = new List<Transform>();
        var queue = new Queue<Transform>();
        queue.Enqueue(start);
        group.Add(start);
        unvisited.Remove(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            AddNeighborsToFloodFill(current, childToCell, unvisited, queue, group);
        }
        return group;
    }

    private void CreateNewParentsForGroups(Transform oldParent, List<List<Transform>> groups, BlockGroupGrid groupGrid)
    {
        foreach (var group in groups)
        {
            var newParent = new GameObject(oldParent.name + "_Split");
            newParent.tag = "Block";
            newParent.transform.position = group[0].position;
            newParent.transform.rotation = oldParent.transform.rotation;
            var handler = newParent.AddComponent<BlockDragHandler>();
            handler.ghostSprite = ghostSprite;

            var newGrid = newParent.AddComponent<BlockGroupGrid>();
            newGrid.shapeType = groupGrid != null ? groupGrid.shapeType : 0;
            newGrid.grid = new bool[4, 4];

            var idComp = newParent.AddComponent<BlockGroupID>();
            idComp.groupID = BlockLineClearer.Instance.GetNextGroupID();

            foreach (var child in group)
            {
                child.SetParent(newParent.transform, true);
                child.tag = "BlockChild";
            }

            handler.AdjustChildColliders(); // <--- Collider/Skripte für alle Kinder neu setzen!
        }
    }


    public void DropBlocksAfterLineClear(List<int> clearedRows)
    {
        clearedRows.Sort(); // Von unten nach oben!
        foreach (int clearedY in clearedRows)
        {
            var allBlocks = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allBlocks)
            {
                if (t.parent == null) continue;
                if (!t.CompareTag("BlockChild")) continue;

                Vector2Int cell = BlockSnapper.Instance.WorldToGrid(t.position);
                if (cell.y > clearedY)
                {
                    // Nach unten verschieben
                    t.position += new Vector3(0, -Grid.Instance.cellSize, 0);
                }
            }
        }
    }

    /*
        public void SplitDisconnectedBlocksByGroupID()
        {
            var parents = FindSplitCandidates();

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
        }*/
}